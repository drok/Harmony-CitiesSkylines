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
using IAwareness;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.PlatformServices;
using ICities;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using static ColossalFramework.Plugins.PluginManager;

namespace HarmonyMod
{
    internal class ModReport : ModReportBase
    {
        PluginInfo plugin;
#if REPORT_AUTHORS
        readonly SavedString Author;
#else
        Version needsHarmony,
            needsHarmonyAPI,
            needsNewHarmony;
#endif

        HashSet<AssemblyName> m_missingAssemblies = new HashSet<AssemblyName>(new SameAssemblyName());

        bool m_usesHarmony;

        internal string Name { get; set; }
        internal string Description { get; set; }
        internal string  modType { get; set; }

        internal enum ProblemType
        {
            /* These should be listed in order of importance, highest to lowest,
             * because they are added in this order to the content manager tooltip,
             * but only up to 5 problems are shown per mod.
             */
            HelperNotLoadedFirst = 0,
            ModConflict,
            ExceptionThrown,
            ExceptionTriggered,
            AssemblyHackery,
            GenericProblem,
            Last
        }

        uint m_numProblems;
        uint m_numProblemsCaused;
        uint[] m_problemCount = new uint[(int)ProblemType.Last];
        Dictionary<string, uint> m_exceptionsThrown = new Dictionary<string, uint>();
        Dictionary<string, uint> m_exceptionsTriggered = new Dictionary<string, uint>();
        List<AssemblyName> m_modConflicts = new List<AssemblyName>();
        Dictionary<ProblemType, Dictionary<string, uint>> m_genericProblems = new Dictionary<ProblemType, Dictionary<string, uint>>();
        public bool isEnumerated { get; set; }

        internal ModReport(PluginInfo p, HashSet<AssemblyName> haveAssemblies, HarmonyModSupportException unsupportedLibs = null, bool enumerated = true)
        {
            plugin = p;
#if REPORT_AUTHORS
            var authorId = new SavedString(p.publishedFileID.AsUInt64.ToString(), Settings.userGameState, UserID.invalid.ToString(), true);
            Author = new SavedString(authorId.value, Settings.userGameState, string.Empty, true);
#endif

            isEnumerated = enumerated;

            SameAssemblyName sameName = new SameAssemblyName();

            if (p.assemblyCount != 0)
            {
                p.GetAssemblies()
                    .Do((assembly) =>
                    {
                        var refs = assembly.GetReferencedAssemblies();
                        foreach (var assemblyName in refs)
                        {
                            switch (assemblyName.Name)
                            {
                                case "0Harmony":
#if !REPORT_AUTHORS
                                    needsHarmony = assemblyName.Version;
#endif
                                    m_usesHarmony = true;
                                    break;
                                case "CitiesHarmony.API":
#if !REPORT_AUTHORS
                                    needsHarmonyAPI = assemblyName.Version;
#endif
                                    m_usesHarmony = true;
                                    break;
                                case "CitiesHarmony.Harmony":
#if !REPORT_AUTHORS
                                    needsNewHarmony = assemblyName.Version;
#endif
                                    m_usesHarmony = true;
                                    break;
                            }

                            if (!haveAssemblies.Contains(assemblyName) && !Report.myProvidedLibs.Contains(assemblyName.Name))
                            {
                                m_missingAssemblies.Add(assemblyName);
                            }

                            if (unsupportedLibs != null)
                            {
                                unsupportedLibs.unsupportedAssemblies.ForEach((u) =>
                                {
                                    if (sameName.Equals(u.assembly.GetName(), assemblyName))
                                    {
                                        ReportProblem(ModReport.ProblemType.ExceptionThrown, unsupportedLibs, u.Message);
                                    }
                                });
                            }
                        }
                    });
            }
        }

        /* Constructor and merge for importing report data from
         * another harmony instance
         */
        internal ModReport(PluginInfo p, ModReportBase report)
        {
            plugin = p;
            m_missingAssemblies = report.missingAssemblies;
            m_numProblems = report.numProblems;
            m_numProblemsCaused = report.numProblemsCaused;
            m_usesHarmony = report.usesHarmony;
        }
        internal void Merge(ModReportBase report)
        {
            m_numProblems += report.numProblems;
            m_numProblemsCaused += report.numProblemsCaused;
        }

