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
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using static UnityEngine.Debug;
using IAwareness;

namespace HarmonyManager
{
    /// <summary>
    /// Static class implements reliable detection, handover of/to HarmonyLib API
    /// <para>It will also install it as local mod or workshop mod and/or update it if necessary.</para>
    /// </summary>
    public class Harmony
    {
        /// <summary>
        /// How should the Harmony Mod be installed if missing? (<see cref="Silent"/>/<see cref="Prompt"/>/<see cref="Smart"/>/<see cref="OnlyDetect"/>)
        /// </summary>
        public enum InstallMode
        {
            /// <summary>
            /// Installation will be done silently, without user prompt.
            /// </summary>
            Silent,

            /// <summary>
            /// User will be prompted <b>[yes]/[no]</b> to confirm local Harmony Installation
            /// </summary>
            Prompt,

            /// <summary>
            /// If the calling mod is a workshop mode, it's assumed the user has already
            /// been prompted on Steam about the required dependence. Behave like "Silent"
            /// 
            /// It the calling mod is a local mod, behave like "Prompt" mode.
            /// </summary>
            Smart,


            /// <summary>
            /// So not install Harmony if unavailable. Only detect presence, and
            /// call either the ready or the unavailable callbacks, as appropriate.
            /// </summary>
            OnlyDetect,

        }
        /// <summary>
        /// What built-in actions to take in case of Harmony permanent installation failure?
        /// <para>Permanent failure means:
        ///    <list type="bullet">
        ///    <item>Installation declined/aborted by the user,</item>
        ///    <item>GitHub repo removed.</item>
        ///    </list>
        /// </para>
        /// <para>Temporary failures (they do not trigger the onCancel callback) means:
        ///    <list type="bullet">
        ///    <item>GitHub is unreachable</item>
        ///    <item>GitHub imposed rate-limiting</item>
        ///    <item>Harmony mod is installed, but not yet enabled</item>
        ///    </list>
        ///</para>
        ///
        /// Both Permanent and Temporary failures will be retried indefinately,
        /// until explicitly cancelled with
        /// <see cref="CancelOnHarmonyReady">CancelOnHarmonyReady</see>, but in
        /// case of permanent failure, this option, <see cref="DisableMod"/>,
        /// allows the mod to be disabled if needed.
        /// 
        /// Even if the mod is so auto-disabled, if the
        /// queued callbacks are not explicitly cancelled, and a Harmony mod
        /// appears later, the callbacks will be called.
        /// 
        /// <para><b>Be sure to either
        /// cancel callbacks at <b>OnDisabled()</b> time, or be ready
        /// to do something reasonable when the callbacks come.</b></para>
        /// </summary>
        public enum HarmonyCancelAction
        {
            /// <summary>
            /// <para>Do nothing special when Haramony installation fails.</para>
            /// <para>This is useful
            /// when some parts of the mod can still work without patches present.</para>
            /// </summary>
            Noop,

            /// <summary>
            /// Disable the mod when Harmony lib installation fails.
            /// <para>This is useful when
            /// no part of your mod can work without patches installed.
            /// </para>
            /// <para>It is also useful when porting 
            /// existing old mods to this API that are unable to graciously
            /// handle not having patches installed.
            /// </para>
            /// </summary>
            DisableMod,
        }
        /// <summary>
        /// Enumerates the possible reasons why the Harmony library is not available.
        /// </summary>
        public enum HarmonyUnavailableReason
        {
            /// <summary>
            /// Installation of Harmony was cancelled by the user
            /// </summary>
            InstallationCancelled,

