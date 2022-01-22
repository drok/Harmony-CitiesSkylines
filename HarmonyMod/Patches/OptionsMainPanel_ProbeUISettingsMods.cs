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
using System;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Plugins;
using ICities;

namespace HarmonyMod.MyPatches
{

    [HarmonyPatch(typeof(OptionsMainPanel), "ProbeUISettingsMods", new Type[] { })]
    internal class OptionsMainPanel_ProbeUISettingsMods
    {
        /// <summary>
        /// when loading asset from a file, IAssetData.OnAssetLoaded() is called for all assets but the one that is loaded from the file.
        /// this postfix calls IAssetData.OnAssetLoaded() for asset loaded from file.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool Prefix(OptionsMainPanel __instance, ref object __result)
        {
            __result = false;
            foreach (PluginManager.PluginInfo pluginInfo in Singleton<PluginManager>.instance.GetPluginsInfo())
            {
                if (pluginInfo.isEnabled)
                {
                    IUserMod[] instances = pluginInfo.GetInstances<IUserMod>();
                    if (instances.Length == 1)
                    {
                        try
                        {

                            MethodInfo method = instances[0]?.GetType().GetMethod("OnSettingsUI", BindingFlags.Instance | BindingFlags.Public);
                            if (method != null)
                            {
                                __result = true;
                                break;
                            }
                        } catch(Exception ex)
                        {
                            Mod.mainModInstance.report.ReportPlugin(pluginInfo, ModReport.ProblemType.ExceptionThrown, ex, "Faulty Settings UI");
                        }

                    }
                }
            }
            return false;
        }

        [UsedImplicitly]
        static bool Prepare(MethodBase original) { return Patcher.harmonyAssembly.Count == 0; }
    }
}
