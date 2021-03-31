extern alias Harmony2;
using Harmony2::HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Reflection;
using ColossalFramework.Plugins;

namespace HarmonyMod.MyPatches
{

    [HarmonyPatch(typeof(PluginManager), "OnDestroy", new Type[] {})]
    internal class PluginManager_OnDestroyPatch
    {
        [HarmonyPrefix]
        [UsedImplicitly]
        public static void Prefix(PluginManager __instance)
        {
            Mod.mainModInstance.OnPluginManagerDestroyStart();
        }

        [HarmonyFinalizer]
        [UsedImplicitly]
        public static Exception Finalizer(Exception __exception)
        {
            if (__exception is Exception)
            {
                UnityEngine.Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR - PluginManager.OnDestroy() threw {__exception.GetType().FullName}: {__exception.Message}\n{__exception.StackTrace}");
            }
            Mod.mainModInstance.OnPluginManagerDestroyDone();
            return null;
        }

        [UsedImplicitly]
        static bool Prepare(MethodBase original) { return Patcher.harmonyAssembly.Count == 0; }
    }
}
