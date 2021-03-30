using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using static UnityEngine.Debug;
using System.IO;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ColossalFramework.IO;
using ColossalFramework.HTTP;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

namespace HarmonyManager
{
    internal class InstallerLite : MonoBehaviour
    {
        static bool skipConfirmation = false;
        static PluginManager.PluginInfo requestingMod = default(PluginManager.PluginInfo);
        static SavedBool confirmation;
        YieldInstruction downloadError = new YieldInstruction();
        YieldInstruction saveError = new YieldInstruction();

        //MethodInfo pluginManagerFSHanderInstaller;
        // MethodInfo loadPluginAtPath;
#if BUGFIXED_RESILIENT_MOD_UNLOAD
        MethodInfo removePluginAtPath;
#endif
        public void Awake()
        {
            Log($"[{Versioning.FULL_PACKAGE_NAME}] InstallerLite.Awake() at \n{new System.Diagnostics.StackTrace(true)}");

            // loadPluginAtPath = typeof(PluginManager)
            //     .GetMethod("LoadPluginAtPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            // Log($"[{Versioning.FULL_PACKAGE_NAME}] loadPluginAtPath={loadPluginAtPath}");

            var unitySdk =
                Path.Combine(
                    Path.Combine(
                        Path.Combine(
                            Path.Combine(DataLocation.applicationBase, "Mono"),
                            "lib"),
                        "mono"),
                    "unity");
            Log($"[{Versioning.FULL_PACKAGE_NAME}] Installer.Awake() will load I18N form {unitySdk}");

            Assembly.LoadFrom(Path.Combine(unitySdk, "I18N.dll"));
            Assembly.LoadFrom(Path.Combine(unitySdk, "I18N.West.dll"));


#if BUGFIXED_RESILIENT_MOD_UNLOAD
            removePluginAtPath = typeof(PluginManager)
                .GetMethod("RemovePluginAtPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] removePluginAtPath={removePluginAtPath}");
#endif
        }

        public void Start()
        {
            PluginManager pmInst = Singleton<PluginManager>.instance;

            //var confirmation = UIView.library.ShowModal<ConfirmPanel>("ExitConfirmPanel", delegate (UIComponent comp, int ret)
            if (!skipConfirmation)
            {
                ConfirmPanel.ShowModal(title: "Harmony Installation", message: $"Install Harmony Locally (needed for {requestingMod.name})?", delegate (UIComponent comp, int ret)
                {
                    LogError($"[{Versioning.FULL_PACKAGE_NAME}] Confirmation : {ret}");
                    switch (ret)
                    {
                        case 1: /* yes */
                            confirmation.value = true;
                            InstallHarmony();
                            break;
                        //                    case 1: /* no */
                        //                        StartCoroutine(HttpGet(api + HarmonyGithubDistributionURL + "/releases/latest", HandleDownload));
                        //                        break;
                        default:
                            InstallAborted();
                            break;
                    }
                });
            }
            else
            {
                /* If OnEnable() runs before UIView is ready, it was
                 * enabled previously (ie, the Confirmation is already positive).
                 * Proceed with installation.
                 */
                InstallHarmony();
            }
            // confirmation.SetMessage(
            //     message: "Install Harmony Locally?",
            //     title: "Harmony Installation");
        }

        void InstallHarmony()
        {
            string api = "https://api.github.com/repos/";
            string endpoint = "/releases/tags/" + Versioning.HARMONY_RELEASE_TAG;
            StartCoroutine(HttpGet(api + Versioning.HarmonyGithubDistributionURL + endpoint, true, null, HandleRelease));
        }