            /// <summary>
            /// Installation of the Harmony library failed.
            /// <para>Possible explanations:</para>
            /// <list type="bullet">
            /// <item>Harmony publishing location does not have the release (eg, release is deleted)</item>
            /// <item>Not enough disk space</item>
            /// </list>
            /// 
            /// This should be considered a transient
            /// condition, and installation may succeed if requested in the future, but
            /// on the current request, no further retries will be made.
            /// </summary>
            InstallationFailed,

#if CALLBACK_UNAVAILABLE_ON_DISABLED_HARMONY
            /// <summary>
            /// Harmony is installed, but it is not available now. It may be disabled by
            /// the user, or it may have not been enumerated (OnEnabled()) yet.
            /// 
            /// This is normal if you call OnReadyHarmony() from your own mod's OnEnabled()
            /// method, because at the time of that call, mods are in the process of being
            /// added to the plugin list and enabled, one at a time.
            /// 
            /// It's not correct to call OnReadyHarmony from OnEnabled() for this reason,
            /// and also because the other mods your mod needs to communicate have not
            /// been OnEnabled() yet.
            /// 
            /// Calling <see cref="DoOnHarmonyReady"/> from <see cref="IUserMod.OnEnabled"/>
            /// is described by the CSL
            /// community as "Load Order Problem", but there is no problem. Mods are not
            /// guarateed to load in any particular order, which means you may not
            /// attempt to communicate with, or even detect other mods from OnEnabled().
            /// until the user enables it. The queue of actions is retained and will be
            /// reattempted when Harmony is enabled. To cancel retries, call
            /// CancelOnHarmonyReady() to clear the queue.
            /// 
            /// The "unavailable" callback will be called again for this reason whenever
            /// a new plugin is added or enabled/disabled, as long as the condition
            /// remains true that Harmony remains disabled.
            /// Typically the mod should do a "noop" on this callback.
            /// </summary>
            HarmonyInstalledButNotActive,
#endif
        }
        static List<ClientCallback> _harmonyReadyActions = new List<ClientCallback>();
        // static List<Action> _harmonyCancelActions = new List<Action>();
        static List<Action<HarmonyUnavailableReason>> _harmonyUnavailableActions = new List<Action<HarmonyUnavailableReason>>();
        static HashSet<PluginManager.PluginInfo> _harmonyDisableModsOnCancel = new HashSet<PluginManager.PluginInfo>();
        static HashSet<PluginManager.PluginInfo> _harmonyWaitingMods = new HashSet<PluginManager.PluginInfo>();

        struct ClientCallback
        {
            public Action action;
            public StackTrace callStack;
        }
        //        static IAwareness.ClientCallback awarenessConversion(ClientCallback genericClientCallback) =>
        //            new IAwareness.ClientCallback { action = genericClientCallback.action, callStack = genericClientCallback.callStack, };

        enum PresentHarmonyType
        {
            None = 0,
            Generic, /* Eg, 0Harmony.dll or CitiesHarmony.Harmony.dll */
            Aware, /* IAwareness instance */
        }
        static PresentHarmonyType? m_availableHarmony = null;
        static Assembly CO_Harmony_API_Assembly = null;
        internal static PluginManager.PluginInfo CO_Harmony_installer = null;
        static object awarenessInstance;
        static HarmonyUnavailableReason? unavailableReason;

        /// <summary>
        /// Sets operating options for this plugin instance.
        /// </summary>
        /// <param name="builtinCancelAction">Built in actions that the API can perform
        /// when Harmony lib installation fails, without needing a callback.
        /// </param>
        public static void SetOptions(
            HarmonyCancelAction builtinCancelAction = HarmonyCancelAction.Noop)
        {
            if (Singleton<PluginManager>.instance.FindPluginInfo(new StackTrace(1, false).GetFrame(0).GetMethod().DeclaringType.Assembly) is PluginManager.PluginInfo plugin)
            {
                switch (builtinCancelAction)
                {
                    case HarmonyCancelAction.DisableMod:
                        _harmonyDisableModsOnCancel.Add(plugin);
                        break;
                    case HarmonyCancelAction.Noop:
                        _harmonyDisableModsOnCancel.Remove(plugin);
                        break;
                }
                _harmonyWaitingMods.Add(plugin);
            }

        }

