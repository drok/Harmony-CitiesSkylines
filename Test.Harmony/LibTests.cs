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
using IAwareness;
using System;
#if API_1_0_3 || API_1_0_4 || API_1_0_5 || API_1_0_6 || API_2_0_0
using CitiesHarmony.API;
#else
using HarmonyManager;
#endif
using ICities;
using UnityEngine;
using System.Linq;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Plugins;

namespace HarmonyMod.Tests
{

    public class TestFailed : Exception
    {
        public TestFailed(string message)
            : base(message) { }

        public TestFailed(string message, Exception ex)
            : base(message, ex) { }
    }

    public class TesterMod : IUserMod {
        public string Name => "_Test.Harmony";

#if API_1_0_3 || API_1_0_4 || API_1_0_5 || API_1_0_6 || API_2_0_0
        internal static string APINAME = typeof(HarmonyHelper).Assembly.GetName().Name + " " + typeof(HarmonyHelper).Assembly.GetName().Version;
#else
        internal static string APINAME = typeof(Harmony).Assembly.GetName().Name + " " + typeof(Harmony).Assembly.GetName().Version;
#endif
        public string Description => $"Test Harmony API \"{APINAME}\"";

        public void OnEnabled() {
            Debug.LogError($"Running TesterMod.OnEnabled() {Assembly.GetExecutingAssembly().GetName().Version}");

            TestRunner.Run();
        }

        public void OnDisabled()
        {
            Debug.LogError("Running TesterMod.OnDisabled() {Assembly.GetExecutingAssembly().GetName().Version}");
        }
    }

    public static class TestRunner {
        public static void Run() {
            HarmonyLib.Harmony.DEBUG = true;

            try
            {
                new AttributePatchTest().Run();
                new ACLTest().Run();

// #if USE_ISAWARE_TYPE
                bool hamonyModTestsDone = Singleton<PluginManager>.instance.GetPluginsInfo()
                    .Where((p) => p.isEnabled && p.userModInstance is IAmAware)
                    .All(
                        (p) => {
                            new ACLTest().RunAfterHarmony();
                            return true;
                        });
                if (!hamonyModTestsDone)
                {
                    Singleton<PluginManager>.instance.eventPluginsChanged += OnModEnabled;
                }
// #endif
            }
            catch (TestFailed ex)
            {
                throw new TestFailed($"Test {TesterMod.APINAME} failed {ex.Message}", ex);
            }
        }

//#if USE_ISAWARE_TYPE
        static void OnModEnabled()
        {
            bool hamonyModTestsDone = Singleton<PluginManager>.instance.GetPluginsInfo()
                .Where((p) => p.isEnabled && p.userModInstance is IAmAware)
                .All(
                (p) => {
                    try
                    {
                        new ACLTest().RunAfterHarmony();
                    }
                    catch (TestFailed ex)
                    {
                        throw new TestFailed($"Test {TesterMod.APINAME} failed {ex.Message}", ex);
                    }
                    return true;
                });

            if (hamonyModTestsDone)
            {
                Singleton<PluginManager>.instance.eventPluginsChanged -= OnModEnabled;
            }
    }
//#endif
    
}

        }
