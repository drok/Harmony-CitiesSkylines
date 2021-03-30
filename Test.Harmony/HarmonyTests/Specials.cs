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
 /*
 MIT License

 Copyright (c) 2017 Felix Schmidt

 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:

 The above copyright notice and this permission notice shall be included in all
 copies or substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 SOFTWARE.
 */

 /* **************************************************************************
  * 
  * 
  * IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT
  * 
  * This file contains leftover code from the initial fork from Felix Schmidt's
  * repository https://github.com/boformer/CitiesHarmony
  * 
  * It contains known bad code, which is either not used at all in my implementation,
  * or it is in the course of being re-written. If I am rewriting it, I only included
  * it because an initial release of my project was needed urgently to address
  * a broken modding eco-system in Cities Skylines, and I considered it will do no
  * further harm over what has already been done by Felix Schmidt's code.
  * 
  * I would recommend you do not copy or rely on this code. A near future update will
  * remove this and replace it with proper code I would be proud to put my name on.
  * 
  * Until then, the copyright notice above was expressely requested by Felix Schmidt,
  * by means of a DMCA complaint at GitHub and Steam.
  * 
  * There is no code between the end of this comment and he "END-OF-Felix Schmidt-COPYRIGHT"
  * line if there is one, or the end of the file, that I, Radu Hociung, claim any copyright
  * on. The rest of the content, outside of these delimiters, is my copyright, and
  * you may copy it in accordance to the modified GPL license at the root or the repo
  * (LICENSE file)
  * 
  * FUTHER, if you base your code on a copy of the example mod from Felix Schmidt's
  * repository, which does not include his copyright notice, you will likely also
  * be a victim of DMCA complaint from him.
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
