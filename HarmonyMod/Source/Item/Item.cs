using System;
using System.Collections.Generic;
using static UnityEngine.Assertions.Assert;
using ColossalFramework.Packaging;

namespace HarmonyMod
{
    internal abstract class Item : Downloadable
    {
        public enum InstallationState
        {
            Unknown = 0,
            Installed,
            Builtin,
            Pending,
            DoesNotExist, /* Item does not exist */
            RateLimit,
            FetchingReleaseInfo,
            UpdateAvailable,
            ReleaseNotFound, /* Item exists, but not the specific version requested */
            NoUpdateAvailable,
            Downloading,
            Saving,
            InfoError,
            DownloadError,
            SaveError,


        }
        public HashSet<Loaded> requiredBy;

        public virtual ColossalFramework.PlatformServices.PublishedFileId fileId => ColossalFramework.PlatformServices.PublishedFileId.invalid;

        public abstract string typeID { get; }
        public abstract string authorID { get; set; }
        public abstract string authorName { get; set; }
        public abstract string repo { get; }
        public abstract string host { get; }
        public virtual string modDll { get; set; }
        public virtual bool isCameraScript { get; set; }
        public string assetCommit { get; protected set; }
        /// <summary>
        /// Local mods loaded before this harmony are marked with their default state, which comes from gameSettings.cgs
        /// </summary>
        /// <param name="active"></param>
        public DateTimeOffset? dateAdded { get; private set; }

        /* FIXME: tags is a union of all children's tags. It's only needed on Item for exporting to steam */
        public virtual string[] tags { get { throw new NotImplementedException(); } protected set { throw new NotImplementedException(); } }
        public bool hasThumbnail => m_thumbnail != null;
        protected string m_thumbnail;
        public virtual byte[] thumbnail => Convert.FromBase64String(m_thumbnail);

        // public virtual bool isBuiltIn { get => false; set { throw new NotImplementedException(); } }
        public bool isBuiltIn => installationState == InstallationState.Builtin;
        //public virtual bool isMissing => false;
        public virtual bool isMyWork { get => false; protected set { } }


        /// <summary>
        /// Branch name in the the author' repo (eg, beta, master, testing, etc)
        /// </summary>
        public virtual string branch { get { return null; } }

        public virtual string downloadToDir
        {
            get
            {
                return "_tmp_" + myBranchAsDirectory;
            }
        }

        string m_title;
        public virtual string title
        {
            get => installationState switch
            {
                // InstallationState.Installed => m_title,
                // InstallationState.Builtin => m_title,
                _ => (m_title == null ? ToString() : m_title) + " - " + installationState.ToString(),
            };
            set => m_title = value;
        }
        public abstract string version { get; protected set; }
        
        public abstract DateTimeOffset? updated { get; set;  }

        InstallationState m_installationState;
        public virtual InstallationState installationState { get => m_installationState;
            set {
                ValidateStateChange(this, value);
                //if (value == InstallationState.Downloading || value == InstallationState.DoesNotExist)
                //    LogError($"[{Versioning.FULL_PACKAGE_NAME}] BREAK - set {this} = {value}");

                m_installationState = value;
            }
        }
//        public virtual bool isInstalled => updated.HasValue;

        /// <summary>
        /// Install the item if necessary
        /// </summary>
        /// <param name="requiredBy"></param>
        /// <returns>True if install is successful, False if install is pendinding</returns>
        public abstract void Install(Loaded requiredBy, bool update);

        /// <summary>
        /// Sets the versionSpec to a fixed version, stored at the given Commit
        /// </summary>
        /// <param name="c"></param>
        // public abstract void Pin();
        // public virtual void Pin(bool pinned)
        // {
        //     UnityEngine.Assertions.Assert.IsNotNull(version,
        //         "Cannot when version is unset");
        // 
        //     m_versionSpec = pinned ? version : null;
        // }

        /// <summary>
        /// Base of branch name in the local repo (ex. "G/drok/Harmony-CitiesSkylines")
        /// The "target_commitish" will be appended to this for the release branch name
        /// </summary>
        public abstract string myItemBranch { get; }

        /// <summary>
        /// Complete branch name in the local repo (ex. "G/drok/Harmony-CitiesSkylines/master")
        /// including the "target_commitish"
        /// </summary>
        public abstract string myReleaseBranch { get; }

        /// <summary>
        /// Base subdirectory, with the OS path separator (ex. "G\drok\Harmony-CitiesSkylines" on Windows)
        /// The "target_commitish" will be appended to this for the full subdirectory name
        /// </summary>
        public abstract string myBranchAsDirectory { get; }


        // public virtual Package.AssetType assetType { get; set; }
        // public virtual Package.AssetType assetType { get; set; }
        public virtual Package.AssetType assetType { get; set; }

        public static Item ItemFromURI(string s)
        {
            var uri = new Uri(s);
            Item item;
            switch (uri.Scheme + ":" + uri.Host)
            {
                case "github:":
                case "https:github.com":
                    item = new GitHubRelease(uri);
                    break;
                default:
                    throw new ArgumentException($"Unsupported uri '{s}'");
            }
            return item;
        }
        protected string UriSegment(Uri uri, int i)
        {
            var slash = uri.Segments[i].IndexOf('/');
            return slash != -1 ? uri.Segments[i].Remove(slash) : uri.Segments[i];
        }

        public abstract override string ToString();

        public virtual void OnDownloadFailed(Loaded requestedBy, bool update, DownloadManager downloadManager, DownloadError error)
        {
            Mod.mainModInstance.report.ReportPlugin(requestedBy.orig, ModReport.ProblemType.GenericProblem, (update ? "Updating" : "Installing") + $" {this} failed: {error}");
            ColossalFramework.Singleton<ColossalFramework.Plugins.PluginManager>.instance.ForcePluginsChanged();

        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void ValidateStateChange(Item item, InstallationState newState)
        {
            if (item is RetryWait)
                return;

            List<InstallationState> from = new List<InstallationState>();
            switch (newState)
            {
                case InstallationState.Unknown:
                    break;
                case InstallationState.Installed:
                    if (item is GitHubRelease)
                        from.Add(InstallationState.Unknown);
                    from.Add(InstallationState.Saving);
                    break;
                case InstallationState.Pending:
                    from.Add(InstallationState.Unknown);
                    break;
                case InstallationState.Downloading:
                    from.Add(InstallationState.UpdateAvailable);
                    from.Add(InstallationState.NoUpdateAvailable); /* NeedAssets */
                    break;
                case InstallationState.DoesNotExist:
                case InstallationState.RateLimit:
                case InstallationState.InfoError:
                case InstallationState.NoUpdateAvailable:
                case InstallationState.UpdateAvailable:
                case InstallationState.ReleaseNotFound:
                    {
                        from.Add(InstallationState.FetchingReleaseInfo); /* Others start from FetchingReleaseInfo */
                        from.Add(InstallationState.RateLimit);
                    }
                    break;
                case InstallationState.FetchingReleaseInfo:
                    from.Add(InstallationState.Pending);
                    break;
                case InstallationState.Saving:
                case InstallationState.DownloadError:
                    from.Add(InstallationState.Downloading);
                    break;
                case InstallationState.SaveError:
                    from.Add(InstallationState.Saving);
                    break;
            }
            IsTrue(from.Contains(item.installationState),
                $"{item} should not go {item.installationState} -> {newState}");
        }
    }

}
