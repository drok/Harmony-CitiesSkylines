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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using HarmonyLib;

namespace HarmonyMod.Tests
{
    internal class AttributePatchTest
    {
        public void Run()
        {
            HarmonyLib.Harmony h = null;
            string hid = "AttributePatchTest-" + TesterMod.APINAME;
            try
            {
                h = new HarmonyLib.Harmony(hid);
                h.PatchAll();
            } catch (Exception ex)
            {
                throw new TestFailed("Patching with Attributes", ex);
            }
            if (h != null)
            {
                Run("PatchAll()");
                try
                {
                    h.UnpatchAll(hid);
                }
                catch (Exception ex)
                {
                    throw new TestFailed("Unpatching with Attributes", ex);
                }
            }
        }

        const int UNTOUCHED = 0x55aa;
        internal static string testName;
        public void Run(string subtest)
        {
            testName = "TEST/" +
                "API/" + TesterMod.APINAME;

            lastArg = UNTOUCHED;
            for (int i = 0; i < 30; i+=5)
            {
                int expected_result =
                    /* prefix patch active */
                    i < 10 ? 100 * i :

                    /* postfix patch active */
                    i < 20 ? 200 * i :

                    /* transpiler patch active */
                    i < 30 ? i :
                    
                    /* No patch active */
                    i * 1000;

                int result = PatchTarget(i);
                if (expected_result != result)
                {
                    throw new TestFailed($"Failed {subtest} Test. Expected {expected_result} got {result} for input={i}");
                }

                if (i < 10)
                {
                    if (lastArg != UNTOUCHED)
                    {
                        throw new TestFailed($"Failed {subtest} Test. LastArg set wrong. Expected {UNTOUCHED} got {lastArg}");
                    }

                } else if (i < 20)
                {
                    if (lastArg != i)
                    {
                        throw new TestFailed($"Failed {subtest} Test. LastArg set wrong. Expected {i} got {lastArg}");
                    }
                }
                else if (i < 30)
                {
#if true
                    expected_result = 1000 + i;
                    if (lastArg != expected_result)
                    {
                        throw new TestFailed($"Failed {subtest} Test. LastArg set wrong. Expected {expected_result} got {lastArg}");
                    }
#endif
                }
            }
        }

        public int lastArg;
        int PatchTarget(int myArg)
        {
            lastArg = myArg;
            // PatchDefinitions.MyExtraMethod(this, myArg);

            UnityEngine.Debug.Log($"[{testName}] INFO - PatchTarget({myArg}) => {myArg} ; lastArg = {lastArg}");
            return myArg;
        }
    }

    [HarmonyPatch(typeof(AttributePatchTest), "PatchTarget")]
    internal static class PatchDefinitions
    {

        [HarmonyPrefix]
        static bool Target_Prefix(AttributePatchTest __instance, int myArg, ref int __result)
        {
            if (myArg < 10)
            {
                __result = myArg * 100;
                UnityEngine.Debug.Log($"[{AttributePatchTest.testName}] INFO - Target_Prefix({myArg}) => {__result}");
                return false;
            }
            UnityEngine.Debug.Log($"[{AttributePatchTest.testName}] INFO - Target_Prefix({myArg}) ... nop");
            return true;
        }

        [HarmonyPostfix]
        static void Target_Postfix(AttributePatchTest __instance, int myArg, ref int __result)
        {
            if (myArg >= 10 && myArg < 20)
            {
                UnityEngine.Debug.Log($"[{AttributePatchTest.testName}] INFO - Target_Postfix({myArg}) => {__result} -> {myArg * 200}");
                __result = myArg * 200;
            } else
            {
                UnityEngine.Debug.Log($"[{AttributePatchTest.testName}] INFO - Target_Postfix({myArg}) ... nop");
            }
        }

#if true
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Target_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo f_lastArg = AccessTools.Field(typeof(AttributePatchTest), "lastArg")
                ?? throw new TestFailed("Could not find lastArg field");

            //MethodInfo m_MyExtraMethod = typeof(PatchDefinitions).GetMethod("MyExtraMethod",
            //        BindingFlags.NonPublic |
            //        BindingFlags.Static |
            //        BindingFlags.Instance);

            MethodInfo m_MyExtraMethod = AccessTools.DeclaredMethod(
                typeof(PatchDefinitions), nameof(MyExtraMethod))
                        ?? throw new TestFailed("cound not find MyExtraMethod()");

            if (m_MyExtraMethod == null)
            {
                throw new TestFailed("Could not find MyExtraMethod()");
            }

            UnityEngine.Debug.Log($"[{AttributePatchTest.testName}] INFO - Target_Transpiler({instructions.Count()} instructions)");

            var found = false;
            foreach (var instruction in instructions)
            {
                if (instruction.StoresField(f_lastArg))
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // load instance ref
                    yield return new CodeInstruction(OpCodes.Ldarg_1); // load argument info
                    yield return new CodeInstruction(OpCodes.Call, m_MyExtraMethod); // Call MyExtraMethod.
                    yield return new CodeInstruction(OpCodes.Nop); // not sure why this is needed

                    found = true;
                } else 
                {
                    yield return instruction;
                }
            }
            if (found is false)
                throw new TestFailed($"Transpiler did not find taget instruction.");
        }
#endif

        public static void MyExtraMethod(AttributePatchTest inst, int theArg)
        {
            if (theArg >= 20 && theArg < 30)
            {
                inst.lastArg = 1000 + theArg;
            }
        }

    }

}
