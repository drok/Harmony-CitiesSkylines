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
using System.Reflection;
using ColossalFramework.Plugins;
using ColossalFramework;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using static UnityEngine.Debug;

namespace HarmonyManager
{
    public class Harmony
    {

        /// <summary>
        /// What built-in actions to take in case of Harmony permanent installation failure?
        /// Permanent failure means:
        ///  - Installation declined/aborted by the user,
        ///  - GitHub repo removed.
        /// Temporary failures (they do not trigger the onCancel callback) means:
        ///  - GitHub is unreachable
        ///  - GitHub imposed rate-limiting
        ///  - Harmony mod is installed, but not yet enabled
        ///
        /// Temporary failures will be retried indefinately, but permanent failures
        /// will be handled as per this option, and/or the onCancel callback 
        /// </summary>
        public enum HarmonyCancelAction
        {
            /// <summary>
            /// Do nothing special when Haramony installation fails. This is useful
            /// when some parts of the mod can still work without patches present.
            /// </summary>
            Noop,

            /// <summary>
            /// Disable the mod when installation fails. This is useful when
            /// no part of the mod can work without patches installed.
            /// </summary>
            DisableMod,
        }

        public enum HarmonyUnavailableReason
        {
            /// <summary>
            /// Installation of Harmony was cancelled by the user
            /// </summary>
            InstallationCancelled,

            /// <summary>
            /// Installation of Harmony failed. This should be considered a transient
            /// condition, and installation may succeed if requested in the future, but
            /// on the current request, no further retries will be made.
            /// . Possible explanations:
            /// - Harmony publishing location does not have the release (eg, release is deleted)
            /// - Not enough disk space
            /// </summary>
            InstallationFailed,

#if CALLBACK_UNAVAILABLE_ON_DISABLED_HARMONY
            /// <summary>
            /// Harmony is installed, but the user has disabled it. It will not be available
            /// until the user enables it. The queue of actions is retained and will be
            /// reattempted when Harmony is enabled. To cancel retries, call
            /// CancelOnHarmonyReady() to clear the queue.
            /// The "unavailable" callback will be called again for this reason whenever
            /// a new plugin is added or enabled/disabled, as long as the condition
            /// remains true that Harmony remains disabled.
            /// Typically the mod should do a "noop" on this callback.
            /// </summary>
            HarmonyInstalledButDisabled,
#endif
        }
        static List<ClientCallback> _harmonyReadyActions = new List<ClientCallback>();
        // static List<Action> _harmonyCancelActions = new List<Action>();
        static List<Action<HarmonyUnavailableReason>> _harmonyUnavailableActions = new List<Action<HarmonyUnavailableReason>>();
        static HashSet<PluginManager.PluginInfo> _harmonyDisableModsOnCancel = new HashSet<PluginManager.PluginInfo>();
        static HashSet<PluginManager.PluginInfo> _harmonyWaitingMods = new HashSet<PluginManager.PluginInfo>();
        static object installer = null;

        struct ClientCallback
        {
            public Action action;
            public StackTrace callStack;

            public IAwareness.ClientCallback awarenessConversion =>
                    new IAwareness.ClientCallback { action = action, callStack = callStack, };
        }

        static bool IsHarmonyPresent {
            get {
                var myKey = typeof(Harmony).Assembly.GetName().GetPublicKeyToken();
                return AppDomain.CurrentDomain.GetAssemblies().Any<Assembly>((a) => {
                    var aName = a.GetName();
                    return aName.Name == "IAmAware" &&
                        (myKey == null || (aName.GetPublicKeyToken() != null && myKey.SequenceEqual(aName.GetPublicKeyToken())));
                });
            }
        }


