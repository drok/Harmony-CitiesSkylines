using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using static UnityEngine.Debug;
using static UnityEngine.Assertions.Assert;
using UnityEngine;
using System.Collections;
using static Json.NETMF.JsonSerializer;
using static Json.NETMF.JsonParser;
using UnityEngine.Networking;
using ColossalFramework;


namespace HarmonyMod
{
    internal class GitHubRelease : Item
    {
// #if DEBUG
//         const string DEFAULT_HOSTNAME = "pro41.ohmisim.ohmi.org";
// #else
        const string DEFAULT_HOSTNAME = "github.com";
//#endif
        readonly string author;
        readonly string repoName;
        readonly string branchName;
        readonly string hostName;
        const string ACCEPT_RELEASE_DATA = "application/vnd.github.v3+json";
        const string ACCEPT_BINARIES = "application/octet-stream";
        #region Item Information
        public override string authorID { get; set; }
        public override string authorName { get { return author; } set { } }
        public override string typeID { get { return _Item.PROVENANCE_TYPEID_GITHUB; } }
        public override string repo { get { return repoName; } }
        public override string host { get { return hostName; } }


        public override string myItemBranch { get { return typeID + "/" + authorName + "/" + repo; } }
        public override string myReleaseBranch { get { return branchName == null ? null : typeID + "/" + authorName + "/" + repo + "/" + branchName; } }
        public override string myBranchAsDirectory { get { return typeID + "_" + authorName + "_" + repo; } }

        public override string branch { get { return branchName; } } /* target_commitish */

        public override DateTimeOffset? updated { get; set; } /* published_at */
        public override string version { get; protected set; } /* tag_name */

        #endregion

        SavedString m_Latest;
        public SavedString Latest => m_Latest;

        public GitHubRelease(Uri uri, bool pinned = false)
            : base()
        {
            if (uri != null)
            {
                IsTrue(uri.Scheme == "github" || 
                    ((uri.Scheme == "https" || uri.Scheme == "http") && uri.Host.Equals(DEFAULT_HOSTNAME, StringComparison.OrdinalIgnoreCase)),
                    "Github mods require scheme 'github' or 'http[s]' and host=="+ DEFAULT_HOSTNAME + $" .. found scheme={uri.Scheme}, host={uri.Host}");
                if (uri.Segments.Length < 3 || uri.Segments.Length > 4)
                    throw new ArgumentException($"GitHub URI '{uri}' is invalid");

                if (uri.Segments[0] != "/")
                    throw new ArgumentException($"GitHub URI '{uri}' does not start with absolute path");


                hostName = string.IsNullOrEmpty(uri.Host) ? DEFAULT_HOSTNAME : uri.Host;
                author = UriSegment(uri, 1);
                repoName = UriSegment(uri, 2);
                updated = DateTimeOffset.MinValue;
                if (uri.Segments.Length == 4)
                    branchName = UriSegment(uri, 3);

                m_Latest = new SavedString(myReleaseBranch + "[latest]", Settings.userGameState);
            }
        }
        public GitHubRelease(string dirName)
            : base()
        {
            repoName = dirName;
        }

        private IEnumerable<YieldInstruction> HandleDownload(UnityWebRequest request, object destination, object metaData)
        {
            string name = (metaData as Hashtable)["name"] as string;
            string content_type = (metaData as Hashtable)["content_type"] as string;
            DownloadState dl = destination as DownloadState;


#if TRACE
            Log($"[{Versioning.FULL_PACKAGE_NAME}] received download {request.downloadHandler.data.Length} as {name}");
#endif
            bool mkdirSuccess = false;
            try
            {
                if (!Directory.Exists(dl.destDir))
                {
                    Directory.CreateDirectory(dl.destDir);
                    mkdirSuccess = true;
                }
            }
            catch (Exception ex)
            {
                LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR: Failed to create {dl.destDir}");
            }

            if (!mkdirSuccess)
                yield return DownloadManager.saveError;

            bool error = false;
            try
            {
                if (content_type == "application/zip")
                {
#if TRACE
                    Log($"[{Versioning.FULL_PACKAGE_NAME}] Should unzip {name} to {dl.destDir}");
#endif
                    dl.files = new Zip(request.downloadHandler.data, name).UnzipTo(dl.destDir);
                }
                else
                {
                    var filename = Path.Combine(dl.destDir, name);
                    var list = new List<string>();
                    list.Add(filename);
                    File.WriteAllBytes(filename, request.downloadHandler.data);
                    dl.files = list;
                }
            }
            catch (Exception ex)
            {
                LogError($"[{Versioning.FULL_PACKAGE_NAME}] Handling downloaded {name} failed: {ex.Message} at\n{ex.StackTrace}");
                error = true;
            }

#if SLOW_DOWNLOADING
            yield return new WaitForSeconds(5);
#endif
            if (error)
            {
                installationState = InstallationState.SaveError;
                yield return DownloadManager.saveError;
            }
        }

