using IAwareness;
using System;
#if API_1_0_3 || API_1_0_4 || API_1_0_5 || API_1_0_6 || API_2_0_0
using CitiesHarmony.API;
#else
using Harmony;
#endif
using HarmonyLib;
using ICities;
using UnityEngine;
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
        internal static string APINAME = typeof(Harmony.Harmony).Assembly.GetName().Name + " " + typeof(Harmony.Harmony).Assembly.GetName().Version;
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
                bool hamonyModTestsDone = false;
                Singleton<PluginManager>.instance.GetPluginsInfo().DoIf(
                    (p) => p.isEnabled && p.userModInstance is IAmAware,
                    (p) => {
                        hamonyModTestsDone = true;
                        new ACLTest().RunAfterHarmony();
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
            bool hamonyModTestsDone = false;
            Singleton<PluginManager>.instance.GetPluginsInfo().DoIf(
                (p) => p.isEnabled && p.userModInstance is IAmAware,
                (p) => {
                    try
                    {
                        new ACLTest().RunAfterHarmony();
                    }
                    catch (TestFailed ex)
                    {
                        throw new TestFailed($"Test {TesterMod.APINAME} failed {ex.Message}", ex);
                    }
                    hamonyModTestsDone = true;
                });

            if (hamonyModTestsDone)
            {
                Singleton<PluginManager>.instance.eventPluginsChanged -= OnModEnabled;
            }
    }
//#endif
    
}

        }
