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
extern alias Harmony2;
using IAwareness;
using System;
using System.Reflection;
using System.Linq;
using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using static ColossalFramework.Plugins.PluginManager;
using Harmony2::HarmonyLib;
using UnityEngine.Assertions;

namespace HarmonyMod
{
    class Handover
    {
        PluginInfo m_self;
        PluginInfo m_mainMod;

        internal Handover(IAmAware selfMod)
        {
            m_self = Singleton<PluginManager>.instance.GetPluginsInfo().First((x) =>
            {
                try
                {
                    return x.isEnabledNoOverride && x.userModInstance == selfMod;
                }
                catch (ReflectionTypeLoadException ex)
                {
                    ex.LoaderExceptions.Do((e) => (selfMod as Mod).report.ReportPlugin(x, ModReport.ProblemType.ExceptionThrown, $"LoaderException: {e}"));
                }
                catch (Exception ex)
                {
                    (selfMod as Mod).report.ReportPlugin(x, ModReport.ProblemType.ExceptionThrown, ex.Message);
                    UnityEngine.Debug.LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] While Scanning plugins, {x.name} caused exception {ex.GetType().Name}: {ex.Message}");
                }
                return false;
            });

            DisableHarmonyFromBoformer();
        }

        internal void UpdateSelf(Loaded selfMod)
        {
            Assert.IsTrue(m_self == m_mainMod,
                "Only the main mod should update the refs");
            m_self = selfMod;
            m_mainMod = selfMod;
        }