        /// <summary>
        /// Install callbacks to be called when Harmony lib is ready, and when installation
        /// fails or is aborted.
        /// 
        /// <para>Multiple callbacks can be installed by calling this function multiple times.
        /// The last call's <see cref="InstallMode"/> takes precedence over previous settings.</para>
        /// </summary>
        /// <remarks>It can be called without any arguments to trigger Harmony Installation,
        /// even if no patching is needed. This is useful if the mod has other pre-requisites
        /// or known conflicts that Harmony should handle. (see "application settings")
        /// </remarks>
        /// <example>
        /// In this example, patches are installed when Harmony is ready, and a callback
        /// handler is installed for the eventuality that the game starts without patches,
        /// and the Harmony installation fails later.
        /// If the Harmony Mod is not installed, the user will be prompted for installation.
        /// If the game is running in an editor or scenario mode, there will be no 
        /// Harmony install prompt, and no callbacks.
        /// <code>
        /// void ILoadingExtension.OnLevelLoaded(LoadMode mode)
        /// {
        ///     if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
        ///     {
        ///         UnityEngine.Debug.LogError($"[{ModName.MyName}] Requesting patching callback");
        ///         patchesInstalledAtGameLoad = Harmony.DoOnHarmonyReady(
        ///             () => { patcher = new Patcher(); patcher.Apply(); },
        ///             (reason) => RunWithoutPatches(reason),
        ///             Harmony.InstallMode.Prompt);
        ///     } else {
        ///         patchesInstalledAtGameLoad = false;
        ///     }
        /// }
        /// </code></example>
        /// <param name="onReady">Action to be called when Harmony is ready to work</param>
        /// <param name="onUnavailable">Action to be called if Harmony will not be available;
        /// The reason is given as an argument to the callback.
        /// </param>
        /// 
        /// <param name="installMode">How to install Harmony if needed
        /// </param>
        /// <returns><B>true</B> if ready and callbacks were called, <b>false</b> if the callbacks were deferred</returns>
        public static bool DoOnHarmonyReady(
            Action onReady = null,
            Action<HarmonyUnavailableReason> onUnavailable = null,
            InstallMode installMode = InstallMode.Smart)
        {
            bool done;

            var firstAction = !_harmonyReadyActions.Any();
            if (onReady != null)
                _harmonyReadyActions.Add(new ClientCallback() { action = onReady, callStack = new StackTrace(1, false), });

            if (onUnavailable != null)
                _harmonyUnavailableActions.Add(onUnavailable);

            PresentHarmonyType harmonyType = DetectHarmonyPresence();
            switch (harmonyType)
            {
                case PresentHarmonyType.Aware:
                    done = DoAwarenessCallbacks();
                    if (!done)
                    {
#if CALLBACK_UNAVAILABLE_ON_DISABLED_HARMONY
                        OnUnavailable(HarmonyUnavailableReason.HarmonyInstalledButNotActive);
#endif
                        InstallPluginChangeCallback();
                    }
                    break;
                case PresentHarmonyType.Generic:
                    DoGenericCallbacks();
                    unavailableReason = null;
                    done = true;
                    break;
                default:
                    if (_harmonyReadyActions.Any() && firstAction)
                    {
                        InstallPluginChangeCallback();
                    }
                    /* Installation of HarmonyLib */
                    if (installMode != InstallMode.OnlyDetect)
                    {
                        var callerAssembly = new StackTrace(0, false).GetFrame(0).GetMethod().DeclaringType.Assembly;
                        var plugin = Singleton<PluginManager>.instance.FindPluginInfo(callerAssembly);

                        bool prompt = installMode switch
                        {
                            InstallMode.Silent => true,
                            InstallMode.Prompt => false,
                            InstallMode.Smart => plugin.publishedFileID != ColossalFramework.PlatformServices.PublishedFileId.invalid,
                            _ => false
                        };
                        if (CO_Harmony_installer == null)
                        {
                            InstallerLite.Install(plugin, prompt);
                        }
                        else
                        {
                            LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] Skipping my Harmony install, because it will (probably) be silently forced by the CitiesHarmony.API of the plugin '{CO_Harmony_installer.name}'");
                        }
                    }
                    if (unavailableReason.HasValue) OnUnavailable(unavailableReason.Value);
                    done = false;
                    break;
            }
            return done;
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

            /* FIXME: In multi-assembly mods, the callback may not be in the same assembly as the callerAssembly
             */
            _harmonyReadyActions.RemoveAll(callback => callback.callStack.GetFrame(0).GetMethod().DeclaringType.Assembly == callerAssembly);
            _harmonyDisableModsOnCancel.Remove(plugin);
            _harmonyWaitingMods.Remove(plugin);
        }