        public IEnumerable<YieldInstruction> HandleReleases(UnityWebRequest request, object destination, object context)
        {
            Collection collection = destination as Collection;
            GitHubRelease item = context as GitHubRelease;

            // context as Collection;
            bool error = false;
            try
            {
                var releases = JsonDecode(Encoding.UTF8.GetString(request.downloadHandler.data)) as ArrayList;
                //releases.Reverse();
                if (collection.StoreGithubReleaseInfo(releases, item))
                {
#if WIP_GITHUB_PAGINATION
            /* FIXME: handle pagination
             * https://stackoverflow.com/questions/9732806/a-c-sharp-parser-for-web-links-rfc-5988
             * https://github.com/JornWildt/Ramone/blob/master/Ramone/Utility/WebLinkParser.cs
             */
                var linksHeader = request.GetResponseHeader("Links");
                if (!string.IsNullOrEmpty(linksHeader))
                {
                    var links = LinkHeader.LinksFromHeader(linksHeader);
                    var download = HttpGet(
                        url: links.NextLink,
                        isJson: true,
                        destination: destination,
                        lastModified: result == Collection.CheckoutResult.UnknownRelease ? lastModified.ToString("R") : null,
                        context: work.item,
                        downloadHandler: HandleReleases);
                    foreach... in download

                }
#endif
                }
            }
            catch (Exception ex)
            {
#if TRACE
                LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERR: Handling Releases of {item} failed: {ex}");
#endif
                Mod.SelfReport(IAwareness.SelfProblemType.FailedToInitialize, ex);
                error = true;
            }
            if (error)
                yield return DownloadManager.downloadError;
        }

        IEnumerator<YieldInstruction> FetchUpdates(DownloadState dl, bool haveRelease)
        {
            Redirect latestRelease = default(Redirect);

            #region Fetch Release info
#if HEAVY_TRACE
            Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Fetching release for {this} Latest={(m_Latest.exists ? m_Latest.value : "none")}");
#endif
            {
// #if RELEASE
                var headCheck = dl.downloadManager.HttpHead("https://" + host + "/" + authorName + "/" + repo + "/releases/latest");
// #else
//                var headCheck = dl.downloadManager.HttpHead("http://" + host + "/" + authorName + "/" + repo + "/releases");
// #endif
                while (headCheck.MoveNext())
                {
                    if (headCheck.Current is Redirect redirect)
                    {
                        if (m_Latest.exists && m_Latest.value == redirect.location)
                        {
                            // installationState = InstallationState.NoUpdateAvailable;
                            yield return DownloadManager.notModified;
                            // yield break;
                        }
                        else
                            latestRelease = redirect;

                    }
                    else if (headCheck.Current == DownloadManager.notFound)
                    {
                        // The repo does not exist (or network connection?)
                        installationState = InstallationState.DoesNotExist;
                    }
                    yield return headCheck.Current;
                }

#if HEAVY_TRACE
                Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Verified that {this} exists (state={installationState}). Now check releases.");
#endif
            }

            // Mod.mainModInstance.modManager.repo.SetRebaseTo();
            var download = dl.downloadManager.HttpGet(
//#if RELEASE
                url: "https://api." + host + "/repos/" + authorName + "/" + repo + "/releases",
// #else
//                 url: "http://" + host + "/" + authorName + "/" + repo + "/releases",
// #endif
                accept: ACCEPT_RELEASE_DATA,
                destination: Mod.mainModInstance.repo,
                etag: null,
                context: this,
                downloadHandler: HandleReleases);

            while (download.MoveNext())
            {
                if (download.Current == DownloadManager.notModified)
                {
                    // installationState = InstallationState.NoUpdateAvailable;
                    break;
                }
                else if (download.Current == DownloadManager.downloadError)
                {
                    installationState = InstallationState.InfoError;
                    // break;
                }
                else if (download.Current == DownloadManager.notFound)
                {
                    installationState = InstallationState.InfoError;
                    // break;
                }
                else if (download.Current is TemporaryError)
                {
                    installationState = InstallationState.RateLimit;
                }
                yield return download.Current;
            }
            if (latestRelease != default(Redirect))
                m_Latest.value = latestRelease.location;
#endregion
        }

