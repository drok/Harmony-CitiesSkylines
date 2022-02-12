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

namespace UpdateFromGitHub
{


    public class Mod : IUserMod
    {
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

        public string Description => $"Updates mods and assets from GitHub Releases";
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
                }
            }
            catch (Exception ex)
            {
                LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] WARN - Mod..cctor: ({ex.Message})");
            }
        }

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

        #endregion

        void Install()
        {
            if (!m_gameObject)
            {
                m_repo = new Collection();
                m_gameObject = new GameObject(Versioning.PACKAGE_NAME);
                GameObject.DontDestroyOnLoad(m_gameObject);
            }

            Item self = Item.ItemFromURI(Versioning.PUBLISH_URL + "/" + Versioning.RELEASE_BRANCH + "?" + Versioning.INSTALL_FILENAME);
            Loaded selfPlugin = new Loaded(mainMod);
            self.Install(selfPlugin, true);

            /* the list of mods to be updated is hardcoded until I fully test the
             * security of the final solution, which must match Steam account owners
             * with their respective GitHub accounts, so downloads cannot be hijacked.
             * 
             */

            Dictionary<PublishedFileId, string> updatelist = new Dictionary<PublishedFileId, string>()
            {
                {new PublishedFileId(2325122732),
                    "https://github.com/drok/SupplyChainColoring/maintenance-1.1?SupplyChainColoringMod"},

                {new PublishedFileId(2389228470),
                    "https://github.com/drok/TransferBroker/beta?TransferBrokerMod"},
            };

            if (HarmonyNeedsUpdate())
            {
                updatelist.Add(new PublishedFileId(2399343344),
                    Versioning.PUBLISH_URL + "/" + Versioning.RELEASE_BRANCH_2399343344 + "?" + Versioning.INSTALL_FILENAME_2399343344);
            }

            PublishedFileId[] subscribedItems = PlatformService.workshop.GetSubscribedItems();
            if (subscribedItems != null)
            {
                foreach (PublishedFileId id in subscribedItems)
                {
                    string subscribedItemPath = PlatformService.workshop.GetSubscribedItemPath(id);
                    if (subscribedItemPath != null && Directory.Exists(subscribedItemPath))
                    {
                        if (updatelist.TryGetValue(id, out var publishURI))
                        {
                            Item.ItemFromURI(publishURI).Install(new Loaded(new PluginInfo(subscribedItemPath, false, id)), true);
                        }
                    }
                }
            }
        }

        bool HarmonyNeedsUpdate()
        {
            var existing = Type.GetType("HarmonyMod.Mod, HarmonyMod, Version=1.0.0.11, PublicKeyToken=8625d3dcfab56202", false);
            return existing != null && existing.Assembly.GetName().Version < new Version(1, 0, 1, 0); ;
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

        /* Auto enable the first time it is installed, as if default enable state = enabled. */
        static void AutoEnableOnce(bool deferred)
        {
            
            if (deferred)
            {
                Singleton<PluginManager>.instance.eventPluginsChanged -= ActiveAutoEnableOnce;
            }

            if (mainMod != null)
            {
                if (mainMod.ContainsAssembly(Assembly.GetExecutingAssembly()))
                {
                    var oneShotAutoEnable = new SavedBool(name: mainMod.name + mainMod.modPath.GetHashCode().ToString() + ".enabled",
                        fileName: Settings.userGameState,
                        def: false,
                        autoUpdate: true);

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

#endregion

    }
}