        private IEnumerable<YieldInstruction> HandleRelease(UnityWebRequest request, Hashtable assetUnused)
        {
            // string etag = request.GetResponseHeader("Etag");
            var releaseInfo = JSON.JsonDecode(Encoding.UTF8.GetString(request.downloadHandler.data)) as Hashtable;


            // string s = string.Empty;
            // foreach (var i in releaseInfo.Keys)
            // {
            //     s += i + " => " + releaseInfo[i] + "\n";
            // }
            // Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] got response: {s}");

            //             Release rel = JsonUtility.FromJson<Release>(Encoding.UTF8.GetString(request.downloadHandler.data));
            if (releaseInfo != null)
            {
                Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] found release {releaseInfo["id"]} = {releaseInfo["name"]}");
                var assets = releaseInfo["assets"] as ArrayList;
                Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] found {assets.Count} assets in release {releaseInfo["name"]}");

                var destdir = Path.Combine(DataLocation.modsPath, Versioning.HarmonyInstallDir);

#if BUGFIXED_RESILIENT_MOD_UNLOAD
                if (Directory.Exists(destdir))
                {
                    /*
                     * CO's plugin manager fails if the mod being removed is corrupt and
                     * throws ReflectionTypeLoadException
                     * Thus attempting to do a clean unload is futile unless a cleaner plugin manager
                     * is present.
                     */
                    try
                    {
                        UnityEngine.Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] Removing existing plugin at {destdir}");
                        removePluginAtPath.Invoke(Singleton<PluginManager>.instance, new object[] { destdir, });

                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] loading plugin threw: {ex.GetType().Name}: {ex.Message}");
                    }
                }
#endif
                var destDir = Path.Combine(DataLocation.tempFolder, Versioning.HarmonyInstallDir);
                bool error = false;
                try
                {
                    Directory.CreateDirectory(destDir);
                }
                catch (Exception ex) { LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR: Creating temp download directory: {ex.Message}"); error = true; }


                if (!error)
                {
                    foreach (Hashtable asset in releaseInfo["assets"] as ArrayList)
                    {
                        string url = asset["browser_download_url"] as string;
                        if (url != null && (asset["content_type"] as string) == "application/zip")
                        {
                            LogError($"[{Versioning.FULL_PACKAGE_NAME}] found asset {asset["id"]} type = {asset["content_type"]} name = {asset["name"]}");

                            var download = HttpGet(url, false, asset, HandleDownload);
                            do
                            {
                                error = download.Current == saveError || download.Current == downloadError;
                                LogError($"[{Versioning.FULL_PACKAGE_NAME}] download.Current={download.Current != null} error={error}");
                                yield return download.Current;
                            } while (!error && download.MoveNext());

                            LogError($"[{Versioning.FULL_PACKAGE_NAME}] after loop download.Current={download.Current != null} error={error}");

                            // if (download.Current == null)
                            //     break;
                            // foreach (var action in HttpGet(url, false, HandleDownload))
                            // {
                            //     yield return action;
                            // }

                            // foreach (var dl in HttpGet(url, false, HandleDownload))
                            // {
                            //     yield return dl;
                            // }
                            if (error)
                                break;
                        }
                    }
                }
                if (!error)
                {
                    InstallSuccess(destdir, releaseInfo);
                }


#if false
                Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] json found release {rel.id}  assets={rel.assets != null} test_ints={rel.test_ints != null} ta={rel.ta != null} ta1={rel.ta1 != null}" +
                    $"\n{Encoding.UTF8.GetString(request.downloadHandler.data)}");
                if (rel.test_ints != null)
                {
                    Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] json found {rel.test_ints.Count} test_ints");
                }
                if (rel.ta != null)
                {
                    Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] json found {rel.ta.Count} TA");
                }
                if (rel.ta != null)
                {
                    Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] json found {rel.ta1.id} TA1");
                }
                if (rel.assets != null)
                {
                    Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] found {rel.assets.Count} assets");
                    foreach (var asset in rel.assets.Where(asset => asset.content_type == "application/zip"))
                    {
                        Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] will download asset {asset.id}");
                        // request.url = asset.Url;
                        HttpGet(asset.url, false, HandleDownload);
                        assetFound = true;
                        // yield return request.Send();
                        // StartCoroutine(HttpGet(asset.Url, HandleDownload, false));
                    }
                }
