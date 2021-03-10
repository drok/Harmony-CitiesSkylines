using IAwareness;
using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using ColossalFramework;
using System;
using System.Collections.Generic;

namespace CitiesHarmony.API
{
    public static class HarmonyHelper {
        internal const ulong HarmonyModWorkshopId = 2399343344uL;

        private static bool _workshopItemInstalledSubscribed = false;
        private static List<ClientCallback> _harmonyReadyActions = new List<ClientCallback>();

        public static bool IsHarmonyInstalled {
            get {
                var empty = new List<ClientCallback>();

                return Singleton<PluginManager>.instance.GetImplementations<IAmAware>()
                .Exists((i) => i.DoOnHarmonyReady(empty));
            }
        }

        public static void EnsureHarmonyInstalled() {
            if (!IsHarmonyInstalled) {
                InstallHarmonyWorkshopItem();
            }
        }

        public static void DoOnHarmonyReady(Action action) {
            if (IsHarmonyInstalled) {
                action();
            } else {
                _harmonyReadyActions.Add(new ClientCallback() { action = action, callStack = new System.Diagnostics.StackTrace(1, false), });

                if (!_workshopItemInstalledSubscribed && SteamWorkshopAvailable) {
                    _workshopItemInstalledSubscribed = true;
                    PlatformService.workshop.eventWorkshopItemInstalled += OnWorkshopItemInstalled;
                }

                InstallHarmonyWorkshopItem();
            }
        }

        private static bool SteamWorkshopAvailable => PlatformService.platformType == PlatformType.Steam && !PluginManager.noWorkshop;

        private static void InstallHarmonyWorkshopItem() {
            // TODO show error message to the user

            if (PlatformService.platformType != PlatformType.Steam) {
                UnityEngine.Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] Cannot auto-subscribe Harmony on platforms other than Steam!");
                SubscriptionWarning.ShowOnce();
                return;
            }

            if (PluginManager.noWorkshop) {
                UnityEngine.Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] Cannot auto-subscribe Harmony in --noWorkshop mode!");
                SubscriptionWarning.ShowOnce();
                return;
            }

            if(!PlatformService.workshop.IsAvailable()) {
                UnityEngine.Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] Cannot auto-subscribe Harmony while workshop is not available");
                SubscriptionWarning.ShowOnce();
                return;
            }


            if(IsHarmonyWorkshopItemSubscribed) {
                UnityEngine.Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] Harmony workshop item is subscribed, but assembly is not loaded!");
                SubscriptionWarning.ShowOnce();
                return;
            }

            UnityEngine.Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] Subscribing to Harmony workshop item!");
            if (!PlatformService.workshop.Subscribe(new PublishedFileId(HarmonyModWorkshopId))) {
                SubscriptionWarning.ShowOnce();
            }
        }

        private static void OnWorkshopItemInstalled(PublishedFileId id) {
            if (Singleton<PluginManager>.instance.GetImplementations<IAmAware>()
                .Exists((i) => i.DoOnHarmonyReady(_harmonyReadyActions)))
            {
                UnityEngine.Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] Harmony workshop item subscribed and {_harmonyReadyActions.Count} OnReady tasks dispatched!");
            }
        }

        internal static bool IsHarmonyWorkshopItemSubscribed {
            get {
                var subscribedIds = PlatformService.workshop.GetSubscribedItems();
                if (subscribedIds == null) return false;

                foreach (var id in subscribedIds) {
                    if (id.AsUInt64 == HarmonyModWorkshopId) return true;
                }

                return false;
            }
        }
    }
}
