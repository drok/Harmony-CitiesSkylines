/*
MIT License

Copyright (c) 2017 Felix Schmidt

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

/* **************************************************************************
 * 
 * 
 * IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT
 * 
 * This file contains leftover code from the initial fork from Felix Schmidt's
 * repository https://github.com/boformer/CitiesHarmony
 * 
 * It contains known bad code, which is either not used at all in my implementation,
 * or it is in the course of being re-written. If I am rewriting it, I only included
 * it because an initial release of my project was needed urgently to address
 * a broken modding eco-system in Cities Skylines, and I considered it will do no
 * further harm over what has already been done by Felix Schmidt's code.
 * 
 * I would recommend you do not copy or rely on this code. A near future update will
 * remove this and replace it with proper code I would be proud to put my name on.
 * 
 * Until then, the copyright notice above was expressely requested by Felix Schmidt,
 * by means of a DMCA complaint at GitHub and Steam.
 * 
 * There is no code between the end of this comment and he "END-OF-Felix Schmidt-COPYRIGHT"
 * line if there is one, or the end of the file, that I, Radu Hociung, claim any copyright
 * on. The rest of the content, outside of these delimiters, is my copyright, and
 * you may copy it in accordance to the modified GPL license at the root or the repo
 * (LICENSE file)
 * 
 * FUTHER, if you base your code on a copy of the example mod from Felix Schmidt's
 * repository, which does not include his copyright notice, you will likely also
 * be a victim of DMCA complaint from him.
 */

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