#endif
            } else
            {
                var z = JSON.JsonDecode(Encoding.UTF8.GetString(request.downloadHandler.data));
                LogError($"[{Versioning.FULL_PACKAGE_NAME}] RELEASE NOT FOUND z={z != null}");
                if (z != null) {
                    LogError($"[{Versioning.FULL_PACKAGE_NAME}] RELEASE NOT FOUND z.type={z.GetType().FullName}");
                }
            }


            // return assetFound;
            //}
            yield break;
        }

        void InstallAborted()
        {
            LogError($"[{Versioning.FULL_PACKAGE_NAME}] Install ABORTED");
            Harmony.OnInstallAborted();
        }
        void InstallSuccess(string modHome, Hashtable release)
        {
            var dlDir = Path.Combine(DataLocation.tempFolder, Versioning.HarmonyInstallDir);
            var myItemBranch = "G/" + Versioning.HarmonyGithubDistributionURL;

            SavedString installed = new SavedString(myItemBranch + "[installed]", Settings.userGameState, string.Empty, true);
            installed.value = myItemBranch + "/" + (release["target_commitish"] as string) + "/" + (release["tag_name"] as string);

            bool needsBackup = Directory.Exists(modHome);
            bool failed = false;
            var bakdir = Path.Combine(DataLocation.tempFolder, Versioning.HarmonyInstallDir + ".bak");
            if (needsBackup)
            {
                try { Directory.Move(modHome, bakdir); }
                catch (Exception ex) { LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR: Backup original Harmony Failed: {ex.Message}"); failed = true; }
            }

            if (!failed)
            {
                try { Directory.Move(dlDir, modHome); }
                catch (Exception ex) { LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR: Installing new Harmony failed: {ex.Message}"); failed = true; }

                if (needsBackup)
                {
                    try
                    {
                        if (failed)
                        {
                            Directory.Move(bakdir, modHome);
                        }
                        else
                        {
                            Directory.Delete(bakdir, true);
                        }
                    }
                    catch (Exception ex) { LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR: Cleanup after Harmony install failed: {ex.Message}"); failed = true; }
                }
            }
            if (!failed)
                LogError($"[{Versioning.FULL_PACKAGE_NAME}] Install SUCCESS");

        }
        private void InstallFailed()
        {
            LogError($"[{Versioning.FULL_PACKAGE_NAME}] Install FAILED 6 {Assembly.GetExecutingAssembly().GetName().Version}");
            Harmony.OnUnavailable(Harmony.HarmonyUnavailableReason.InstallationFailed);
        }
        void InstallComplete()
        {
            LogError($"[{Versioning.FULL_PACKAGE_NAME}] Install Complete {Assembly.GetExecutingAssembly().GetName().Version}");
            Destroy(gameObject);
        }

        private IEnumerable<YieldInstruction> HandleDownload(UnityWebRequest request, Hashtable asset)
        {
            //            var cwd = Directory.GetCurrentDirectory();
            /* Unzip */
            string etag = request.GetResponseHeader("Etag");
#if TRACE
            Log($"[{Versioning.FULL_PACKAGE_NAME}] received download {etag} (type={request.GetResponseHeader("Content-Type")} asset-type={asset["content_type"]} {request.downloadHandler.data.Length} bytes) as {asset["name"] ?? "null"}");
#endif

            bool error = false;
            try
            {
                var destDir = Path.Combine(DataLocation.tempFolder, Versioning.HarmonyInstallDir);

                if (asset["content_type"] as string == "application/zip")
                {
#if TRACE
                    Log($"[{Versioning.FULL_PACKAGE_NAME}] Should unzip {name} to {destDir}");
#endif
                    new Zip(request.downloadHandler.data, name).UnzipTo(destDir);
                }
                else
                {
                    var filename = Path.Combine(destDir, name);
                    File.WriteAllBytes(filename, request.downloadHandler.data);
                }
            }
            catch (Exception ex)
            {
                LogError($"[{Versioning.FULL_PACKAGE_NAME}] Handling downloaded {asset["name"]} failed: {ex.Message} at\n{ex.StackTrace}");
                InstallFailed();
                error = true;
            }

//#if TRACE
            // yield return new WaitForSeconds(5);
//#endif
            if (error)
                yield return saveError;
        }

        private IEnumerator<YieldInstruction> HttpGet(string url, bool isJson, Hashtable asset, Func<UnityWebRequest, Hashtable, IEnumerable<YieldInstruction>> downloadHandler)
        {

            bool done = false;
            do
            {
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    var user_agent = Uri.EscapeUriString(Versioning.FULL_PACKAGE_NAME + " ") +
                        "&#40" +
                        Uri.EscapeUriString(Versioning.ISSUES_URL) +
                        "&#41";
                    Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] Setting User-Agent: {user_agent}");
                    request.SetRequestHeader("user-agent", user_agent);
                    Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] User-Agent OK");
                    var accept = isJson ? "application/vnd.github.v3+json" : "application/octet-stream";
                    Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] Setting Accept: {accept}");
                    request.SetRequestHeader("Accept", accept);
                    Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] Accept OK");
                    Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] sending web req {request.url} from\n{new System.Diagnostics.StackTrace(true)}");
                    yield return request.Send();

                    Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] WWW got code {request.responseCode} for {request.url}");

                    switch (request.responseCode)
                    {
                        case 200:
                            // yield return (YieldInstruction)DownloadHandler(request, request.GetResponseHeader("Etag"));
                            foreach (var action in downloadHandler(request, asset))
                            {
                                yield return action;
                            }
                            // {
                            //     InstallFailed();
                            // }
                            done = true;
                            break;
                        //case 302:
                        //    request.url = request.GetResponseHeader("Location");
                        //    done = request.url == null;
                        //    if (!done)
                        //    {
                        //        Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] WWW got redirect to {request.url} - following");
                        //    }
                        //    break;
                        case 403: /* Rate Limit */
                        case 502: /* bad gateway */
                        case 500: /* Internal server error */
                        case 503: /* Service unavailable */
                        case 504: /* gateway timeout */
                            int delay = 5;
                            LogError($"[{Versioning.FULL_PACKAGE_NAME}] will try again in {delay}");
                            yield return new WaitForSeconds(delay);
                            break;

                        default:
                            InstallFailed();
                            done = true;
                            yield return downloadError;
                            break;
                    }

                    if (isJson)
                    {
                        InstallComplete();
                    }
                }
            } while (!done);
        }

        internal static void Install(ref object installer, PluginManager.PluginInfo plugin, bool autoInstall)
        {
            skipConfirmation |= autoInstall;

            if (!skipConfirmation)
            {
                confirmation = new SavedBool(name: plugin.name + plugin.modPath.GetHashCode().ToString() + ".harmony",
                    fileName: Settings.userGameState,
                    def: false,
                    autoUpdate: true);
                skipConfirmation |= confirmation.value;
            }

            if (installer == null && (skipConfirmation || UIView.library != null))
            {
                requestingMod = plugin;
                GameObject gameObject = new GameObject("HarmonyInstallerObj");
                installer = gameObject.GetComponent("HarmonyInstaller");
                if (installer == null)
                {
                    installer = gameObject.AddComponent<InstallerLite>();
                    DontDestroyOnLoad(gameObject);
                }
            }
        }
        internal static void CancelInstall(ref object installer)
        {
            if (installer != null)
            {
                var inst = installer as InstallerLite;
#if TRACE
                Log($"[{Versioning.FULL_PACKAGE_NAME}] Installer.CancelInstall");
#endif
                inst.StopAllCoroutines();
                Destroy(inst.gameObject);
                installer = null;
            }
        }

        public void OnDestroy()
        {
#if TRACE
            Log($"[{Versioning.FULL_PACKAGE_NAME}] Installer.OnDestroy");
#endif
        }

    }
}
