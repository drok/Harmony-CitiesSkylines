/*
 * Harmony for Cities Skylines
 *  Copyright (C) 2021 Radu Hociung <radu.csmods@ohmi.org>
 *  
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *  
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *  
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */
/* Borrowed from CitiesHarmony mod by boformer - thank you!
*/
using HarmonyLib;
using HarmonyLibTests.Assets.Methods;
using System;

namespace HarmonyMod.Tests
{
	public static class Specials {
		// Based on HarmonyLib test "Test_Patch_Returning_Structs", adjusted to be run ingame
		public static void Test_Patch_Returning_Structs(int n, string type) {
			var name = $"{type}M{n:D2}";

			var patchClass = typeof(ReturningStructs_Patch);

			var prefix = SymbolExtensions.GetMethodInfo(() => ReturningStructs_Patch.Prefix(null));

			var instance = new Harmony("returning-structs");

			var cls = typeof(ReturningStructs);
			var method = AccessTools.DeclaredMethod(cls, name);
			if (method == null) throw new Exception("method == null");

			UnityEngine.Debug.Log($"Test_Returning_Structs: patching {name} start");
			try {
				var replacement = instance.Patch(method, new HarmonyMethod(prefix));
				if (replacement == null) throw new Exception("replacement == null");
			} catch (Exception ex) {
				UnityEngine.Debug.Log($"Test_Returning_Structs: patching {name} exception: {ex}");
			}
			UnityEngine.Debug.Log($"Test_Returning_Structs: patching {name} done");

			var clsInstance = new ReturningStructs();
			try {
				UnityEngine.Debug.Log($"Test_Returning_Structs: running patched {name}");

				var original = AccessTools.DeclaredMethod(cls, name);
				if (original == null) throw new Exception("original == null");
				var result = original.Invoke(type == "S" ? null : clsInstance, new object[] { "test" });
				if (result == null) throw new Exception("result == null");
				if ($"St{n:D2}" != result.GetType().Name) throw new Exception($"invalid result type name: {result.GetType().Name}");

				var field = result.GetType().GetField("b1");
				var fieldValue = (byte)field.GetValue(result);
				UnityEngine.Debug.Log(fieldValue);
				if (fieldValue != 42) throw new Exception($"result scrambled!");

				UnityEngine.Debug.Log($"Test_Returning_Structs: running patched {name} done");
			} catch (Exception ex) {
				UnityEngine.Debug.Log($"Test_Returning_Structs: running {name} exception: {ex}");
			}
		}
	}
}
