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
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using static UnityEngine.Debug;
using static UnityEngine.Assertions.Assert;
using System.IO;
using System.Reflection;
using ColossalFramework;
using static ColossalFramework.PlatformServices.PlatformService;
using static ColossalFramework.Plugins.PluginManager;
using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ColossalFramework.IO;
using ColossalFramework.HTTP;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using GitHub;
using UnityEngine.SceneManagement;

namespace HarmonyManager
{
    internal static class _InstallerLite
    {
#if DEBUG
        public const int NETWORK_UNAVAILABLE_RETRY_DELAY = 5;
        public const int SERVICE_UNAVAILABLE_RETRY_DELAY = 5;
        public const int WEBREQUEST_TIMEOUT = 5;
#else
        /* Delay before retrying a WebRequest that failed to connect */
        public const int NETWORK_UNAVAILABLE_RETRY_DELAY = 10;

        /* Delay before retrying a WebRequest that failed due to service unavailable (server reported problem) */
        public const int SERVICE_UNAVAILABLE_RETRY_DELAY = 30;

        /* How long can a HTTP request take? */
        public const int WEBREQUEST_TIMEOUT = 60;
#endif

    }

    internal class InstallerLite : MonoBehaviour
    {
        internal static bool needsUpdate = false;
        internal static bool downloadSuccess = false;
        internal static MethodInfo InstallerRunMethod = default(MethodInfo);
        static bool skipConfirmation = false;
        static PluginManager.PluginInfo installInitiator = default(PluginManager.PluginInfo);
        static UnityEngine.Events.UnityAction<Scene, LoadSceneMode> onSceneLoaded = null;
        static SavedBool confirmation;
        YieldInstruction downloadError = new YieldInstruction();
        YieldInstruction saveError = new YieldInstruction();
        static object installer = null;
        static bool isWorkshopItem = false;
        static string homeDir = null;
        static string dlDir = null;
        static readonly PublishedFileId workshopHarmony = new PublishedFileId(2399343344);

#if BUGFIXED_RESILIENT_MOD_UNLOAD
        MethodInfo removePluginAtPath;
#endif
        public void Awake()
        {
            Log($"[{Versioning.FULL_PACKAGE_NAME}] InstallerLite.Awake() at \n{new System.Diagnostics.StackTrace(true)}");

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

            if (!skipConfirmation)
            {

                ConfirmPanel.ShowModal(title: "Harmony Installation", message: $"Install Harmony Locally (needed for {installInitiator.name})?", delegate (UIComponent comp, int ret)
                {
                    LogError($"[{Versioning.FULL_PACKAGE_NAME}] Confirmation : {ret}");
                    switch (ret)
                    {
                        case 1: /* yes */
                            confirmation.value = true;
                            InstallHarmony();
                            break;
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
        }

        void InstallHarmony()
        {
            string api = "https://api.github.com/repos/";
            string endpoint;

            if (!needsUpdate) // ie, if it's not an in-place update, it's a local install
            {
                dlDir = Path.Combine(DataLocation.tempFolder, Versioning.HarmonyInstallDir + ".dl");
                homeDir = Path.Combine(DataLocation.modsPath, Versioning.HarmonyInstallDir);
            }

            endpoint = "/releases/tags/" + Versioning.HARMONY_LEGACY_UPDATE_TAG;

            StartCoroutine(HttpGet(api + Versioning.HarmonyGithubDistributionURL + endpoint, true, null, HandleRelease));
        }

        private IEnumerable<YieldInstruction> HandleRelease(UnityWebRequest request, Hashtable assetUnused)
        {
            var releaseInfo = JSON.JsonDecode(Encoding.UTF8.GetString(request.downloadHandler.data)) as Hashtable;
            if (releaseInfo != null)
            {
                var assets = releaseInfo[_GitHubRelease.REL_ASSETS] as ArrayList;

                bool error = false;
                try
                {
                    Directory.CreateDirectory(dlDir);
                }
                catch (Exception ex) { LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR: Creating temp download directory: {ex.Message}"); error = true; }


                if (!error)
                {
                    var wantedFilename = needsUpdate ? Versioning.UPDATE_FILENAME : Versioning.INSTALL_FILENAME;
                    foreach (Hashtable asset in releaseInfo[_GitHubRelease.REL_ASSETS] as ArrayList)
                    {
                        string url = asset[_GitHubRelease.ASSET_DOWNLOAD_URL] as string;
                        if (url != null &&
                            (asset[_GitHubRelease.ASSET_CONTENT_TYPE] as string) == "application/zip" &&
                            (asset[_GitHubRelease.ASSET_FILENAME] as string).StartsWith(wantedFilename))
                        {
                            var download = HttpGet(url, false, asset, HandleDownload);
                            do
                            {
                                error = download.Current == saveError || download.Current == downloadError;
                                yield return download.Current;
                            } while (!error && download.MoveNext());

                            if (error)
                                break;
                        }
                    }
                    if (!error)
                    {
                        if (downloadSuccess)
                            InstallSuccess(releaseInfo);
                        else
                        {
                            LogError($"[{Versioning.FULL_PACKAGE_NAME}] Release assets not found ({wantedFilename})");
                            InstallFailed(); // Release not available
                        }
                    }
                }
            } else
            {
                var z = JSON.JsonDecode(Encoding.UTF8.GetString(request.downloadHandler.data));
                LogError($"[{Versioning.FULL_PACKAGE_NAME}] RELEASE NOT FOUND z={z != null}");
                if (z != null) {
                    LogError($"[{Versioning.FULL_PACKAGE_NAME}] RELEASE NOT FOUND z.type={z.GetType().FullName}");
                }
            }

            yield break;
        }

        void InstallAborted()
        {
            LogError($"[{Versioning.FULL_PACKAGE_NAME}] Install ABORTED");
            Harmony.OnInstallAborted();
        }
        void InstallSuccess(Hashtable release)
        {
            var myItemBranch = "G/" + Versioning.HarmonyGithubDistributionURL;

            SavedString installed = new SavedString(myItemBranch + "/installed", Settings.userGameState, string.Empty, true);
            installed.value = (release[_GitHubRelease.REL_TARGET] as string) + "/" + (release[_GitHubRelease.REL_TAG_NAME] as string);
            bool failed = false;
            if (dlDir != homeDir)
            {
                bool needsBackup = Directory.Exists(homeDir);
                var bakdir = isWorkshopItem ?
                    homeDir + ".bak"
                    : Path.Combine(DataLocation.tempFolder, Versioning.HarmonyInstallDir + ".bak");
                if (needsBackup)
                {
                    try { Directory.Move(homeDir, bakdir); }
                    catch (Exception ex) { LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR: Backup original Harmony Failed: {ex.Message}"); failed = true; }
                }

                if (!failed)
                {
                    try { Directory.Move(dlDir, homeDir); }
                    catch (Exception ex) { LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR: Installing new Harmony failed: {ex.Message}"); failed = true; }

                    if (isWorkshopItem)
                    {
                        new SavedBool(name: workshopHarmony.ToString() + homeDir.GetHashCode().ToString() + ".enabled",
                            fileName: Settings.userGameState,
                            def: false,
                            autoUpdate: true).value = true;
                    }
                    else
                    {
                        new SavedBool(name: Versioning.HarmonyInstallDir + homeDir.GetHashCode().ToString() + ".enabled",
                            fileName: Settings.userGameState,
                            def: false,
                            autoUpdate: true).value = true;
                    }

                    if (needsBackup)
                    {
                        try
                        {
                            if (failed)
                            {
                                Directory.Move(bakdir, homeDir);
                            }
                            else
                            {
                                Directory.Delete(bakdir, true);
                            }
                        }
                        catch (Exception ex) { LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR: Cleanup after Harmony install failed: {ex.Message}"); failed = true; }
                    }
                }
            }
            if (isWorkshopItem) {
                var LoadPluginAtPath = typeof(PluginManager).GetMethod("LoadPluginAtPath", BindingFlags.NonPublic | BindingFlags.Instance);
                LoadPluginAtPath?.Invoke(Singleton<PluginManager>.instance, new object[] { homeDir, false, workshopHarmony });
            }
        }
        private void InstallFailed()
        {
            LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] WARNING - {(needsUpdate ? "Updating" : "Installing")} Harmony FAILED");
            Harmony.OnUnavailable(Harmony.HarmonyUnavailableReason.InstallationFailed);
        }
        void InstallComplete()
        {
            Destroy(gameObject);
        }

        private IEnumerable<YieldInstruction> HandleDownload(UnityWebRequest request, Hashtable asset)
        {
            /* Unzip */

            bool error = false;
            try
            {
                var name = asset[_GitHubRelease.ASSET_FILENAME] as string;
                if (asset[_GitHubRelease.ASSET_CONTENT_TYPE] as string == "application/zip")
                {
                    new Zip(request.downloadHandler.data, name).UnzipTo(dlDir);
                    downloadSuccess = true;
                }
                else
                {
                    var filename = Path.Combine(dlDir, name);
                    File.WriteAllBytes(filename, request.downloadHandler.data);
                }
            }
            catch (Exception ex)
            {
                LogError($"[{Versioning.FULL_PACKAGE_NAME}] Handling downloaded {asset[_GitHubRelease.ASSET_FILENAME]} failed: {ex.Message} at\n{ex.StackTrace}");
                InstallFailed(); // Probably out of disk space, permissions, or similar
                error = true;
            }

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
                    request.SetRequestHeader("user-agent", user_agent);
                    var accept = isJson ? "application/vnd.github.v3+json" : "application/octet-stream";
                    request.SetRequestHeader("Accept", accept);
                    yield return request.Send();

                    switch (request.responseCode)
                    {
                        case 200:
                            foreach (var action in downloadHandler(request, asset))
                            {
                                yield return action;
                            }
                            done = true;
                            break;
                        case 403: /* Rate Limit */
                        case 502: /* bad gateway */
                        case 500: /* Internal server error */
                        case 503: /* Service unavailable */
                        case 504: /* gateway timeout */
                            LogError($"[{Versioning.FULL_PACKAGE_NAME}] will try again in {_InstallerLite.SERVICE_UNAVAILABLE_RETRY_DELAY}");
                            yield return new WaitForSeconds(_InstallerLite.SERVICE_UNAVAILABLE_RETRY_DELAY);
                            break;

                        default:
                            InstallFailed(); // Server will not give it.
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

        internal static void StartUpdates(object awarenessInstance)
        {
            needsUpdate = awarenessInstance.GetType().Assembly.GetName().Version < new Version(1, 0, 1, 0);

            if(needsUpdate)
            {
            string path = workshop.GetSubscribedItemPath(workshopHarmony);


                    homeDir = path;
                    dlDir = path + ".dl";
                    isWorkshopItem = true;

                Fuck_OFF_hijacker();
                    StartUpdate();
                }
            }
        internal static void Install(PluginManager.PluginInfo requestingPlugin, bool autoInstall)
        {
            skipConfirmation |= autoInstall;

            if (!skipConfirmation)
            {
                confirmation = new SavedBool(name: requestingPlugin.name + requestingPlugin.modPath.GetHashCode().ToString() + ".harmony",
                    fileName: Settings.userGameState,
                    def: false,
                    autoUpdate: true);
                skipConfirmation |= confirmation.value;
            }

            if (installInitiator == null && !autoInstall)
                installInitiator = requestingPlugin;

            if (installer == null && (skipConfirmation || UIView.library != null))
            {
                StartInstaller();
            }
            else if (UIView.library == null && onSceneLoaded == null)
            {
                onSceneLoaded = new UnityEngine.Events.UnityAction<Scene, LoadSceneMode>(OnSceneLoaded);
                SceneManager.sceneLoaded += onSceneLoaded;
            }

        }
        public static void OnSceneLoaded(Scene s, LoadSceneMode m)
        {
            if (UIView.library != null)
            {
                SceneManager.sceneLoaded -= onSceneLoaded;
                onSceneLoaded = null;
                if (Harmony.CO_Harmony_installer == null)
                {
                    StartInstaller();
                } else
                {
                    Log($"[{Versioning.FULL_PACKAGE_NAME}] (Harmony not found) Enabled Harmony Installer: {Harmony.CO_Harmony_installer.name}; Skipping my installation.");
                }
            }
        }

        internal static void Fuck_OFF_hijacker()
        {
            if (GameObject.Find("Harmony2SubscriptionWarning") is GameObject prompt)
            {
                GameObject.DestroyImmediate(prompt);
            }
            else
                new GameObject("Harmony2SubscriptionWarning").SetActive(false);
        }

        internal static void StartUpdate()
        {
            skipConfirmation = true; // If it needs update, no need for further confirmations

            StartInstaller();
        }

        internal static void StartInstaller()
        {
            IsNull(installer, "Updates should start only at init");

            GameObject gameObject = new GameObject("HarmonyInstallerObj");
            installer = gameObject.GetComponent("HarmonyInstaller");
            if (installer == null)
            {
                installer = gameObject.AddComponent<InstallerLite>();
                DontDestroyOnLoad(gameObject);
            }
        }
        internal static void CancelInstall()
        {
            if (installer != null)
            {
                var inst = installer as InstallerLite;
                inst.StopAllCoroutines();
                Destroy(inst.gameObject);
                installer = null;
            }
        }
    }
}
