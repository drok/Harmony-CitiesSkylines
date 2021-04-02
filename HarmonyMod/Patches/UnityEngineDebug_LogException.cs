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
extern alias Harmony2;
using Harmony2::HarmonyLib;
using JetBrains.Annotations;
using ColossalFramework.UI;
using System;
using System.Reflection;

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

        [UsedImplicitly]
        static bool Prepare(MethodBase original) { return Patcher.harmonyAssembly.Count == 0; }
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

        [UsedImplicitly]
        static bool Prepare(MethodBase original) { return Patcher.harmonyAssembly.Count == 0; }
    }
}