        internal void CacheModInfo()
        {
            IUserMod modInst = null;
            try
            {
                modInst = plugin.userModInstance as IUserMod;
            }
#pragma warning disable CS0168 // The variable 'ex' is declared but never used
            catch (Exception ex)
#pragma warning restore CS0168 // The variable 'ex' is declared but never used
            {
            }

            if (plugin.assemblyCount == 0 || modInst == null)
            {
                var modDir = new System.IO.DirectoryInfo(plugin.modPath);
                Name = modDir.Name;
                modType = plugin.name;
                Description = plugin.name;
            }
            else
            {
                modType = modInst.GetType().Namespace;
                Name = modInst.Name;
                Description = modInst.Description;
            }
        }

        internal void ReportUnsupportedHarmony(HarmonyModSupportException e)
        {
            SameAssemblyName sameName = new SameAssemblyName();

            Array.ForEach(plugin.userModInstance.GetType().Assembly.GetReferencedAssemblies(),
                (refa) =>
                {
                    e.unsupportedAssemblies.ForEach((u) =>
                    {
                        if (sameName.Equals(u.assembly.GetName(), refa))
                        {
                            ReportProblem(ModReport.ProblemType.ExceptionThrown, e, u.Message);
                        }
                    });
                });
        }

        internal string Summary(bool brief, Report.ReportFormat reportFormat)
        {
            bool isLocal = plugin.publishedFileID == PublishedFileId.invalid;

            string strEnabled;
            switch (reportFormat)
            {
                case Report.ReportFormat.Gist:
                    strEnabled = plugin.isEnabled ? "x |" : " |";
                    break;
                default:
                    strEnabled = plugin.isEnabled ? "*" : " ";
                    break;
            }

            string location;
            if (plugin.isBuiltin)
            {
                location = "(built-in)";
            }
            else if (isLocal)
            {
                var modDir = new System.IO.DirectoryInfo(plugin.modPath);
                switch (reportFormat)
                {
                    case Report.ReportFormat.Gist:
                        location = $"`{modDir.Name}`";
                        break;
                    default:
                        location = $"'{modDir.Name}'";
                        break;
                }
            } else
            {
                if (brief)
                {
                    location = plugin.publishedFileID.AsUInt64.ToString();
                } else
                {
                    var url = "https://steamcommunity.com/sharedfiles/filedetails/?id=" + plugin.publishedFileID.AsUInt64;
                    switch (reportFormat)
                    {
                        case Report.ReportFormat.Gist:
                            location = $"[`{plugin.publishedFileID.AsUInt64.ToString()}`]({url})";
                            break;
                        case Report.ReportFormat.SteamForum:
                            location = $"[url={url}]{plugin.publishedFileID.AsUInt64.ToString()}[/url]";
                            break;

                        default:
                            location = plugin.publishedFileID.AsUInt64.ToString();
                            break;
                    }
                }
            }

#if !REPORT_AUTHORS
            string harmonyVer = needsHarmony != null ? $"0H: {needsHarmony.ToString()}" :
                                needsNewHarmony != null ? $"CH: {needsNewHarmony.ToString()}" : string.Empty;
            string harmonyAPI = needsHarmonyAPI != null ? needsHarmonyAPI.ToString() : string.Empty;
#endif

            string summaryLines;
            string problems;
            string missingAssembliesStr = string.Empty;

            if (missingAssemblies.Count > 0)
            {
                missingAssembliesStr += ListMissingAssemblies(reportFormat);
            }

            switch (reportFormat)
            {
                case Report.ReportFormat.Gist:
                    problems = ProblemSummary(reportFormat, null, brief ? Report.MAX_PROBLEMS_PER_TYPE_IN_DISPLAY : Report.MAX_PROBLEMS_PER_TYPE_IN_LOG);
                    summaryLines = $"{strEnabled} {location} | `{modType.Max(31)}` |";
                    if (!string.IsNullOrEmpty(problems) || missingAssemblies.Count > 0)
                    {
                        summaryLines += " <ul>" + problems + missingAssembliesStr + "</ul>";
                    }
                    summaryLines += $" | {Name.Max(27)} | ";
#if REPORT_AUTHORS
                    if (!string.IsNullOrEmpty(Author.value) && Author.exists)
                        summaryLines += $" | `{Author.value}`";
#else
                    if (!string.IsNullOrEmpty(harmonyVer))
                        summaryLines += $"`{harmonyVer}`";
                    summaryLines += " | ";
                    if (!string.IsNullOrEmpty(harmonyAPI))
                        summaryLines += $"`{harmonyAPI}`";
#endif
                    summaryLines += "\n";
                    
                    break;
                default:
                    problems = ProblemSummary(reportFormat, "                   [ERR] ", brief ? Report.MAX_PROBLEMS_PER_TYPE_IN_DISPLAY : Report.MAX_PROBLEMS_PER_TYPE_IN_LOG);
                    summaryLines = $"{strEnabled} {location.Max(19),-20} {modType.Max(25),-26} {Name.Max(27),-28}";
#if REPORT_AUTHORS
                    summaryLines += $" {Author.value,-14}\n";
#else
                    summaryLines += $" {harmonyVer,-14} {harmonyAPI,-10}\n";
#endif
                    summaryLines += problems + missingAssembliesStr;
                    break;
            }

            return summaryLines;
        }

