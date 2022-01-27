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
using JetBrains.Annotations;
using ICities;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Plugins;
using ColossalFramework.PlatformServices;
using static ColossalFramework.Plugins.PluginManager;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Debug;
using static UnityEngine.Assertions.Assert;
#if !INSTALLER
using Harmony2::HarmonyLib;
#endif
using HarmonyMod;

namespace HarmonyInstaller
{


#if INSTALLER
    public class Mod : IUserMod
#else
    public class Mod : IUserMod, IAmAware, ILoadingExtension
#endif
    {

#if DEVELOPER
        internal const string RECOMMENDED_LOCAL_HELPER_DIRNAME = "000-" + Versioning.PACKAGE_NAME + "-HELPER";
#elif MODMANAGER
        internal const string RECOMMENDED_LOCAL_HELPER_DIRNAME = "000-" + Versioning.PACKAGE_NAME + "-ModManager";
#endif
#if TRACE
        internal static int instancenum = 0;
#endif
        internal const string SettingsFile = Versioning.PACKAGE_NAME;

#region IUserMod Name and Description
        public string Name {
            get {
                /* FIXME: Find a more elegant way to call isEnabled=true
                 * Cannot be called from the static constructor because it runs inside get_userModInstance()
                 */
                if (needAutoEnableCall)
                {
                    needAutoEnableCall = false;
                    AutoEnableOnce(false);
                }
                return Versioning.PACKAGE_NAME;
            }
        }

        public string Description => $"Installing locally at {mainMod.name} from {Versioning.PUBLISH_URL}";
#endregion

        /* enabled is sticky, as this mod can only be enabled for now, but not disabled.
         * FIXME: Implement teardown, then disabling will work.
         */
        /*
         * FIXME: For bonus points, Helper should automatically become main when main is deleted live.
         * This would mean main calling helper from OnDisable(). Need an API so different vesions can talk.
         */
        bool enabled;
        internal static bool firstRun = false;
        internal static PluginInfo mainMod = default(PluginInfo);

        static GameObject m_gameObject;
        internal static GameObject gameObject => m_gameObject;

        static Collection m_repo;
        internal static Collection repo => m_repo;

        static bool needAutoEnableCall = false;

        static Mod()
        {
            raiseExceptions = true;

            try
            {
                //#if DEBUG
                /* Workaround for https://github.com/drok/Harmony-CitiesSkylines/issues/1
                 */
                var a = Assembly.GetExecutingAssembly();
                mainMod = Singleton<PluginManager>.instance.GetPluginsInfo().FirstOrDefault((x) => x.ContainsAssembly(a));
                if (mainMod != null)
                {
                    if (!mainMod.isEnabled)
                    {
                        var stackTrace = new System.Diagnostics.StackTrace(0, false);
                        var caller = stackTrace.GetFrame(stackTrace.FrameCount - 1).GetMethod();
                        var callerName = caller.DeclaringType.FullName + "." + caller.Name;
#if TRACE
                        LogError($"[{Versioning.FULL_PACKAGE_NAME}] In Mod..cctor() thread={Thread.CurrentThread.ManagedThreadId} plugin={mainMod != null} initiated by {callerName}\n{(new System.Diagnostics.StackTrace(0, true)).ToString()}");
#endif
                        if (callerName == "MenuPanel.Start")
                        {
                            /* Need to call EnableOnce after the constructor returns */
                            needAutoEnableCall = true;
                        }
                        else /* assume call from Starter.Awake() */
                        /* When instantiated via Starter, it is likely another mod scanning the mod list,
                         * instantiating all, including disabled mods, and not the Starter instantiating
                         * because this mod is actually enabled.
                         *
                         * This logic of examining the stack trace is needed because of these scanning mods
                         * Without them, it would be simply
                         * if (mainMod != null && !mainMod.isEnabled) {
                         *      ActiveAutoEnableOnce();
                         * }
                         *
                         * However, because of them, the instantiation happens inside the PluginManager.AddPlugins()
                         * loop, which means mod.IsEnabled=true cannot be called, but must be scheduled for later.
                         * mod.IsEnabled=true would cause a second instance to be instantiated first and OnEnabled(),
                         * but the first instance added as userModInstance, not OnEnabled(), but present
                         * on the instance list.
                         */
                        {
                            /* I've already been added to the plugin list. Need active enable */
                            Singleton<PluginManager>.instance.eventPluginsChanged += ActiveAutoEnableOnce;
                        }
                    }
#if DEVELOPER_UPDATER
                    AutoInstallHelperOnce(mainMod);
                    needInstallCall = false;
#endif
                }

            }
            catch (Exception ex)
            {
                LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] WARN - Mod..cctor: ({ex.Message})");
            }
#if HEAVY_TRACE
            Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Mod..cctor Debugger: {System.Diagnostics.Debugger.IsAttached} DONE");
#endif
        }
        // Install Harmony as soon as possible to avoid problems with mods not following the guidelines

#region UserMod invoked handlers
        public void OnEnabled()
        {
            try
            {
                /* I am the main mod */
                if (!enabled)
                {
                    enabled = true;

                    Install();
                }
            }
            catch (Exception ex)
            {
                LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR FAIL TO INIT: {ex.Message}");
            }
        }

