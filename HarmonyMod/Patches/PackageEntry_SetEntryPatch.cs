extern alias Harmony2;
using Harmony2::HarmonyLib;
using JetBrains.Annotations;
using System;

namespace HarmonyMod.MyPatches
{

    [HarmonyPatch(typeof(PackageEntry), "SetEntry", new Type[] { typeof(EntryData), })]
    internal class PackageEntry_SetEntryPatch
    {
        /// <summary>
        /// when loading asset from a file, IAssetData.OnAssetLoaded() is called for all assets but the one that is loaded from the file.
        /// this postfix calls IAssetData.OnAssetLoaded() for asset loaded from file.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        public static void Postfix(PackageEntry __instance, EntryData data)
        {
            (Mod.mainMod.userModInstance as Mod).report.OnContentManagerSetEntry(__instance, data.pluginInfo);
        }
    }
}