        internal string ProblemSummary(Report.ReportFormat reportFormat, string prefix, uint maxLines)
        {
            string summary = string.Empty;
            uint found = 0;
            for (uint i = 0; i < (uint)ProblemType.Last && found < maxLines; ++i)
            {
                if (m_problemCount[i] > 0)
                {
                    var problemHeadline = ProblemDescription((ProblemType)i, m_problemCount[i]);
                    switch (reportFormat)
                    {
                        case Report.ReportFormat.Gist:

                            summary += "<li>" + problemHeadline + "</li><ul>" +
                                IncidentList((ProblemType)i, "<li>", "</li>", maxLines, ref found) +
                                "</ul>";
                            break;
                        default:
                            summary += prefix + problemHeadline + "\n";
                            summary += IncidentList((ProblemType)i, prefix, "\n", maxLines, ref found);
                            break;
                    }
                }
            }

            return summary;
        }

        string IncidentList(ProblemType i, string prefix, string postfix, uint maxLines, ref uint found)
        {
            string summary = string.Empty;
            switch ((ProblemType)i)
            {
                case ProblemType.ExceptionThrown:
                    foreach (var e in m_exceptionsThrown)
                    {
                        if (found == maxLines)
                        {
                            summary += prefix + "  ..." + postfix;
                            continue;
                        }

                        summary += prefix + "  " + e.Key + $": {e.Value} time{(e.Value > 1 ? "s" : string.Empty)}" + postfix;
                        found++;
                    }
                    break;
                case ProblemType.ExceptionTriggered:
                    foreach (var e in m_exceptionsTriggered)
                    {
                        if (found == maxLines)
                        {
                            summary += prefix + "  ..." + postfix;
                            continue;
                        }

                        summary += prefix + "  " + e.Key + $": {e.Value} time{(e.Value > 1 ? "s" : string.Empty)}" + postfix;
                        found++;
                    }
                    break;
                case ProblemType.AssemblyHackery:
                case ProblemType.GenericProblem:
                    if (m_genericProblems.TryGetValue((ProblemType)i, out var problems))
                    {
                        foreach (var e in problems)
                        {
                            if (found == maxLines)
                            {
                                summary += prefix + "  ..." + postfix;
                                continue;
                            }


                            summary += prefix + "  " + e.Key;
                            if (e.Value > 1)
                            {
                                summary += $": {e.Value} times";
                            }
                            summary += postfix;
                            found++;
                        }
                    }
                    break;
                case ProblemType.ModConflict:
                    foreach (var assembly in m_modConflicts)
                    {
                        if (found == maxLines)
                        {
                            summary += prefix + "  ..." + postfix;
                            continue;
                        }
                        {
                            string conflictingPlugin;
                            try
                            {
                                var p = Singleton<PluginManager>.instance.GetPluginsInfo().First((x) => x.ContainsAssembly(assembly));
                                conflictingPlugin = $"{p.name}, {(p.publishedFileID == PublishedFileId.invalid ? "local" : "workshop")}";

                            }
                            catch (InvalidOperationException)
                            {
                                conflictingPlugin = "unknown location";
                            }

                            summary += prefix + "  " + assembly + " from " + conflictingPlugin + postfix;
                        }
                        found++;
                    }
                    break;
            }
            return summary;
        }
        private string ProblemDescription (ProblemType p, uint n)
        {
            switch (p)
            {
                case ProblemType.ExceptionThrown: return $"Exceptions thrown: {n}";
                case ProblemType.ExceptionTriggered: return $"Exceptions triggered: {n} incidents from {m_exceptionsTriggered.Count} locations";
                case ProblemType.GenericProblem: return $"Other problems: {n}";
                case ProblemType.AssemblyHackery: return $"Exploits: {n}";
                case ProblemType.ModConflict: return $"Mod Conflicts: {n}";
#if DEVELOPER
                case ProblemType.HelperNotLoadedFirst: return "Helper is not loaded first";
#endif
            }
            return p.ToString();
        }


