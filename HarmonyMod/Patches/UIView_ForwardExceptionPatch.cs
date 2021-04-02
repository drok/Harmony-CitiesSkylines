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

    [HarmonyPatch(typeof(UIView), "ForwardException", new Type[] { typeof(Exception), })]
    [HarmonyPriority(Priority.First)]
    internal class UIView_ForwardExceptionPatch
    {
        /// <summary>
        /// when loading asset from a file, IAssetData.OnAssetLoaded() is called for all assets but the one that is loaded from the file.
        /// this postfix calls IAssetData.OnAssetLoaded() for asset loaded from file.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool Prefix(Exception e)
        {
            /* Return false if the Plugin report was successful
             * Allows up to 3 (MAX_EXCEPTION_PROMPTS_PER_MOD) ExceptionPanel popups per mod,
             * suppressing the rest.
             *
             * All exceptions are logged and reported in the Harmony report.
             */
            return (Mod.mainMod.userModInstance as Mod).report.TryReportPlugin(e) <= Report.MAX_EXCEPTION_PROMPTS_PER_MOD;
        }
    
        [UsedImplicitly]
        static bool Prepare(MethodBase original) { return Patcher.harmonyAssembly.Count == 0; }
    }

    /* TODO: Hook UnityEngine.DebugLogHandler.LogException(), but it gets the same exceptions as UIView.
     * Also possibly hook Starter.OnDestroy (not declared) to capture logs to the last breath of the app.
     * 
     *     public void LogException(Exception exception, Object context)
     *    {
     *        DebugLogHandler.Internal_LogException(exception, context);
     *    }
     *    
     * NullReferenceException: Object reference not set to an instance of an object
     *   at HarmonyMod.Mod.get_assemblyLocations () [0x00001] in U:\proj\skylines\CitiesHarmony\HarmonyMod\Source\Mod.cs:81 
     *   at HarmonyMod.Report.GetModList (Boolean showOnlyProblems) [0x001d3] in U:\proj\skylines\CitiesHarmony\HarmonyMod\Source\Report.cs:181 
     *   at HarmonyMod.Report.OutputReport (HarmonyMod.Mod self, Boolean final, System.String stepName) [0x00001] in U:\proj\skylines\CitiesHarmony\HarmonyMod\Source\Report.cs:277 
     *   at HarmonyMod.Mod.ICities.ILoadingExtension.OnReleased () [0x00001] in U:\proj\skylines\CitiesHarmony\HarmonyMod\Source\Mod.cs:275 
     *   at LoadingWrapper.OnLoadingExtensionsReleased () [0x00000] in <filename unknown>:0 
     * UnityEngine.DebugLogHandler:Internal_LogException(Exception, Object)
     * UnityEngine.DebugLogHandler:LogException(Exception, Object)
     * UnityEngine.Logger:LogException(Exception, Object)
     * UnityEngine.Debug:LogException(Exception)
     * LoadingWrapper:OnLoadingExtensionsReleased()
     * LoadingWrapper:Release()
     * LoadingManager:ReleaseRelay()
     * LoadingManager:OnDestroy()
     *
     */

}
