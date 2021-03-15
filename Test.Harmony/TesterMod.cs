using IAwareness;
using System;
using CitiesHarmony.API;
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
        public string Description => $"Test Harmony API \"{typeof(HarmonyHelper).Assembly.GetName().Name} {typeof(HarmonyHelper).Assembly.GetName().Version}\"";

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
            Harmony.DEBUG = true;

            ReturnColorTest.Run();


            int[] values = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            foreach (var n in values) Specials.Test_Patch_Returning_Structs(n, "S");
            foreach (var n in values) Specials.Test_Patch_Returning_Structs(n, "I");

            try
            {
                new AttributePatchTest().Run();
                new ACLTest().Run();

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
            }
            catch (TestFailed ex)
            {
                throw new TestFailed($"Test API-{typeof(HarmonyHelper).Assembly.GetName().Version} failed {ex.Message}", ex);
            }
        }

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
                        throw new TestFailed($"Test API-{typeof(HarmonyHelper).Assembly.GetName().Version} failed {ex.Message}", ex);
                    }
                    hamonyModTestsDone = true;
                });

            if (hamonyModTestsDone)
            {
                Singleton<PluginManager>.instance.eventPluginsChanged -= OnModEnabled;
            }
        }
    }

    public static class ReturnColorTest {
        public static void Run() {
            var harmony = new Harmony("boformer.Harmony2Example");
            harmony.Patch(typeof(MyClass).GetMethod("GetColorStatic"), null, new HarmonyMethod(typeof(ReturnColorTest).GetMethod("GetColor_Postfix")));
            harmony.Patch(typeof(MyClass).GetMethod("GetColor"), null, new HarmonyMethod(typeof(ReturnColorTest).GetMethod("GetColor_Postfix")));


            var colorStatic = MyClass.GetColorStatic();
            UnityEngine.Debug.Log("colorStatic.g: " + colorStatic.g);
            UnityEngine.Debug.Log(colorStatic.ToString());

            var myClass = new MyClass();

            var color = myClass.GetColor();
            UnityEngine.Debug.Log("color.g: " + color.g);
            UnityEngine.Debug.Log(color.ToString());
        }

        public static void GetColor_Postfix(ref UnityEngine.Color __result) {
            UnityEngine.Debug.Log("GetColor__Postfix: __result.g: " + __result.g);
        }

        public class MyClass {
            public static UnityEngine.Color GetColorStatic() {
                UnityEngine.Debug.Log("GetColorStatic");
                return new UnityEngine.Color(1f, 0.75f, 0.5f, 0.25f);
            }

            public UnityEngine.Color GetColor() {
                UnityEngine.Debug.Log("GetColor");
                return new UnityEngine.Color(1f, 0.75f, 0.5f, 0.25f);
            }
        }
    }
}