        static PresentHarmonyType DetectHarmonyPresence()
        {
            if (m_availableHarmony != null)
                return m_availableHarmony.Value;
            m_availableHarmony = null;

            var myKey = typeof(Harmony).Assembly.GetName().GetPublicKey();
            AssemblyName genericProvider = default(AssemblyName);

            var awareness = Type.GetType("IAwareness.IAmAware, IAmAware, PublicKeyToken=8625d3dcfab56202", false);

            if (awareness != null)
            {
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var aName = a.GetName();
                    /* be flexible about the assembly name, only match the properties for compat with HCM */
                    if (myKey == null || (aName.GetPublicKey() != null && myKey.SequenceEqual(aName.GetPublicKey())))
                    {
                        try
                        {
                            if (a.GetTypes()
                            .Where(p =>
                            {
                                return awareness.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract;
                            })
                            .Any((p) =>
                            {
                                awarenessInstance = Activator.CreateInstance(p) as IAmAware;
                                return true;
                            })) m_availableHarmony = PresentHarmonyType.Aware;
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            /* Ie, another assembly signed by me has an exported interface which depends on
                             * another assembly which is absent. Ie, that assembly makes an invalid assumption.
                             */
                            LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] ReflectionTypeLoadException hit in {aName.FullName}, meaning its installation is incomplete. Please report as bug to that project.\n{ex.StackTrace}");
                        }
                        catch (FileNotFoundException ex)
                        {
                            LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] {ex.Message}:\n{ex.StackTrace}");
                        }
                        finally { }
                    }
                    if (m_availableHarmony.HasValue)
                        break;
                }
            }
            if (!m_availableHarmony.HasValue)
            {
                var coharmony = Type.GetType("CitiesHarmony.Installer, CitiesHarmony, Version=0.0.0.0", false);
                if (coharmony != null)
                {
                    m_availableHarmony = PresentHarmonyType.Generic;
                    genericProvider = coharmony.Assembly.GetName();
                } else
                {
                    m_availableHarmony = PresentHarmonyType.None;

                    var first_CO_Harmony_API_Assembly = Type.GetType("CitiesHarmony.API.HarmonyHelper, CitiesHarmony.API, Version=0.0.0.0",
                        false)?.Assembly;
                    if (first_CO_Harmony_API_Assembly != null)
                    {
                        CO_Harmony_installer = FindEnabledCOHarmonyInstaller();
                    }
                }
            }

            if (m_availableHarmony.Value == PresentHarmonyType.Aware)
            {
                Log($"[{Versioning.FULL_PACKAGE_NAME}] Aware Harmony Provider: {awarenessInstance.GetType().Assembly.GetName().FullName}");
            } else if (m_availableHarmony.Value == PresentHarmonyType.Generic)
            {
                Log($"[{Versioning.FULL_PACKAGE_NAME}] Generic Harmony Provider: {genericProvider.FullName}");
            }
            return m_availableHarmony.Value;
        }

        static PluginManager.PluginInfo FindEnabledCOHarmonyInstaller()
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a.GetName().Name == "CitiesHarmony.API" &&
                    Singleton<PluginManager>.instance.FindPluginInfo(a) is PluginManager.PluginInfo installerPlugin)
                {
                    CO_Harmony_API_Assembly = a;
                    return installerPlugin;
                }
            }
            return null;
        }

        internal static void OnUnavailable(HarmonyUnavailableReason reason)
        {
            unavailableReason = reason;

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
            if (reason != HarmonyUnavailableReason.HarmonyInstalledButNotActive)
                _harmonyUnavailableActions.Clear();
#else
            _harmonyUnavailableActions.Clear();
#endif
        }
        internal static void OnInstallAborted()
        {
            InstallerLite.CancelInstall();
            OnUnavailable(HarmonyUnavailableReason.InstallationCancelled);

            foreach (var mod in _harmonyDisableModsOnCancel)
            {
                Log($"[{Versioning.FULL_PACKAGE_NAME}] Disabling mod {mod.name} because Harmony Install is cancelled");
                mod.isEnabled = false;
            }
            if (_harmonyDisableModsOnCancel.Any())
            {
                _harmonyDisableModsOnCancel.Clear();

                /* FIXME: Updating all is heavy. Find a way to toggle the "active" checkbox
                 * of the affected items only
                 */
                Singleton<PluginManager>.instance.ForcePluginsChanged();
            }

        }

        static bool DoAwarenessCallbacks()
        {
            var callbacks = new List<IAwareness.ClientCallback>();
            _harmonyReadyActions.ForEach((a) => callbacks.Add(new IAwareness.ClientCallback { action = a.action, callStack = a.callStack, }));

            bool result = false;
            if (InstallerLite.InstallerRunMethod != default(MethodInfo))
            {
                /* Harmony mod 9.21 and older did not mark proper API usage in their DoOnHarmonyReady() handlers.
                    * So those marks need to be set by the API, for each action, otherwise they improperly report
                    * misuse of the API.
                    * 
                    * There is an assumption that the v9.21 Harmony is already installed, so multiple actions from different
                    * mods are not deferred, but are run immediately, otherwise Installer.run will not be able to mark each
                    * mod as good (because it checks the stacktrace of at the time Installer.Run is called, ie, when
                    * the mod is finally installed).
                    * 
                    * This is a safe assumption, because if it were not already installed, this API would install an updated
                    * Harmony instead of the v9.21
                    */
                InstallerLite.InstallerRunMethod?.Invoke(null, new object[0]);
            }

            result = (awarenessInstance as IAmAware).DoOnHarmonyReady(callbacks);

            if (result)
            {
                _harmonyReadyActions.Clear();
                _harmonyUnavailableActions.Clear();
                _harmonyWaitingMods.Clear();
            }
            return result;
        }

        static void DoGenericCallbacks()
        {
            _harmonyReadyActions.ForEach((a) =>
            {
                try
                {
                    a.action();
                }
                catch (Exception ex)
                {
                    LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] OnHarmonyReady Callback failed. It was requested from:\n{a.callStack}\n\nThe calback failure was at\n{ex.StackTrace}");
                }
            });
        }

        static void DoDeferredReadyCalls()
        {
#if HEAVY_TRACE
            Log($"[{Versioning.FULL_PACKAGE_NAME}] DoDeferredReadyCalls() m_availableHarmony={m_availableHarmony}");
#endif
            try
            {
                if (m_availableHarmony == PresentHarmonyType.None)
                {
                    m_availableHarmony = null;
                    DetectHarmonyPresence();
                }
                switch (m_availableHarmony.Value)
                {
                    case PresentHarmonyType.Aware:
                        if (DoAwarenessCallbacks())
                        {
                            RemovePluginChangeCallback();
                        }
#if CALLBACK_UNAVAILABLE_ON_DISABLED_HARMONY
                        else
                        {
                            OnUnavailable(HarmonyUnavailableReason.HarmonyInstalledButNotActive);
                        }
#endif
                        break;
                    case PresentHarmonyType.Generic:
                        DoGenericCallbacks();
                        RemovePluginChangeCallback();
                        unavailableReason = null;
                        break;

                }
            }
            catch (Exception ex)
            {
                /* Awareness1 not present */
                Log($"[{Versioning.FULL_PACKAGE_NAME}] while calling DoAwarenessCallbacks: {ex.Message}");
                LogException(ex);
            }
        }

        static PluginManager.PluginsChangedHandler doDeferredReadyCalls = null;
        static void InstallPluginChangeCallback()
        {
            if (doDeferredReadyCalls == null)
            {
                doDeferredReadyCalls = new PluginManager.PluginsChangedHandler(DoDeferredReadyCalls);
                Singleton<PluginManager>.instance.eventPluginsChanged += doDeferredReadyCalls;
            }
        }
        static void RemovePluginChangeCallback()
        {
            Singleton<PluginManager>.instance.eventPluginsChanged -= doDeferredReadyCalls;
            doDeferredReadyCalls = null;
        }

        static Harmony()
        {
            DetectHarmonyPresence();
            if (awarenessInstance != null)
                InstallerLite.StartUpdates(awarenessInstance);
        }
    }
}
