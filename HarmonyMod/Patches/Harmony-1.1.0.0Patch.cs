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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;


namespace HarmonyMod.MyPatches.Harmony_1_1_0_0
{
    using HarmonyMod;
    using IAwareness;

    internal class Apply
    {
        static Version To = new Version(1, 1, 0, 0);
        public static Assembly targetAssembly;

        static Apply()
        {
            Patcher.harmonyAssembly.TryGetValue(Apply.To, out targetAssembly);

            if (targetAssembly != null)
            {
                StateTransfer.Transfer(targetAssembly);
            }
        }
    }

    internal class StateTransfer
    {
        static bool done = false;

        internal static void Transfer(Assembly assembly)
        {
            if (!done)
            {
#if TRACE
                UnityEngine.Debug.LogError($"[{Versioning.FULL_PACKAGE_NAME}] Transferring Harmony {assembly.GetName().Version} state");
#endif

                try
                {
                    var Create = assembly.GetType("Harmony.HarmonyInstance").GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
                    var GetPatchedMethods = assembly.GetType("Harmony.HarmonySharedState").GetMethod("GetPatchedMethods", BindingFlags.NonPublic | BindingFlags.Static);
                    var GetPatchInfo = assembly.GetType("Harmony.HarmonySharedState").GetMethod("GetPatchInfo", BindingFlags.NonPublic | BindingFlags.Static);
                    var Unpatch = assembly.GetType("Harmony.HarmonyPatchType").GetMethod("RemovePatch", new Type[] { typeof(MethodBase), assembly.GetType("Harmony.HarmonyPatchType"), typeof(string), });
                    var AllPatches = Enum.ToObject(assembly.GetType("Harmony.HarmonyPatchType"), 0);

                    var infoType = assembly.GetType("Harmony.PatchInfo");
                    var prefixes = infoType.GetField("prefixes");
                    var postfixes = infoType.GetField("postfixes");
                    var transpilers = infoType.GetField("transpilers");

                    var patchType = assembly.GetType("Harmony.Patch");
                    var owner = patchType.GetField("owner");
                    var priority = patchType.GetField("priority");
                    var before = patchType.GetField("before");
                    var after = patchType.GetField("after");
                    var patch = patchType.GetField("patch");

                    var patchedMethods = GetPatchedMethods.Invoke(null, new object[0]) as IEnumerable<MethodBase>;

                    var patchers = new Dictionary<string, Harmony>();

                    Harmony GetPatcher(string id)
                    {
                        if (patchers.TryGetValue(id, out var h))
                        {
                            return h;
                        }
                        else
                        {
                            h = new Harmony(id);
                            patchers[id] = h;
                        }
                        return h;
                    }

                    HarmonyMethod GetHarmonyMethod(object p)
                    {
                        return new HarmonyMethod(
                            (MethodInfo)patch.GetValue(p),
                            (int)priority.GetValue(p),
                            (string[])before.GetValue(p),
                            (string[])after.GetValue(p));
                    }


                    var processors = new List<PatchProcessor>();
                    patchedMethods.Do((m) =>
                    {
                        var patchInfo = GetPatchInfo.Invoke(null, new object[] { m });
                        if (patchInfo != null)
                        {
                            ((object[])prefixes.GetValue(patchInfo)).Do((patch) =>
                            {
                                processors
                                    .Add(GetPatcher((string)owner.GetValue(patch))
                                        .CreateProcessor(m)
                                        .AddPrefix(GetHarmonyMethod(patch)));
                            });

                            ((object[])postfixes.GetValue(patchInfo)).Do((patch) =>
                            {
                                processors
                                    .Add(GetPatcher((string)owner.GetValue(patch))
                                        .CreateProcessor(m)
                                        .AddPrefix(GetHarmonyMethod(patch)));
                            });
                            ((object[])transpilers.GetValue(patchInfo)).Do((patch) =>
                            {
                                processors
                                    .Add(GetPatcher((string)owner.GetValue(patch))
                                        .CreateProcessor(m)
                                        .AddPrefix(GetHarmonyMethod(patch)));
                            });
                        }
                    });

                    var unpatcher = Create.Invoke(null, new object[] { "HarmonyMod", });
                    patchedMethods.Do((m) => Unpatch.Invoke(unpatcher, new object[] { m, AllPatches, null, }));

                    /* Clear Shared State */
                    AppDomain.CurrentDomain.GetAssemblies().DoIf((a) => a.GetName().Name.Equals("HarmonySharedState"),
                        (a) =>
                        {
                            a.GetType("HarmonySharedState")?.GetField("state")?.SetValue(null, null);
                        });

                    processors.Do((p) => p.Patch());
#if TRACE
                    UnityEngine.Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] Transferred patches for {processors.Count} methods from {assembly.GetName().Version}");
#endif
                }
                catch (Exception e)
                {
                    Mod.SelfReport(SelfProblemType.CompatibilityPatchesFailed, e);
                }

            }
            done = true;
        }
    }

    [HarmonyPatch]
    // [HarmonyPriority(Priority.First)]
    public static class HarmonyInstance_GetPatchedMethods_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return Apply.targetAssembly.GetType("Harmony.HarmonyInstance").GetMethod("GetPatchedMethods");
        }

        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool PrefixFunction(string ___id, ref IEnumerable<MethodBase> __result)
        {
            __result = Harmony.GetAllPatchedMethods();
            return false;
        }

        [UsedImplicitly]
        static bool Prepare(MethodBase original) { return Apply.targetAssembly != null; }
    }



    [HarmonyPatch]
    [HarmonyPriority(Priority.First)]
    internal class HarmonyInstance_GetPatchInfo_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return Apply.targetAssembly.GetType("Harmony.HarmonyInstance").GetMethod("GetPatchInfo");
        }

        [UsedImplicitly]
        public static bool Prefix(object __instance, string ___id, MethodBase method, ref object __result)
        {
            Patches patches = Harmony.GetPatchInfo(method);
            if (patches == null)
            {
                __result = null;
            }
            else
            {
                var targetAssembly = __instance.GetType().Assembly;
                var targetPatchType = targetAssembly.GetType("Harmony.Patch");

                __result = Activator.CreateInstance(targetAssembly.GetType("Harmony.Patches"),
                    ConvertPatches(patches.Prefixes, targetPatchType),
                    ConvertPatches(patches.Postfixes, targetPatchType),
                    ConvertPatches(patches.Transpilers, targetPatchType)
                    );
            }
            return false;
        }

        static Array ConvertPatches(ReadOnlyCollection<Patch> from, Type to)
        {
            var result = Array.CreateInstance(to, from.Count);

            for (var i = 0; i < from.Count; ++i)
            {
                result.SetValue(Activator.CreateInstance(to, new object[]
                {
                    from[i].PatchMethod,
                    from[i].index,
                    from[i].owner,
                    from[i].priority,
                    from[i].before,
                    from[i].after,
                }), i);
            }
            return result;
        }

        [UsedImplicitly]
        static bool Prepare(MethodBase original) { return Apply.targetAssembly != null; }
    }


    [HarmonyPatch]
    [HarmonyPriority(Priority.First)]
    internal class HarmonyInstance_UnpatchAll_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return Apply.targetAssembly.GetType("Harmony.HarmonyInstance").GetMethod("UnpatchAll");
        }

        [UsedImplicitly]
        public static bool Prefix(string ___id, string harmonyID)
        {
            Patcher.CreateClientHarmony(___id).UnpatchAll(harmonyID);
            return false;
        }

        [UsedImplicitly]
        static bool Prepare(MethodBase original) { return Apply.targetAssembly != null; }
    }






    [HarmonyPatch]
    [HarmonyPriority(Priority.First)]
    internal class HarmonyInstance_VersionInfo_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return Apply.targetAssembly.GetType("Harmony.HarmonyInstance").GetMethod("VersionInfo");
        }

        [UsedImplicitly]
        public static bool Prefix(string ___id, out Version currentVersion, ref Dictionary<string, Version> __result)
        {
            __result = Harmony.VersionInfo(out currentVersion);
            return false;
        }

        [UsedImplicitly]
        static bool Prepare(MethodBase original) { return Apply.targetAssembly != null; }
    }



    [HarmonyPatch]
    [HarmonyPriority(Priority.First)]
    internal class PatchProcessor_Patch_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return Apply.targetAssembly.GetType("Harmony.PatchProcessor").GetMethod("Patch");
        }

        [UsedImplicitly]
        public static bool Prefix(
            object ___instance,
            List<MethodBase> ___originals,
            object ___prefix,
            object ___postfix,
            object ___transpiler,
            ref List<System.Reflection.Emit.DynamicMethod> __result)
        {
            HarmonyMethod prefix = ___prefix != null ? ConvertHarmonyMethod(___prefix) : null;
            HarmonyMethod postfix = ___postfix != null ? ConvertHarmonyMethod(___postfix) : null;
            HarmonyMethod transpiler = ___transpiler != null ? ConvertHarmonyMethod(___transpiler) : null;

            var harmony = Patcher.CreateClientHarmony(___instance);

            ___originals.ForEach((o) => {
                var patchProcessor = harmony.CreateProcessor(o.GetDeclaredMember());

                if (prefix != null) patchProcessor.AddPrefix(prefix);
                if (postfix != null) patchProcessor.AddPostfix(postfix);
                if (transpiler != null) patchProcessor.AddTranspiler(transpiler);

                patchProcessor.Patch();
            });

            __result = new List<System.Reflection.Emit.DynamicMethod>();
            return false;
        }

        static object GetFieldValue(string name, object from)
        {
            return from.GetType().GetField(name).GetValue(from);
        }

        static HarmonyMethod ConvertHarmonyMethod(object from)
        {
            return new HarmonyMethod
            {
                method = (MethodInfo)GetFieldValue("method", from),
                declaringType = (Type)GetFieldValue("originalType", from),
                methodName = (string)GetFieldValue("methodName", from),
                argumentTypes = (Type[])GetFieldValue("parameter", from),
                priority = (int)GetFieldValue("prioritiy", from), // typo is reproduced from 0Harmony v1 implementation
                before = (string[])GetFieldValue("before", from),
                after = (string[])GetFieldValue("after", from),
            };
        }

        [UsedImplicitly]
        static bool Prepare(MethodBase original) { return Apply.targetAssembly != null; }
    }



    [HarmonyPatch]
    [HarmonyPriority(Priority.First)]
    internal class PatchProcessor_UnPatch1_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return Apply.targetAssembly.GetType("Harmony.PatchProcessor").GetMethod("Unpatch", new Type[] { typeof(MethodInfo) });
        }

        [UsedImplicitly]
        public static bool Prefix(MethodInfo patch, object ___instance, List<MethodBase> ___originals)
        {
            var harmony = Patcher.CreateClientHarmony(___instance);
            ___originals.ForEach((o) => harmony.Unpatch(o, patch));
            return false;
        }

        [UsedImplicitly]
        static bool Prepare(MethodBase original) { return Apply.targetAssembly != null; }
    }



    [HarmonyPatch]
    [HarmonyPriority(Priority.First)]
    internal class PatchProcessor_UnPatch2_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return Apply.targetAssembly.GetType("Harmony.PatchProcessor").GetMethod("Unpatch", new Type[] { Apply.targetAssembly.GetType("Harmony.HarmonyPatchType"), typeof(string) });
        }

        [UsedImplicitly]
        public static bool Prefix(HarmonyPatchType type, string harmonyID, object ___instance, List<MethodBase> ___originals)
        {
            var harmony = Patcher.CreateClientHarmony(___instance);
            ___originals.ForEach((o) => harmony.Unpatch(o, type, harmonyID));
            return false;
        }

        [UsedImplicitly]
        static bool Prepare(MethodBase original) { return Apply.targetAssembly != null; }
    }
}
