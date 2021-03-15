extern alias Harmony2;
using Harmony2::HarmonyLib;
using JetBrains.Annotations;
using ColossalFramework.UI;
using System;

namespace HarmonyMod.MyPatches
{

    [HarmonyPatch(typeof(UnityEngine.Debug), "LogException", new Type[] { typeof(Exception), })]
    [HarmonyPriority(Priority.First)]
    internal class UnityEngineDebug_LogException
    {
        /// <summary>
        /// when loading asset from a file, IAssetData.OnAssetLoaded() is called for all assets but the one that is loaded from the file.
        /// this postfix calls IAssetData.OnAssetLoaded() for asset loaded from file.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool Prefix(Exception exception)
        {
            /* Return false if the Plugin report was successful
             * Allows up to 3 (MAX_EXCEPTION_PROMPTS_PER_MOD) ExceptionPanel popups per mod,
             * suppressing the rest.
             *
             * All exceptions are logged and reported in the Harmony report.
             */
            Mod.mainModInstance?.report.TryReportPlugin(exception);
            return true;
        }
    }

    [HarmonyPatch(typeof(UnityEngine.Debug), "LogException", new Type[] { typeof(Exception), typeof(UnityEngine.Object),})]
    [HarmonyPriority(Priority.First)]
    internal class UnityEngineDebug_LogException_withObject
    {
        /// <summary>
        /// when loading asset from a file, IAssetData.OnAssetLoaded() is called for all assets but the one that is loaded from the file.
        /// this postfix calls IAssetData.OnAssetLoaded() for asset loaded from file.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool Prefix(Exception exception, UnityEngine.Object context)
        {
            /* Return false if the Plugin report was successful
             * Allows up to 3 (MAX_EXCEPTION_PROMPTS_PER_MOD) ExceptionPanel popups per mod,
             * suppressing the rest.
             *
             * All exceptions are logged and reported in the Harmony report.
             */
            Mod.mainModInstance?.report.TryReportPlugin(exception);
            return true;
        }
    }
}
