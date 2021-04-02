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
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework.Plugins;
using static ColossalFramework.Plugins.PluginManager;


namespace HarmonyMod.MyPatches
{

    [HarmonyPatch(typeof(PluginInfo), "get_userModInstance", new Type[] { })]
    internal class PluginInfo_userModInstancePatch
    {
        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool Prefix(PluginInfo __instance, ref object __result)
        {
			var inst = Traverse.Create(__instance);
			var userModInstance = inst.Field<object>("m_UserModInstance");
			var m_UserModInstance = userModInstance.Value;
			List<Assembly> m_Assemblies = inst.Field<List<Assembly>>("m_Assemblies").Value;

#if HEAVY_TRACE
			UnityEngine.Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] PluginInfo.get_userModInstance() for {__instance.name} called from \n{new System.Diagnostics.StackTrace(true)}"); // : {__exception.Message}\n{__exception.StackTrace}");
#endif

			__result = null;
			if (m_UserModInstance == null)
			{
				for (int i = 0; i < m_Assemblies.Count; i++)
				{
					foreach (Type type in m_Assemblies[i].GetExportedTypes())
					{
						if (type.IsClass && !type.IsAbstract)
						{
							Type[] interfaces = type.GetInterfaces();
							if (interfaces.Contains(PluginManager.userModType))
							{
								ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
								m_UserModInstance = constructor.Invoke(null);
								userModInstance.Value = m_UserModInstance;
							}
						}
					}
				}
			}
			__result = m_UserModInstance;

			return false;
		}

		[HarmonyFinalizer]
        [UsedImplicitly]
        public static Exception Finalizer(Exception __exception, PluginInfo __instance)
        {
			if (__exception is not null)
            {
				if (__exception is ReflectionTypeLoadException)
				{
					(__exception as ReflectionTypeLoadException).LoaderExceptions.Do(
						(e) =>
						{
							e.HelpLink = "https://github.com/drok/Harmony-CitiesSkylines/issues/9";
							Mod.mainModInstance.report.ReportPlugin(__instance, ModReport.ProblemType.ExceptionThrown, e, $"{e.GetType().Name}: {e.Message}");
							UnityEngine.Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] {__instance.name} threw: {e.GetType().Name}: {e.Message}");
						});
				}
				else
				{
					Mod.mainModInstance.report.ReportPlugin(__instance, ModReport.ProblemType.ExceptionThrown, __exception, $"while instatiating: {__exception.Message}");
#if TRACE
					UnityEngine.Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] PluginInfo.get_userModInstance() for {__instance.name} threw {__exception.GetType().FullName}");
#endif
				}
			}

			return null;
        }
	
		[UsedImplicitly]
		static bool Prepare(MethodBase original) { return Patcher.harmonyAssembly.Count == 0; }
	}
}
