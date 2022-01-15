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
    internal class ACLTest
    {
        public void Run()
        {
            HarmonyLib.Harmony h = null;
            string hid = $"ACL-{TesterMod.APINAME}";
            try
            {
                h = new HarmonyLib.Harmony(hid);
                if (h != null)
                {
                    AttemptProhibitedUnpatchAll(h);
                    AttemptProhibitedPatch(h);
                }
            }
            catch (Exception ex)
            {
                throw new TestFailed("Test ACL", ex);
            }
        }
        public void RunAfterHarmony()
        {
            HarmonyLib.Harmony h = null;
            string hid = $"ACL-{TesterMod.APINAME}";
            try
            {
                h = new HarmonyLib.Harmony(hid);
                if (h != null)
                {
                    AttemptRemoveHarmonyModPatch(h);
                }
            }
            catch (Exception ex)
            {
                throw new TestFailed("Test ACL", ex);
            }
        }
        public void AttemptProhibitedUnpatchAll(HarmonyLib.Harmony h)
        {
            testName = "TEST/" +
                "API/ACL1/" + TesterMod.APINAME;

            try
            {
                h.UnpatchAll();
                throw new TestFailed("Global UnpatchAll() should throw");
            }
            catch (HarmonyException ex)
            {
                if (ex.GetType().Name != "HarmonyUserException" || ex.Message != "Prohibited global UnpatchAll()")
                {
                    throw new TestFailed("Global UnpatchAll() failed but is not prohibited");
                }
                UnityEngine.Debug.Log($"[{testName}] INFO - Prohibited UnpatchAll() works. OK");
            }
            catch (Exception ex)
            {
                throw new TestFailed("Global Unpatch", ex);
            }

        }
        public void AttemptProhibitedPatch(HarmonyLib.Harmony h)
        {
            testName = "TEST/" +
                "API/ACL2/" + TesterMod.APINAME;

            PatchProcessor processor = null;
            MethodInfo prohibitedPatch = null;
            try
            {
                var targetName = "CompareTo";
                MethodInfo target = typeof(Patch).GetMethod(targetName,
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);
                if (target == null)
                {
                    throw new TestFailed($"Target fn ({targetName}) was not found.");
                }
                processor = h.CreateProcessor(target);
                if (processor == null)
                {
                    UnityEngine.Debug.Log($"[{testName}] INFO - Processor=null. BAD");
                    throw new TestFailed($"Failed to create processor.");
                }

                prohibitedPatch = typeof(ACLPatchDefinitions).GetMethod("ProhibitedPatch",
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);
                if (prohibitedPatch == null)
                {
                    throw new TestFailed("Patch fn (ProhibitedPatch) was not found.");
                }

                processor.AddPostfix(prohibitedPatch);
                processor.Patch();
                throw new TestFailed("Installing a prohibited patch did not throw HarmonyModACLException");
            }
            catch (TestFailed ex)
            {
                UnityEngine.Debug.Log($"[{testName}] ERROR : {ex.Message}. BAD");
                throw ex;
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name != "HarmonyModACLException")
                {
                    throw new TestFailed($"Installing a prohibited patch failed but not because of ACL: {ex.Message}");
                }
                UnityEngine.Debug.Log($"[{testName}] INFO - Attempting to install a prohibited patch was blocked ({ex.Message}). OK.");

            }
            finally
            {
                if (processor != null && prohibitedPatch != null)
                {
                    try
                    {
                        processor.Unpatch(prohibitedPatch);
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType().Name != "HarmonyModACLException")
                        {
                            throw new TestFailed($"Uninstalling a prohibited patch failed but not because of ACL: {ex.Message}");
                        }
                        UnityEngine.Debug.Log($"[{testName}] INFO - Attempting to remove a prohibited patch was blocked ({ex.Message}). OK.");
                    }

                }
                else
                {
                    throw new TestFailed($"Prohibited patch test is broken. processor={processor != null} patch={prohibitedPatch != null}");
                }
            }
        }

        public void AttemptRemoveHarmonyModPatch(HarmonyLib.Harmony h)
        {
            /* Needs to run after Harmony is OnEnabled() */

            testName = "TEST/" +
                "API/ACL3/" + TesterMod.APINAME;

            PatchProcessor processor = null;
            MethodInfo prohibitedPatch = null;
            try
            {
                var targetName = "SetEntry";
                MethodInfo target = typeof(PackageEntry).GetMethod(targetName,
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);
                if (target == null)
                {
                    throw new TestFailed($"Target fn ({targetName}) was not found.");
                }
                processor = h.CreateProcessor(target);
                if (processor == null)
                {
                    UnityEngine.Debug.Log($"[{testName}] INFO - Processor=null. BAD");
                    throw new TestFailed($"Failed to create processor.");
                }

                var patches = HarmonyLib.Harmony.GetPatchInfo(target);

                if (patches == null)
                {
                    throw new TestFailed($"Failed to get '{target}' patches.");
                }

                if (patches.Prefixes != null) 
                    UnityEngine.Debug.Log($"[{testName}] INFO - found {patches.Prefixes.Count} prefix patches to {targetName}");
                if (patches.Postfixes != null)
                    UnityEngine.Debug.Log($"[{testName}] INFO - found {patches.Postfixes.Count} postfix patches to {targetName}");
                if (patches.Transpilers!= null)
                    UnityEngine.Debug.Log($"[{testName}] INFO - found {patches.Transpilers.Count} transpiler patches to {targetName}");
                if (patches.Finalizers != null)
                    UnityEngine.Debug.Log($"[{testName}] INFO - found {patches.Finalizers.Count} finalizer patches to {targetName}");

                    patches.Postfixes.Do((p) => UnityEngine.Debug.Log($"[{testName}] INFO - found patch to {targetName} = {p.PatchMethod.Name} by {p.owner}"));

                bool found = false;
                patches.Postfixes.DoIf((p) => p.owner.Contains("org.ohmi.harmony"),
                    (p) => {
                        found = true;
                        processor.Unpatch(p.PatchMethod);
                    });

                if (!found)
                {
                    throw new TestFailed($"Mod's patch to {targetName} not found.");
                }

                throw new TestFailed("Removing a Mod's patch did not throw HarmonyModACLException");
            }
            catch (TestFailed ex)
            {
                UnityEngine.Debug.Log($"[{testName}] ERROR : {ex.Message}. BAD");
                throw ex;
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name != "HarmonyModACLException")
                {
                    throw new TestFailed($"Unpatching the mod failed but not because of ACL: {ex.Message}", ex);
                }
                UnityEngine.Debug.Log($"[{testName}] INFO - Attempting to unpatch the mod was blocked ({ex.Message}). OK.");

            }
        }

        internal static string testName;

        public int lastArg;
    }

    internal static class ACLPatchDefinitions
    {
        public static void ProhibitedPatch()
        {
        }

    }

}