        internal string ListMissingAssemblies(Report.ReportFormat reportFormat)
        {
            string str = string.Empty;

            foreach (var m in m_missingAssemblies)
            {
                switch (reportFormat)
                {
                    case Report.ReportFormat.Gist:
                        str += $"<li>*missing*: `{m.Name}[{m.Version}]`</li>";
                        break;
                    default:
                        str += $"                   [ERR] missing: {m.Name}[{m.Version}]\n";
                        break;
                }
            }
            return str;
        }

        internal uint ReportProblem(ProblemType problem, Exception e, AssemblyName assemblyName)
        {

            switch (problem)
            {
                case ProblemType.AssemblyHackery:
                    m_missingAssemblies.Remove(assemblyName);
                    break;
            }
            return ReportProblem(problem, e);
        }

        internal uint ReportProblem(ProblemType problem, Exception e, string detail = null)
        {
            string circumstance = detail ?? Report.ExMessage(e, false);

            if (!string.IsNullOrEmpty(e.HelpLink))
            {
                circumstance += " (see " + e.HelpLink + ")";
            }

            /* e may be null */
            if (problem == ProblemType.ExceptionThrown)
            {
                if (m_exceptionsThrown.ContainsKey(circumstance))
                {
                    m_exceptionsThrown[circumstance]++;
                }
                else
                {
                    m_exceptionsThrown[circumstance] = 1;
                }
            } else if (problem == ProblemType.ExceptionTriggered)
            {
                if (m_exceptionsTriggered.ContainsKey(circumstance))
                {
                    m_exceptionsTriggered[circumstance]++;
                }
                else
                {
                    m_exceptionsTriggered[circumstance] = 1;
                }
            }
            return ReportProblem(problem, circumstance);
        }

        internal uint ReportProblem(ProblemType problem, string detail = null)
        {

            switch (problem)
            {
                case ProblemType.ExceptionTriggered:
                    m_numProblemsCaused++;
                    break;
                default:
                    m_numProblems++;
                    break;
            } 

            m_problemCount[(int)problem]++;
            switch (problem)
            {
                case ProblemType.AssemblyHackery:
                case ProblemType.GenericProblem:
                    if (!m_genericProblems.TryGetValue(problem, out var problems))
                    {
                        problems = new Dictionary<string, uint>();
                        m_genericProblems[problem] = problems;
                    }
                    if (problems.ContainsKey(detail))
                    {
                        problems[detail]++;
                    } else
                    {
                        problems[detail] = 1;
                    }
                    break;
            }
            return m_numProblems;
        }
        internal uint ReportProblem(ProblemType problem, Assembly assembly)
        {
            switch (problem)
            {
                case ProblemType.ExceptionTriggered:
                    m_numProblemsCaused++;
                    m_problemCount[(int)problem]++;
                    break;
                default:
                    m_numProblems++;
                    m_problemCount[(int)problem]++;
                    break;
            }
            switch (problem)
            {
                case ProblemType.ModConflict:
                    m_modConflicts.Add(assembly.GetName());
                    break;
            }
            return m_numProblems;
        }

        public override HashSet<AssemblyName> missingAssemblies { get { return m_missingAssemblies; } }
        public override uint numProblems { get { return m_numProblems + (uint)m_missingAssemblies.Count; } }
        public override uint numProblemsCaused { get { return m_numProblemsCaused; } }
        public override bool usesHarmony { get { return m_usesHarmony; } }
    }


    public static class Extensions
    {
        public static string Max(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 1) + ' ';
        }
    }
}