        public void OnDisabled()
        {
        }
#endregion

        void Install()
        {
            if (!m_gameObject)
            {
                m_repo = new Collection();
                m_gameObject = new GameObject("HarmonyInstall");
                GameObject.DontDestroyOnLoad(m_gameObject);
            }

            Item self = Item.ItemFromURI(Versioning.PUBLISH_URL + "/" + Versioning.RELEASE_BRANCH);
            Loaded selfPlugin = new Loaded(mainMod);
            self.Install(selfPlugin, true);
        }

        internal static void Deactivate()
        {
            if (m_gameObject != null)
                UnityEngine.Object.Destroy(m_gameObject);
        }

#region Helper Maintenance
        static void ActiveAutoEnableOnce()
        {
            AutoEnableOnce(true);
        }
        static void ActiveAutoEnableOnce_Scheduled()
        {
            AutoEnableOnce(false);
        }

        static void AutoEnableOnce(bool deferred)
        {
            
            if (deferred)
            {
                Singleton<PluginManager>.instance.eventPluginsChanged -= ActiveAutoEnableOnce;
            }

#if DEBUG
            LogError($"[{Versioning.FULL_PACKAGE_NAME}] In Mod.AutoEnableOnce({deferred}) plugins={mainMod != null}\n{(new System.Diagnostics.StackTrace(0, true)).ToString()}");
#endif

            if (mainMod != null)
            {
#if DEBUG
                        Log($"[{Versioning.FULL_PACKAGE_NAME}] Found - {mainMod.name} - enabled={mainMod.isEnabled} - {mainMod.assembliesString}");
#endif
                if (mainMod.ContainsAssembly(Assembly.GetExecutingAssembly()))
                {
                    var oneShotAutoEnable = new SavedBool(name: mainMod.name + mainMod.modPath.GetHashCode().ToString() + ".enabled",
                        fileName: Settings.userGameState,
                        def: false,
                        autoUpdate: true);

#if TRACE
                            Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - checking enabled flag at mainMod.modPath={mainMod.modPath} {mainMod.name + mainMod.modPath.GetHashCode().ToString() + ".enabled"}");
#endif

                    if (!oneShotAutoEnable && !oneShotAutoEnable.exists)
                    {
                        Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Mod is disabled in the Content Manager! Self-enabling now.");

                        firstRun = true;
                        mainMod.isEnabled = true;

                        /* FIXME: isEnabled vs oneShotAutoEnable.Value = true */
                    }
                    else
                    {
                        LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] WARNING - Mod is disabled in the Content Manager! Self-enabling has already been used.");
                    }
                }
            }
        }

#if DEVELOPER
        static void AutoInstallHelperOnce(PluginInfo plugin)
        {

#if DEBUG
            LogError($"[{Versioning.FULL_PACKAGE_NAME}] In Mod.AutoInstallHelperOnce() plugin={plugin != null}");
#endif


            try
            {
                string helperPath = ColossalFramework.IO.DataLocation.modsPath + "\\" + RECOMMENDED_LOCAL_HELPER_DIRNAME;

                if (!Directory.Exists(helperPath))
                {
                    /* Enable it every time the directory needs to be recreated
                     * To avoid this, simply disable the local Diagnostics module
                     * when you don't want it, without deleting the folder
                     */
                    var localDiagnosticsModEnable = new SavedBool(name: RECOMMENDED_LOCAL_HELPER_DIRNAME + helperPath.GetHashCode().ToString() + ".enabled",
                        fileName: Settings.userGameState,
                        def: false,
                        autoUpdate: true).value = true;

                    LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Installing local HELPER");

                    FileSystemUtils.CopyDirectoryFiltered(
                        plugin.modPath,
                        helperPath,
                        CopyAllButMyAssembly,
                        false);
                }
                HelperUpdate(plugin);
            }
            catch (Exception ex) {
                SelfReport(SelfProblemType.HelperInstallationFailed, ex);
            }
        }

        static bool HelperUpdate(PluginInfo self)
        {
            try
            {
                var helperFilename = Directory.GetFiles(self.modPath, Assembly.GetExecutingAssembly().GetName().Name + "_helper_dll");
                string helperDestination = ColossalFramework.IO.DataLocation.modsPath + "/" +
                    RECOMMENDED_LOCAL_HELPER_DIRNAME + "/" +
                    Assembly.GetExecutingAssembly().GetName().Name + ".dll";

                if (helperFilename.Length == 1)
                {
                    File.Copy(FileSystemUtils.NiceWinPath(helperFilename[0]), FileSystemUtils.NiceWinPath(helperDestination), true);
                    return true;
                }
                else
                {
                    throw new Exception("helper_dll file was not found");
                }
            }
            catch (Exception ex)
            {
                SelfReport(SelfProblemType.HelperInstallationFailed, new Exception("Updating Helper failed", ex));
            }

            return false;
        }

        static bool CopyAllButMyAssembly(string filename) {
            bool result = !filename.EndsWith(Assembly.GetExecutingAssembly().GetName().Name + ".dll") ||
                !filename.EndsWith(Assembly.GetExecutingAssembly().GetName().Name + "_helper_dll");
            LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] INFO - CopyAllButMyAssembly({filename}) = {result}");
            return result;
        }
#endif
#endregion

    }
}