        /// <summary>
        /// Install Callbacks to be called when Harmony is ready to patch, or, if installation
        /// is needed, when installation positively fails or is aborted.
        /// 
        /// - onReady calback can be used to start patching, and start the mod.
        /// - onUnavailable callback can be used to stop waiting and switch to a basic
        ///   operation if available (without patches).
        ///   Mods should not use onCancel to pop-up an exception informing the user,
        ///   because the user has either cancelled the installation, or is informed
        ///   via Harmony Report / Content Manager coloring of failing mods.
        /// </summary>
        /// <remarks>It can be called without any arguments to trigger Harmony Installation,
        /// even if no patching is needed. This is useful if the mod has other pre-requisites
        /// or known conflicts that Harmony should handle. (see "application settings")
        /// </remarks>
        /// 
        /// <param name="onReady">Action to be called when Harmony is ready to work</param>
        /// <param name="onUnavailable">Action to be called if Harmony will not be available;
        /// The reason is given as an argument to the callback.
        /// <param name="builtinCancelAction">Built in actions that the API can perform
        /// without needing a callback:
        /// <p><see cref="HarmonyCancelAction.Noop">do nothing</see></p>
        /// <p><see cref="HarmonyCancelAction.DisableMod">disable the mod</see>
        /// so it will not be auto-loaded on future start-ups (useful with installers)</p>
        /// </param>
        /// <param name="autoInstallHarmony">Whether to install Harmony without
        /// user confirmation (default=true). It is recommended to set it to true
        /// for items installed from a workshop because the user has already seen
        /// a "Required items" pop-up when requesting the mod be subscribed. It would be
        /// unnecessary nagging (by over-confirmation) to prompt again on the same topic.
        /// 
        /// </param>
        public static void DoOnHarmonyReady(
            Action onReady = null,
            Action<HarmonyUnavailableReason> onUnavailable = null,
            HarmonyCancelAction builtinCancelAction = HarmonyCancelAction.Noop,
            bool autoInstallHarmony = true)
        {
            var plugin = Singleton<PluginManager>.instance.FindPluginInfo(new StackTrace(1, false).GetFrame(0).GetMethod().DeclaringType.Assembly);

            /* FIXME: If mod is enabled before harmony. OnReady needs to be called at Harmony enable or LoadExtension.OnCreate */
            var firstAction = !_harmonyReadyActions.Any();
            if (onReady != null)
                _harmonyReadyActions.Add(new ClientCallback() { action = onReady, callStack = new StackTrace(1, false), });

            if (onUnavailable != null)
                _harmonyUnavailableActions.Add(onUnavailable);

            if (plugin != null)
            {
                switch (builtinCancelAction)
                {
                    case HarmonyCancelAction.DisableMod:
                        _harmonyDisableModsOnCancel.Add(plugin);
                        break;
                }
                _harmonyWaitingMods.Add(plugin);
            }

            Log($"[{Versioning.FULL_PACKAGE_NAME}] (1) _harmonyReadyActions.Any()={_harmonyReadyActions.Any()} && firstAction={firstAction}");

            if (IsHarmonyPresent)
            {
                Log($"[{Versioning.FULL_PACKAGE_NAME}] Harmony is present!! would do callbacks.");
                if (!DoAwarenessCallbacks())
                {
#if CALLBACK_UNAVAILABLE_ON_DISABLED_HARMONY
                    OnUnavailable(HarmonyUnavailableReason.HarmonyInstalledButDisabled);
#endif
                }
            }
            else
            {
                Log($"[{Versioning.FULL_PACKAGE_NAME}] (2) _harmonyReadyActions.Any()={_harmonyReadyActions.Any()} && firstAction={firstAction}");
                if (_harmonyReadyActions.Any() && firstAction)
                {
                    Singleton<PluginManager>.instance.eventPluginsChanged += OnInstallSuccess;
                    Log($"[{Versioning.FULL_PACKAGE_NAME}] Added eventPluginsChanged handler = OnInstallSuccess()");
                }

                InstallerLite.Install(ref installer, plugin, autoInstallHarmony);
            }
        }

