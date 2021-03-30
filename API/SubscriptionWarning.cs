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

using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using static ColossalFramework.Plugins.PluginManager;
using ColossalFramework.UI;
using ColossalFramework;
using System;
using System.Reflection;
using System.Text;
using ICities;

namespace CitiesHarmony.API
{
    internal static class SubscriptionWarning {
        private const string Marker = "Harmony2SubscriptionWarning";

        public static void ShowOnce() {
            if (UnityEngine.GameObject.Find(Marker)) return;

            var go = new UnityEngine.GameObject(Marker);
            UnityEngine.Object.DontDestroyOnLoad(go);

            if(LoadingManager.instance.m_currentlyLoading || UIView.library == null) {
                LoadingManager.instance.m_introLoaded += OnIntroLoaded;
                LoadingManager.instance.m_levelLoaded += OnLevelLoaded;
            } else {
                Show();
            }
        }

        private static void OnIntroLoaded() {
            LoadingManager.instance.m_introLoaded -= OnIntroLoaded;
            Show();
        }

        private static void OnLevelLoaded(SimulationManager.UpdateMode updateMode) {
            LoadingManager.instance.m_levelLoaded -= OnLevelLoaded;
            Show();
        }

        public static void Show() {
            string reason = "An error occured while attempting to automatically subscribe to Harmony (no network connection?)";
            string solution = "You can manually download the Harmony mod from github.com/boformer/CitiesHarmony/releases";
            if (PlatformService.platformType != PlatformType.Steam) {
                reason = "Harmony could not be installed automatically because you are using a platform other than Steam.";
            } else if (PluginManager.noWorkshop) {
                reason = "Harmony could not be installed automatically because you are playing in --noWorkshop mode!";
                solution = "Restart without --noWorkshop or manually download the Harmony mod from github.com/boformer/CitiesHarmony/releases";
            } else if(!PlatformService.workshop.IsAvailable()) {
                reason = "Harmony could not be installed automatically because the Steam workshop is not available (no network connection?)";
            } else if(HarmonyHelper.IsHarmonyWorkshopItemSubscribed) {
                reason = "It seems that Harmony has already been subscribed, but Steam failed to download the files correctly or they were deleted.";
                solution = $"Close the game, then unsubscribe and resubscribe the Harmony workshop item from steamcommunity.com/sharedfiles/filedetails/?id={HarmonyHelper.HarmonyModWorkshopId}";
            }

            /* FIXME: 
             */
            var affectedAssemblyNames = new StringBuilder();

            Singleton<PluginManager>.instance.GetImplementations<IUserMod>()
                .ForEach((i) =>
               {
                   if (Array.Exists(i.GetType().Assembly.GetReferencedAssemblies(), 
                       (a) => a.Name == "CitiesHarmony.API" || a.Name == "Harmony.API"))
                   {
                       affectedAssemblyNames.Append("• ").Append(i.Name).Append('\n');
                   }
               });

            var message = $"The mod(s):\n{affectedAssemblyNames} require the dependency 'Harmony' to work correctly!\n\n{reason}\n\n{solution}";

            UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Missing dependency: Harmony", message, false);
        }
    }
}