        internal PluginInfo mainMod(bool? mainEnabled = null)
        {
            /* When the main mode gets OnDisable(), its userModInstance is still valid, it gets set to null
             * *after* OnDisable returns. So, I must ignore a main that appears enabled, but in fact
             * is telling me it's being disabled
             */

            Assert.IsTrue(
                (m_mainMod == null && mainEnabled == null) || /* First Scan */
                (m_mainMod != null && mainEnabled == null) || /* get existing value */
                (m_mainMod != null && mainEnabled != null)); /* scan for update, ignoring m_mainMod when mainEnabled=false */

            var prevMain = m_mainMod;
            PluginInfo ignoreThis = null;
            if (m_mainMod != null)
            {
                if (mainEnabled != null)
                {
                    if (mainEnabled == false)
                    {
                        ignoreThis = m_mainMod;
                    }
                    m_mainMod = null;
                }
            }

            if (m_mainMod == null)
            {

                /* This instance is a Helper to a main mod if:
                    * 1. There is a higher version, enabled mod
                    *
                    * This instance is the main mod if:
                    * 1. It is the highest version, enabled mod.
                    *
                    * Also sets appropriately:
                    * - isMainMod
                    * - isLocal
                    * - isFirst
                    */
                var myName = Assembly.GetExecutingAssembly().GetName();

                PluginInfo firstInstance = null;


                isLocal = m_self.publishedFileID == PublishedFileId.invalid;
#if DEVELOPER
                bool isFirstInstanceLocal = false;
#endif
                isFirst = true;
                bool isFirstInstanceFirst = false;

                /* GetPlugingsInfo is no good. When the 1st mod is OnEnable, it is the only mod on this list.
                    * So helper cannot detect another instance, and assumes it is main.
                    * Also a main cannot detect it should be inactive if a later main is avail.
                    */
                foreach (var info in Singleton<PluginManager>.instance.GetPluginsInfo())
                {
                    if (info.isBuiltin)
                    {
                        continue;
                    }

                    if (info == ignoreThis)
                    {
                        continue;
                    }
                    bool isAnotherInstance = false;
                    foreach (var assembly in info.GetAssemblies())
                    {
                        var name = assembly.GetName();

                        if (name.Name == myName.Name &&
                            ((name.GetPublicKeyToken() == null && myName.GetPublicKeyToken() == null) ||
                                name.GetPublicKeyToken().SequenceEqual(myName.GetPublicKeyToken())))
                        {
                            isAnotherInstance = true;
                            if (info.isEnabledNoOverride)
                            {
                                /* Query if it is firstRun (not fully aware). If any instance is first run, all are, but only one can detect. */
                                if (!Mod.firstRun && assembly != Assembly.GetExecutingAssembly())
                                {
                                    Mod.firstRun = !assembly.GetTypes()
                                    .Where(p => typeof(IAmAware).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract)
                                    .Any((p) => (Activator.CreateInstance(p) as IAmAware).IsFullyAware());
#if TRACE
                                    UnityEngine.Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Other instance is firstRun={Mod.firstRun}");
#endif
                                }
                                if (m_mainMod == null || name.Version >= mainModVersion)
                                {
                                    m_mainMod = info;
                                    mainModVersion = name.Version;
                                    isMainMod = info == m_self;
#if TRACE
                                    UnityEngine.Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - In Handover..get_mainMod() current best {mainModVersion}");
#endif
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    if (isAnotherInstance && !info.isEnabledNoOverride)
                    {
                        continue;
                    }
                    if (isAnotherInstance && firstInstance == null)
                    {
                        firstInstance = info;
                        isFirstInstanceFirst = isFirst;
#if DEVELOPER
                        isFirstInstanceLocal = info.publishedFileID == PublishedFileId.invalid;
#endif
                    }

                    if (isFirst)
                    {
                        isFirst = m_mainMod != null;
                    }
                }

                isHelper = !isMainMod && (m_self == firstInstance);
                if (m_mainMod != firstInstance)
                {
                    helperMod = firstInstance;
                    isHelperFirst = isFirstInstanceFirst;
#if DEVELOPER
                    isHelperLocal = isFirstInstanceLocal;
#endif
                }
            }
#if TRACE
            if (m_mainMod != prevMain)
            {
                var helperDescription = (helperMod != null) ? $" Helper={helperMod.userModInstance.GetType().FullName}, {helperMod.userModInstance.GetType().Assembly.GetName().Version}" : string.Empty;
#if TRACE
#if DEVELOPER
                UnityEngine.Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - In Handover..get_mainMod() Result: isFirst={isFirst} isMainMod={isMainMod} isLocal={isLocal} isHelper={isHelper} isHelperFirst={isHelperFirst} isHelperLocal={isHelperLocal} mainModVersion={mainModVersion}{helperDescription}. I am {m_self.name}");
#else
                UnityEngine.Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - In Handover..get_mainMod() Result: isFirst={isFirst} isMainMod={isMainMod} isLocal={isLocal} isHelper={isHelper} isHelperFirst={isHelperFirst} mainModVersion={mainModVersion}{helperDescription}. I am {m_self.name}");
#endif
#endif
            }
#endif
            return m_mainMod;
        }

        internal PluginInfo self { get { return m_self; } }

        internal PluginInfo helperMod { get; private set; }
        internal Version mainModVersion { get; private set; }

        /* Is THIS instance the main mod ? */
        internal bool isMainMod { get; private set; }

        /* Is THIS instance installed locally ? */
        internal bool isLocal { get; private set; }

        /* Is THIS instance the designated helper? */
        internal bool isHelper { get; private set; }

        /* Is THIS instance First ? */
        internal bool isFirst { get; private set; }

        internal bool isHelperFirst { get; private set; }
#if DEVELOPER
        internal bool isHelperLocal { get; private set; }
#endif
        internal bool BootStrapMainMod(bool? mainEnabled = null)
        {
            return mainMod(mainEnabled) != null && !isMainMod;
        }

        /* Signal the other instances which have lost mainMod status
         */
        internal void NotifyStandbys(bool mainEnabled)
        {
            var selfAware = m_self.userModInstance as IAmAware;
            Assert.IsTrue(m_self == m_mainMod,
                "Only the main mod should notify others");
            Assert.IsNotNull(selfAware,
                $"Self should be an {typeof(IAmAware).Name} instance");

            foreach (var mod in Singleton<PluginManager>.instance.GetPluginsInfo())
            {
                if (mod != m_mainMod && mod != m_self && mod.isEnabledNoOverride)
                {
                    try
                    {
                        IAmAware awareInst = mod.userModInstance as IAmAware;
                        if (awareInst != null)
                        {
                            awareInst.OnMainModChanged(selfAware, mainEnabled);
                        }
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        ex.LoaderExceptions.Do((e) => (m_self.userModInstance as Mod).report.ReportPlugin(mod, ModReport.ProblemType.ExceptionThrown, $"LoaderException: {e}"));
                    }
                    catch (Exception ex)
                    {
                        (m_self.userModInstance as Mod).report.ReportPlugin(mod, ModReport.ProblemType.ExceptionThrown, ex.Message);
                        UnityEngine.Debug.LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] While notifying standys, {mod.name} caused exception {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }
        }

        void DisableHarmonyFromBoformer()
        {
            /* TODO: Implement re-enabling it if this mod is disabled?
             * It is possible, however, it will work flawlessly after this mod,
             * because all the harmony assemblies (1.2.0.1, 2.0.1, 2.0.4) are already
             * patched and loaded. Boformer's mod would have nothing to do.
             *
             * I think it's more fair that if this mod Captured the Flag by getting
             * installed first, it gets to keep the flag.
             */
            const string PatchedMarker = "Harmony1_PatchedMarker";

            var go = UnityEngine.GameObject.Find(PatchedMarker);
            if (go == null)
            {
                UnityEngine.Object.DontDestroyOnLoad(new UnityEngine.GameObject(PatchedMarker));
            }

        }
    }
}