        internal static void OnUnavailable(HarmonyUnavailableReason reason)
        {
            foreach (var action in _harmonyUnavailableActions)
            {
                try
                {
                    action(reason);
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }
#if CALLBACK_UNAVAILABLE_ON_DISABLED_HARMONY
            if (reason != HarmonyUnavailableReason.HarmonyInstalledButDisabled)
                _harmonyUnavailableActions.Clear();
#else
            _harmonyUnavailableActions.Clear();
#endif
        }
        internal static void OnInstallAborted()
        {
            installer = null;
            OnUnavailable(HarmonyUnavailableReason.InstallationCancelled);
            Log($"[{Versioning.FULL_PACKAGE_NAME}] OnInstallAborted _harmonyDisableModsOnCancel={_harmonyDisableModsOnCancel.Count}");

            HashSet<PluginManager.PluginInfo> disableList = new HashSet<PluginManager.PluginInfo>(_harmonyDisableModsOnCancel);

            _harmonyDisableModsOnCancel.Clear();
            _harmonyWaitingMods.Clear();
            _harmonyReadyActions.Clear();
            Log($"[{Versioning.FULL_PACKAGE_NAME}] OnInstallAborted disablelist={disableList.Count}");

            foreach (var mod in disableList)
            {
                Log($"[{Versioning.FULL_PACKAGE_NAME}] Disabling mod {mod.name} because Harmony Install is cancelled");
                mod.isEnabled = false;
            }

            if (disableList.Count > 0)
            {
                /* FIXME: Updating all is heavy. Find a way to toggle the "active" checkbox
                 * of the affected items only
                 */
                Singleton<PluginManager>.instance.ForcePluginsChanged();
            }

        }

        static bool DoAwarenessCallbacks()
        {
            var callbacks = new List<IAwareness.ClientCallback>();
            _harmonyReadyActions.ForEach((a) => callbacks.Add(a.awarenessConversion));

            Log($"[{Versioning.FULL_PACKAGE_NAME}] found {Singleton<PluginManager>.instance.GetImplementations<IAwareness.IAmAware>().Count} {typeof(IAwareness.IAmAware).AssemblyQualifiedName} implementations.");
            
            return Singleton<PluginManager>.instance.GetImplementations<IAwareness.IAmAware>()
                .Any((awareness) =>
                {

                    var result = awareness.DoOnHarmonyReady(callbacks);
                    Log($"[{Versioning.FULL_PACKAGE_NAME}] Try sending ready actions to awareness instance {awareness} ... {result}");
                    return result;
                });
        }

        internal static void AfterList()
        {
            var a = new IAwareness.ClientCallback() { action = null, callStack = null };
            Log($"[{Versioning.FULL_PACKAGE_NAME}] AfterList() worked");

        }
#if HEAVY_TRACE
        internal static void ListAssemblies()
        {
            string names = string.Empty;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                names += assembly.GetName() + "\n";
            }
            Log(names);
        }
#endif
        internal static void OnInstallSuccess()
        {
            // Log($"[{Versioning.FULL_PACKAGE_NAME}] OnInstallSuccess()");
            try
            {
#if HEAVY_TRACE
                ListAssemblies();
#endif

                if (IsHarmonyPresent)
                {
                    if (DoAwarenessCallbacks())
                    {
                        Singleton<PluginManager>.instance.eventPluginsChanged -= OnInstallSuccess;
                    }
#if CALLBACK_UNAVAILABLE_ON_DISABLED_HARMONY
                    else
                    {
                        OnUnavailable(HarmonyUnavailableReason.HarmonyInstalledButDisabled);
                    }
#endif
                    // Log($"[{Versioning.FULL_PACKAGE_NAME}] OnInstallSuccess() - all done");
                }
            }
            catch (Exception ex)
            {
                /* Awareness1 not present */
                Log($"[{Versioning.FULL_PACKAGE_NAME}] while calling DoAwarenessCallbacks: {ex.Message}");
                LogException(ex);
            }
        }
        /// <summary>
        /// Cancel all previously registered OnHarmonyReady callbacks.
        /// This should be done in OnDisable() or if a gamesave is loaded before
        /// Harmony is installed (eg, if the Harmony download is slow), and the
        /// calling plugin is not able to cleanly handle patching a running
        /// simulation.
        /// In case the mod is able to be patched-in at any time, including
        /// after the game has already started, there is no need to Cancel the
        /// callbacks. They will be called whenever Harmony becomes available,
        /// including possibly never (eg, extended connectivity outage)
        /// </summary>
        public static void CancelOnHarmonyReady()
        {
            var callerAssembly = new StackTrace(1, false).GetFrame(0).GetMethod().DeclaringType.Assembly;
            PluginManager.PluginInfo plugin = null;
            foreach (var p in _harmonyWaitingMods)
            {
                if (p.ContainsAssembly(callerAssembly))
                {
                    plugin = p;
                    break;
                }
            }

            // plugin is null when called from OnDisable
            if (plugin != null)
            {
                Log($"[{Versioning.FULL_PACKAGE_NAME}] CancelOnHarmonyReady caller = {callerAssembly.GetName().FullName} plugin={plugin.name}");
            }
            else
            {
                Log($"[{Versioning.FULL_PACKAGE_NAME}] CancelOnHarmonyReady caller = {callerAssembly.GetName().FullName} plugin={plugin != null}");

            }

            /* FIXME: In multi-assembly mods, the callback may not be in the same assembly as the callerAssembly
             */
            _harmonyReadyActions.RemoveAll(callback => callback.callStack.GetFrame(0).GetMethod().DeclaringType.Assembly == callerAssembly);
            _harmonyDisableModsOnCancel.Remove(plugin);
            _harmonyWaitingMods.Remove(plugin);

            // if (_harmonyWaitingMods.Count == 0)
            // {
            //     UnityEngine.Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] CancelOnHarmonyReady requesting installer cancel = {installer != null}");
            //     if (installer != null)
            //     {
            //         Installer.CancelInstall(ref installer);
            //     }
            // }
        }

        static Harmony()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ARResolve;
            AppDomain.CurrentDomain.TypeResolve += ATResolve;
        }
        public static Assembly ARResolve(object sender, ResolveEventArgs args)
        {
            var assname = new AssemblyName(args.Name);

#if TRACE
            Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Assembly Resolve: {args.Name} {args.GetType()}");
#endif
            return null;
        }

        public static Assembly ATResolve(object sender, ResolveEventArgs args)
        {
            var assname = new AssemblyName(args.Name);

#if TRACE
            Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Type Resolve: {args.Name} {args.GetType()}");
#endif
            return null;
        }


    }
}
