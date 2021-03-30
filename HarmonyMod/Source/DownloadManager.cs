/*
 * Harmony for Cities Skylines
 *  Copyright (C) 2021 Radu Hociung <radu.csmods@ohmi.org>
 *  
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the modified GNU General Public License as
 *  published in the root directory of the source distribution.
 *  
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  modified GNU General Public License for more details.
 *  
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using static UnityEngine.Assertions.Assert;
using static UnityEngine.Debug;
using System.IO;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.IO;
using Priority_Queue;

namespace HarmonyMod
{
    internal static class _DownloadManager
    {
#if DEBUG
        public const int WORKSHOP_INSTALL_TIMEOUT = 10;
        public const int NETWORK_UNAVAILABLE_RETRY_DELAY = 5;
        public const int SERVICE_UNAVAILABLE_RETRY_DELAY = 5;
        public const int STEAM_INTERNAL_OUTAGE_RETRY_DELAY = 5;
        public const int STEAM_RATELIMIT_RETRY_DELAY = 30;
        public const int WEBREQUEST_TIMEOUT = 5;
        public const int STEAM_MAX_SUBSCRIBE_RETRIES = 10;

#else
        /* How many seconds can Steam take to install a subscribed item? */
        public const int WORKSHOP_INSTALL_TIMEOUT = 120;

        /* Delay before retrying a WebRequest that failed to connect */
        public const int NETWORK_UNAVAILABLE_RETRY_DELAY = 10;

        /* Delay before retrying a WebRequest that failed due to service unavailable (server reported problem) */
        public const int SERVICE_UNAVAILABLE_RETRY_DELAY = 30;

        /* Delay before retrying a request to Steam that failed do to Steam-internal network outages */
        public const int STEAM_INTERNAL_OUTAGE_RETRY_DELAY = 30;

        /* Delay before retrying a request to Steam that failed do to Steam rate limiting */
        public const int STEAM_RATELIMIT_RETRY_DELAY = 30;

        /* How long can a HTTP request take? */
        public const int WEBREQUEST_TIMEOUT = 60;

        /* How many times to retry workshop subscriptions on failure? */
        public const int STEAM_MAX_SUBSCRIBE_RETRIES = 10;
        public const int STEAM_SUBSCRIBE_RETRY_DELAY = 10;