        IEnumerator<YieldInstruction> FetchAssets(Loaded destination, Hashtable itemRelease, DownloadState dl)
        {
#region Fetching Assets
#if HEAVY_TRACE
            Log($"[{Versioning.FULL_PACKAGE_NAME}] Fetching assets for {this}");
#endif

#region Setup download location
            /* Each local mod is a worktree, so the mod files must be at the root of the workingDirectory.
             * Non-local mod files share a worktree, so they will be in a subdir named for the mod's full branch name
             */
            dl.destDir = Path.Combine(Mod.mainModInstance.repo.tempDir, downloadToDir);

#endregion

            var authorHash = itemRelease["author"] as Hashtable;
            authorID = ((long)authorHash["id"]).ToString();
            foreach (Hashtable asset in itemRelease["assets"] as ArrayList)
            {
                var download = asset["content_type"] as string switch
                {
                    "application/zip" => dl.downloadManager.HttpGet(
                            url: asset["browser_download_url"] as string,
                            accept: ACCEPT_BINARIES,
                            destination: dl,
                            etag: string.Empty,
                            context: asset,
                            downloadHandler: HandleDownload),

                    _ => DoNothing(asset["content_type"] as string),
                };
                while (download.MoveNext()) {
                    yield return download.Current;
                }
            }
#endregion
        }

        public override IEnumerator<YieldInstruction> FetchReleaseInfo(
            bool update,
            Loaded destination,
            DownloadManager downloadManager
            )
        {
#if HEAVY_TRACE
            Log($"[{Versioning.FULL_PACKAGE_NAME}] Installer DoWork on {this} for mod {destination}");
#endif
            var dl = new DownloadState(downloadManager);

            Collection.CheckoutResult result;

#region Check if download can be avoided
            result = Mod.mainModInstance.repo.PrepareToDownload(this, update, out Hashtable itemRelease);
            if (result == Collection.CheckoutResult.OK)
            {
#if TRACE
                Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO: Installation of {this} succeeded from local repo");
#endif
                yield break;
            }
#endregion


            if (result == Collection.CheckoutResult.UnknownRelease)
            {
                var activity = FetchUpdates(dl, false);
                while (activity.MoveNext())
                    if (activity.Current == DownloadManager.notModified) break;
                    else
                    yield return activity.Current;

                result = Mod.mainModInstance.repo.PrepareToDownload(this, false, out itemRelease);
                //if (result == Collection.CheckoutResult.UnknownRelease)
                //    installationState = InstallationState.NoUpdateAvailable;

            }

            if (result == Collection.CheckoutResult.NeedAssets)
            {
                var activity = FetchAssets(destination, itemRelease, dl);
                while (activity.MoveNext()) yield return activity.Current;

#if TRACE
                Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - {this} succeeded to download location {dl.destDir != null}");
#endif
                dl.packageInfo.Add(_Item.RELEASE_DATA_ID_RELEASE, itemRelease);
                dl.DownloadSuccess(destination, this, itemRelease, dl);

                try
                {
                    IsFalse(Directory.Exists(dl.destDir), "Download dest dir should have been moved in place");
                    if (Directory.Exists(dl.destDir))
                    {
#if HEAVY_TRACE
                        Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Delete Dest directory '{dl.destDir}' for {this}");
#endif
                        Directory.Delete(dl.destDir, true);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR: Failed to delete {dl.destDir}");
                    Mod.mainModInstance.report.ReportPlugin(destination.orig, ModReport.ProblemType.ExceptionThrown, ex, "Temp Download Directory removal failed");
                }


            }
            else if (result == Collection.CheckoutResult.OK)
            {

            }
            else
            {
                // LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] WARN: No matching releases found for {this} - result = {result}");
                if (installationState == InstallationState.DoesNotExist)
                    yield return DownloadManager.repoDoesNotExist;
                else if (installationState == InstallationState.ReleaseNotFound)
                    yield return DownloadManager.releaseDoesNotExist;
                else if (installationState == InstallationState.NoUpdateAvailable)
                    yield return DownloadManager.notFound;
                else if (installationState == InstallationState.FetchingReleaseInfo)
                    yield return DownloadManager.branchNotFound;
                else
                    yield return DownloadManager.downloadError;
                         
                //installationState = InstallationState.ReleaseNotFound;
                //yield return DownloadManager.releaseDoesNotExist;
            }
            Log($"[{Versioning.FULL_PACKAGE_NAME}] Installer DoWork on {this} for mod {destination} DONE");
        }

        public override void Install(Loaded requiredBy, bool update)
        {
            IsTrue(installationState == InstallationState.Unknown || update,
                "GitHubRelease Install should only be requested for unknown items");
            /* FIXME: also enable it */
            DownloadManager.Install(requiredBy, this, update);
        }

        public override string ToString()
        {
            return "github:///" + author + "/" + repoName + (string.IsNullOrEmpty(branchName) ? string.Empty : "/" + branchName);
        }
    }
}
