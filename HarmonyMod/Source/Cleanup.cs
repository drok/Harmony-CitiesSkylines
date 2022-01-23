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
using UnityEngine;
using UnityEngine.SceneManagement;
using Harmony2::HarmonyLib;
using System.Reflection;
using JetBrains.Annotations;
using System.Collections.Generic;

namespace HarmonyMod
{
	internal class Cleanup
	{
		public struct Malware
		{
			public string category;
			public System.Type[] types;
		}
		internal static Malware[] knownMalware = new Malware[] {
			new Malware() { category = "adware",
				types = new System.Type[] {
					/* Adware on the main menu */
					typeof(NewsFeedPanel),
					typeof(WorkshopAdPanel),
					typeof(WhatsNewPanelShower),
					typeof(DLCPanel),
					typeof(DLCPanelNew), }},
			new Malware() {category = "data exfiltrator",
				/* Data exfiltration to Paradox Interactive "Paradox Online Publishing Services = POPS" */
				types = new System.Type[] {
					typeof(ParadoxAccountPanel),
					typeof(PopsManager), } },
		};

		public static void StartMenu()
		{
			var starterObj = GameObject.Find("Application Starter");
			if (starterObj)
			{
				var starter = starterObj.GetComponent<Starter>();
				if (starter != null)
				{
					starter.m_loadIntro = false;
					SceneManager.sceneLoaded += NoAds;
					SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Additive);
				}
			}
		}

		public static void NoAds(Scene scene, LoadSceneMode loadMode) => RemoveMalware();

		public static void RemoveMalware()
        {
			foreach (var cat in knownMalware)
				foreach (var type in cat.types)
					foreach (var obj in GameObject.FindObjectsOfType(type))
						if (obj is MonoBehaviour malware)
						{
							malware.gameObject.SetActive(false);
							GameObject.DestroyImmediate(malware);
						}
		}
	}

	[HarmonyPatch]
	static class MalwareCleanup_Patches
	{
		public static IEnumerable<MethodBase> TargetMethods()
		{
			foreach (var cat in Cleanup.knownMalware)
			{
				foreach (var type in cat.types)
				{
					var m = type.GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
					if (m is not null) yield return m;
				}
			}
		}

		[HarmonyPrefix]
		[UsedImplicitly]
		public static bool PrefixFunction()
		{
			return false;
		}
		[UsedImplicitly]
		static bool Prepare(MethodBase original) { return Patcher.harmonyAssembly.Count == 0; }
	}

}
