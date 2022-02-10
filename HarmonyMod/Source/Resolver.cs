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
extern alias HarmonyCHH2040;

using System;
using System.Reflection;
using static UnityEngine.Debug;
using ColossalFramework;

namespace HarmonyMod
{
    class Resolver
    {
        static SameAssemblyName maybeUpdate = new SameAssemblyName(
            compareName: true,
            compareVersion: SameAssemblyName.VersionComparison.MajorMinor,
            compareCulture: true,
            compareToken: false);

        static SameAssemblyName maybeReplacement = new SameAssemblyName(
            compareName: false,
            compareVersion: SameAssemblyName.VersionComparison.MajorMinor,
            compareCulture: true,
            compareToken: false);

        static Assembly ARResolve(object sender, ResolveEventArgs args)
        {
            Assembly resolved = default(Assembly);
            Assembly found_by_exploit = default(Assembly);

            var assname = new AssemblyName(args.Name);
            AssemblyName sourceAssembly = default(AssemblyName);

            switch (assname.Name)
            {
                case "0Harmony":
                    if (maybeUpdate.Equals(assname, typeof(Harmony2::HarmonyLib.Harmony).Assembly.GetName()))
                        resolved = typeof(Harmony2::HarmonyLib.Harmony).Assembly;
                    break;
                case "CitiesHarmony.Harmony":
                    if (maybeReplacement.Equals(assname, typeof(Harmony2::HarmonyLib.Harmony).Assembly.GetName()))
                        resolved = typeof(Harmony2::HarmonyLib.Harmony).Assembly;
                    break;
                default:
                    found_by_exploit = Behave_Like_ColossalOrder_Assembly_Resolver_Vulnerability_YUK_YUK_YUK_WHY_DO_I_FEEL_DIRTY_NOW(assname, out sourceAssembly);
                    if (found_by_exploit != null && !Versioning.IsObsolete(Versioning.Obsolescence.PROHIBIT_RESOLVER_EXPLOIT_AFTER, "Permit exploiting game's resolver vulnerability"))
                    {
                        resolved = found_by_exploit;
                    }
                    break;
            }

            string source = string.Empty;
            if (sourceAssembly != null)
            {
                source = $" (requested by {sourceAssembly.Name}[{sourceAssembly.Version}]{(found_by_exploit != null ? ", exploit!" : "")})";
            }
            else
            {
                var origin = new System.Diagnostics.StackTrace(2).GetFrame(0).GetMethod().DeclaringType.Assembly;
                if (origin != null)
                {
                    source = $" (requested by {origin.GetName().Name}[{origin.GetName().Version}]{(found_by_exploit != null ? ", exploit!" : "")})";
                }
            }

            var warn = resolved == null || found_by_exploit != null;
            if (resolved == null || found_by_exploit != null || CODebugBase<LogChannel>.verbose)
            {
                var info = $"[{Versioning.FULL_PACKAGE_NAME}] {(warn ? "WARNING" : "INFO")}:- Assembly Resolve{source}:\n  Requested:   {args.Name}\n" +
                    $"  Resolved as: {(resolved != null ? resolved.GetName() : "not found")}\n" +
                    ((warn && CODebugBase<LogChannel>.verbose) ? $"From:\n{new System.Diagnostics.StackTrace(true)}\nPresent Assemblies:\n{AssemblyList()}" : string.Empty);

                if (warn)
                    LogWarning(info);
                else
                    Log(info);
            }

            return resolved;
        }

        static Assembly ATResolveOnlyLog(object sender, ResolveEventArgs args)
        {
            Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Type Resolve: {args.Name} -> not found");
            return null;
        }

        static Assembly Behave_Like_ColossalOrder_Assembly_Resolver_Vulnerability_YUK_YUK_YUK_WHY_DO_I_FEEL_DIRTY_NOW(AssemblyName assname, out AssemblyName sourceAssembly)
        {
            foreach(var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assname.Name.StartsWith(a.GetName().Name))
                {
                    var e = new Exception($"Exploits resolver vulnerability to find {assname.Name}[{assname.Version}]");
                    e.HelpLink = "https://github.com/drok/Harmony-CitiesSkylines/issues/18";

                    if (Mod.mainModInstance != null)
                        Mod.mainModInstance.report.ReportPlugin(ModReport.ProblemType.AssemblyHackery, e, 2, out sourceAssembly, assname);
                    else
                    {
                        sourceAssembly = null;
                    }

                    return a;
                }
            }
            sourceAssembly = null;
            return null;
        }
        static string AssemblyList()
        {
            string names = string.Empty;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                names += assembly.GetName() + "\n";
            }
            return names;
        }

        public static void Install()
        {


            ResolveEventHandler csBuggedResolver = (ResolveEventHandler)Delegate.CreateDelegate(typeof(ResolveEventHandler),
                typeof(BuildConfig).GetMethod("CurrentDomain_AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Static));

            AppDomain.CurrentDomain.AssemblyResolve -= csBuggedResolver;
            AppDomain.CurrentDomain.TypeResolve -= csBuggedResolver;

            AppDomain.CurrentDomain.AssemblyResolve += ARResolve;
            if (CODebugBase<LogChannel>.verbose)
                AppDomain.CurrentDomain.TypeResolve += ATResolveOnlyLog;
        }

    }
}