#endif

    }
    internal class DownloadManager : MonoBehaviour
    {
        // const string RATELIMIT_FILE = "GRateLimit";

        // const string HarmonyGithubDistributionURL = "drok/Harmony-CitiesSkylines";
        // const string HarmonyGithubDistributionURL = "drok/reltest";
        // const string HarmonyInstallDir = "000-HarmonyMod";
        // static string requestingMod = string.Empty;

        //MethodInfo pluginManagerFSHanderInstaller;
        MethodInfo loadPluginAtPath;
#if BUGFIXED_RESILIENT_MOD_UNLOAD
        MethodInfo removePluginAtPath;
#endif
#if USE_VANILLA_REPORTERS
        FileSystemReporter folderReporter;
        FileSystemReporter modsReporter;
        FileSystemReporter sourcesReporter;
#endif

        static public YieldInstruction saveError = new DownloadError("Saving failed (disk full or permissions?)");
        static public YieldInstruction downloadError = new DownloadError("Downloading failed");
        static public YieldInstruction notFound = new DownloadError("Not Found");
        static public YieldInstruction branchNotFound = new DownloadError("Release channel no longer exists");
        static public YieldInstruction notModified = new DownloadError("No new releases");
        static public YieldInstruction repoDoesNotExist = new DownloadError("Repository does not exist");
        static public YieldInstruction releaseDoesNotExist = new DownloadError("Release does not exist");

        public class Work
        {
            public Item item;
            public Loaded destination;
            public bool update;
            public override string ToString()
            {
                return "Downloading " + item.ToString() + " to " + (destination == null ? "mod manager" : destination.ToString());
            }
        }

        object workQueueLock;
        SimplePriorityQueue<Work> workQueue;
        HashSet<Item> allDownloads;


        // Collection downloadTempLocation = default(Collection);
        // string tempRepoDir = default(string);
        // string destDir = default(string);

        public void Awake()
        {
            loadPluginAtPath = typeof(PluginManager)
                .GetMethod("LoadPluginAtPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

            workQueueLock = new object();
            workQueue = new SimplePriorityQueue<Work>();
            allDownloads = new HashSet<Item>();

#if BUGFIXED_RESILIENT_MOD_UNLOAD
            removePluginAtPath = typeof(PluginManager)
                .GetMethod("RemovePluginAtPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] removePluginAtPath={removePluginAtPath}");
#endif
        }

        public void Start()
        {
            PluginManager pmInst = Singleton<PluginManager>.instance;
            StartCoroutine(DoWork());
        }

        public void OnDisable()
        {
            StopAllCoroutines();
            Cleanup();
        }
        string HomeOfItem(string baseDir, string fullBranchName)
        {
            /* given a fullBranchName = "G/Mod/drok/Harmony-CitiesSkylines/testing/big-feature"
             * the output is "testing/big-feature"
             */
            int indexOfAssetName = 1;
            for (int i = 0; i < 4; ++i) { indexOfAssetName = fullBranchName.IndexOf('/', indexOfAssetName) + 1; }
            var assetBranch = fullBranchName.Substring(indexOfAssetName);
            var branchDir = new string(assetBranch.Where(c => !char.IsPunctuation(c) && !char.IsSymbol(c)).ToArray());

            var parts = fullBranchName.Split('/');
            return Path.Combine(
                    Path.Combine(
                        Path.Combine(
                            Path.Combine(
                                Path.Combine(baseDir, parts[0]), /* typeID */
                                parts[1]), /* Item kind (Mod, Asset) */
                            parts[2]), /* author */
                        parts[3]), /* Repo */
                branchDir);
        }

        static DownloadManager()
        {
            var unitySdk =
                Path.Combine(
                    Path.Combine(
                        Path.Combine(
                            Path.Combine(DataLocation.applicationBase, "Mono"),
                            "lib"),
                        "mono"),
                    "unity");
#if HEAVY_TRACE
            Log($"[{Versioning.FULL_PACKAGE_NAME}] Installer.Awake() will load I18N form {unitySdk}");
#endif
            Assembly.LoadFrom(Path.Combine(unitySdk, "I18N.dll"));
            Assembly.LoadFrom(Path.Combine(unitySdk, "I18N.West.dll"));
        }

        bool DoOneStep(IEnumerator<YieldInstruction> actions)
        {
            try
            {
                return actions.MoveNext();
            }
            catch (Exception ex)
            {
                Mod.SelfReport(IAwareness.SelfProblemType.FailedToInitialize, ex);
                return false;
            }
        }

        public void ResetDownloadData()
        {
        }
        private IEnumerator<YieldInstruction> DoWork()
        {
            while (true)
            {
                Work work = default(Work);
                bool hasWork = false;
                lock (workQueueLock)
                {
                    if (workQueue.Count == 0)
                        break;
                    else
                    {
                        work = workQueue.Dequeue();
                        hasWork = true;
                    }
                }

                if (hasWork)
                {
                    bool error = false;

                    work.item.installationState = Item.InstallationState.FetchingReleaseInfo;

                    var actions = work.item.FetchReleaseInfo(work.update, work.destination, this);
                    while (DoOneStep(actions))
                    {
                        if (actions.Current is DownloadError dlerror)
                        {
                            LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] WARN: {work} failed: {dlerror}");
                            work.item.OnDownloadFailed(work.destination, work.update, this, dlerror);
                            error = true;
                            break;
                        }
                        else if (actions.Current is TemporaryError delay)
                        {
                            lock (workQueueLock)
                            {
                                // yield return new WaitForSeconds(delay);
                                var reWork = RetryWait.GetRetryWork(delay.waitTill, work, actions);
#if TRACE
                                Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Re-queuing {reWork} due to {delay}");
#endif
                                // reWork.item.installationState = Item.InstallationState.Pending;
                                workQueue.Enqueue(reWork, (float)(reWork.item as RetryWait).delay);
                                break;
                            }
                        }
#if HEAVY_TRACE
                        Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - download.Current={(actions.Current != null ? actions.Current.GetType() : "null")} error={error}");
#endif
                        yield return actions.Current;
                    }
#if HEAVY_TRACE
                    Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Downloader finished {work}");
#endif
                    /* Fixme: Why is Cleanup() needed? test those scenarios */
                    Cleanup();
                    continue;
                }
                break;
            }

#if DEBUG
            Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Downloader finished all work");
#endif
        }

        void Cleanup()
        {
#if DEBUG
            Mod.mainModInstance.repo.Cleanup();
            Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Cleanup download repo");
#endif
        }

        public IEnumerator<YieldInstruction> HttpGet(string url, string accept, object destination, string etag, object context, Func<UnityWebRequest, object, object, IEnumerable<YieldInstruction>> downloadHandler)
        {
            var download = HttpGetPost(url, accept, null, destination, etag, context, downloadHandler);
            while (download.MoveNext()) yield return download.Current;
        }

        public IEnumerator<YieldInstruction> HttpPost(string url, string accept, Dictionary<string, string> formFields, object destination, string etag, object context, Func<UnityWebRequest, object, object, IEnumerable<YieldInstruction>> downloadHandler)
        {
            var items = formFields.Values.Aggregate(string.Empty, (a, v) => a + "," + v);
#if HEAVY_TRACE
            Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - POST values {items}");
#endif
            var download = HttpGetPost(url, accept, formFields, destination, etag, context, downloadHandler);
            while (download.MoveNext()) yield return download.Current;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="isJson"></param>
        /// <param name="destination"></param>
        /// <param name="etag">Etag to use with request. If null, check HEAD of web page. If Empty, use no Etag</param>
        /// <param name="context"></param>
        /// <param name="downloadHandler"></param>
        /// <returns></returns>
        IEnumerator<YieldInstruction> HttpGetPost(string url, string accept, Dictionary<string, string> formFields, object destination, string etag, object context, Func<UnityWebRequest, object, object, IEnumerable<YieldInstruction>> downloadHandler)
        {

#if HEAVY_TRACE
            Log($"[{Versioning.FULL_PACKAGE_NAME}] Fetch {(formFields == null ? "GET" : "POST")} {url} {(formFields!=null ? formFields.Count : 0)} formFields");
#endif
            using (UnityWebRequest request = formFields == null ? UnityWebRequest.Get(url) : UnityWebRequest.Post(url, formFields))
            {
                var user_agent = Uri.EscapeUriString(Versioning.FULL_PACKAGE_NAME + " ") +
                    "&#40" +
                    Uri.EscapeUriString(Versioning.ISSUES_URL) +
                    "&#41";
                // Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] Setting User-Agent: {user_agent}");
                request.SetRequestHeader("user-agent", user_agent);
//                 Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] User-Agent OK");
                //                var accept = isJson ? "application/vnd.github.v3+json" : "application/octet-stream";
                // Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] Setting Accept: {accept}");
                request.SetRequestHeader("Accept", accept);
                // Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] Accept OK");
                // Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] sending web req {request.url} from\n{new System.Diagnostics.StackTrace(true)}");

                request.timeout = _DownloadManager.WEBREQUEST_TIMEOUT;
                // request.redirectLimit = 0;
                // request.downloadHandler = new TestDownloader();

                if (!string.IsNullOrEmpty(etag))
                    request.SetRequestHeader("If-None-Match", etag);

                yield return request.Send();

                if (request.isError)
                {
                    LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] WARN - Failed to send request {url}");
                    yield return new TemporaryError($"Error Requesting {request.url} - {request.error}", new TimeSpan(0, 0, _DownloadManager.NETWORK_UNAVAILABLE_RETRY_DELAY));
                }
                else
                {
#if HEAVY_TRACE
                    Log($"[{Versioning.FULL_PACKAGE_NAME}] WWW got code {request.responseCode} for {request.url} @ {DateTime.Now}");
#endif

                    switch (request.responseCode)
                    {
                        case 200:
                            // yield return (YieldInstruction)DownloadHandler(request, request.GetResponseHeader("Etag"));
                            foreach (var action in downloadHandler(request, destination, context))
                            {
                                yield return action;
                            }
                            // {
                            //     InstallFailed();
                            // }
                            break;
                        case 304:
#if HEAVY_TRACE
                            Log($"[{Versioning.FULL_PACKAGE_NAME}] There is no new release at {url}");
#endif
                            yield return DownloadManager.notModified;
                            break;
                        case 302:
#if HEAVY_TRACE
                            Log($"[{Versioning.FULL_PACKAGE_NAME}] Received redirect to {request.GetResponseHeader("Location")}");
#endif
                            yield return new Redirect(request.GetResponseHeader("Location"));
                            break;
                        case 403: /* Rate Limit .. fixme calculate exact wait time */
                            int rateLimitReset;
                            if (request.GetResponseHeader("X-RateLimit-Reset") is string resetStr)
                            {
                                rateLimitReset = int.Parse(resetStr);
                                yield return new TemporaryError($"RateLimit for {request.url}", rateLimitReset);
                            }
                            else
                            {
                                yield return downloadError;
                            }
                            break;
                        case 502: /* bad gateway */
                        case 500: /* Internal server error */
                        case 503: /* Service unavailable */
                        case 504: /* gateway timeout */
                            int delay = 30;
#if HEAVY_TRACE
                            LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] WARN - will try again in {delay}");
#endif
                            yield return new TemporaryError($"HTTP response {request.responseCode} for {request.url})", new TimeSpan(0, 0, _DownloadManager.SERVICE_UNAVAILABLE_RETRY_DELAY));
                            break;

                        case 404: /* Positively does not exist */
                        default:
                            LogError($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Other Download Error HTTP.responseCode={request.responseCode}");
                            yield return notFound;
                            break;
                    }
                }
            }
        }

        public IEnumerator<YieldInstruction> HttpHead(string url)
        {

            bool done = false;
            do
            {
#if HEAVY_TRACE
                Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO: Fetch HEAD {url}");
#endif
                using (UnityWebRequest request = UnityWebRequest.Head(url))
                {
                    var user_agent = Uri.EscapeUriString(Versioning.FULL_PACKAGE_NAME + " ") +
                        "&#40" +
                        Uri.EscapeUriString(Versioning.ISSUES_URL) +
                        "&#41";
                    request.SetRequestHeader("user-agent", user_agent);
                    request.SetRequestHeader("Accept", "*/*");
                    request.redirectLimit = 0;

                    yield return request.Send();

#if HEAVY_TRACE
                    Log($"[{Versioning.FULL_PACKAGE_NAME}] WWW got code {request.responseCode} for {request.url}");
#endif

                    switch (request.responseCode)
                    {
                        case 200:
                            done = true;
                            break;
                        case 302:
#if HEAVY_TRACE
                            Log($"[{Versioning.FULL_PACKAGE_NAME}] Received redirect to {request.GetResponseHeader("Location")}");
#endif
                            done = true;
                            yield return new Redirect(request.GetResponseHeader("Location"));
                            break;
                        case 403: /* Rate Limit .. fixme calculate exact wait time */
                        case 502: /* bad gateway */
                        case 500: /* Internal server error */
                        case 503: /* Service unavailable */
                        case 504: /* gateway timeout */
                            int delay = 5;
                            LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] WARN - will try again in {delay}");
                            yield return new WaitForSeconds(delay);
                            break;

                        case 404: /* Positively does not exist */
                            done = true;
                            yield return DownloadManager.notFound;
                            break;
                        default:
                            done = true;
                            yield return DownloadManager.downloadError;
                            break;
                    }
                }
            } while (!done);
        }

        public void Enqueue(Loaded destination, Item item, bool update)
        {
#if HEAVY_TRACE
            Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Installer enqueue {item} from \n{ new System.Diagnostics.StackTrace(true)}");
#endif
            lock (workQueueLock)
            {
                workQueue.Enqueue(new Work(){ item = item, destination = destination, update = update, }, 0f);
            }
            item.installationState = Item.InstallationState.Pending;
            allDownloads.Add(item);
        }

        static public void Install(Loaded callbackModInfo, Item item, bool update)
        {

            IsTrue((item.installationState == Item.InstallationState.Unknown && update) || 
                (false),
                "Item Install should only be requested for unknown items");

            var installer = Mod.mainModInstance.gameObject.GetComponent<DownloadManager>();

            if (installer == null)
            {
                Mod.mainModInstance.gameObject.SetActive(false);
                installer = Mod.mainModInstance.gameObject.AddComponent<DownloadManager>();

                installer.enabled = false;
                Mod.mainModInstance.gameObject.SetActive(true);
                installer.Enqueue(callbackModInfo, item, update);
                installer.enabled = true;
            } else
            {
                installer.Enqueue(callbackModInfo, item, update);
            }
        }

        static public bool IsDownloading()
        {
            var installer = Mod.mainModInstance.gameObject.GetComponent<DownloadManager>();
            return installer != null;
        }

        static public IEnumerable<Item> GetQueuedItems()
        {
            var installer = Mod.mainModInstance.gameObject.GetComponent<DownloadManager>();
            if (installer != null)
            {
                return installer.allDownloads;
            }
            return Enumerable.Empty<Item>();
        }

#if HEAVY_TRACE
        public void OnDestroy()
        {
            Log($"[{Versioning.FULL_PACKAGE_NAME}] Installer.OnDestroy");
        }
#endif


    }
}
