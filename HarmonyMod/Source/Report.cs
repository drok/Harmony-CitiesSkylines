﻿/*
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
using IAwareness;
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Plugins;
using ColossalFramework.DataBinding;
#if REPORT_AUTHORS
using ColossalFramework.PlatformServices;
using static ColossalFramework.PlatformServices.PlatformService;
#endif
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using static UnityEngine.Debug;
using static UnityEngine.Assertions.Assert;
using Harmony2::HarmonyLib;

using static ColossalFramework.Plugins.PluginManager;

/*
 * FIXME: Assertion Exceptions thrown by mods in their LoadingExtension.OnCreate() are lost,
 * even though HarmonyMod is loaded. Maybe a game bug, needs investigation
 * 
 * Non-assertions do work.
 * 
 */
namespace HarmonyMod
{
    using static TextureResources;

    public class Report
    {
        internal const string REPORT_TITLE = Versioning.PACKAGE_NAME + " Report";
        internal const uint MAX_PROBLEMS_IN_TOOLTIP = 20;
        internal const uint MAX_EXCEPTION_PROMPTS_PER_MOD = 3;
        internal const uint MAX_PROBLEMS_PER_TYPE_IN_DISPLAY = 5;
        internal const uint MAX_PROBLEMS_PER_TYPE_IN_LOG = 50;

        public enum ReportFormat
        {
            Gist = 0,
            SteamForum,
            PlainText
        }


        private readonly Color32 COLOR_NORMAL = new Color32(255, 255, 255, 200);
        private readonly Color32 COLOR_WARNING = new Color32(238, 168, 100, 255);
        private readonly Color32 COLOR_ERROR = new Color32(249, 112, 98, 255);
        private readonly Color32 COLOR_SELF_FRESH_GOOD = new Color32(44, 235, 71, 255);
        private readonly Color32 COLOR_SELF_GOOD = new Color32(202, 235, 207, 255);
        private readonly Color32 COLOR_USES_HARMONY_GOOD = new Color32(202, 235, 207, 255);

        string report = string.Empty;

        readonly Dictionary<PluginInfo, ModReportBase> m_modReports;
        readonly Dictionary<string, PluginInfo> m_removedMods;
        readonly Dictionary<string, List<Assembly>> m_pathToAssemblyMap;
        readonly Dictionary<string, PluginInfo> m_Plugins;

        readonly HashSet<AssemblyName> haveAssemblies;
        readonly HashSet<AssemblyName> missingAssemblies;
        HarmonyModSupportException unsupportedLibs = null;

        readonly List<string> activities;

        static internal readonly HashSet<string> myProvidedLibs = new HashSet<string>
        {
            "0Harmony",
            "CitiesHarmony.Harmony",
            // "CitiesHarmony.API",
        };
        private static readonly HashSet<string> manifest = new HashSet<string>
        {
            "0Harmony, Version=2.0.400.0",
            "CitiesHarmony.Harmony, Version=2.0.400.0",
            "IAmAware, Version=0.0.1.0",
            "CitiesHarmony, Version=0.0.0.0",
            // "Mono.Cecil, Version=0.10.4.0",
            // "Mono.Cecil.Mdb, Version=0.10.4.0",
            // "Mono.Cecil.Pdb, Version=0.10.4.0",
            // "Mono.Cecil.Rocks, Version=0.10.4.0",
            // "MonoMod.Common, Version=22.1.16.0",
        };

        string m_optionsButtonOriginalText = null;
        float reportPanel_orig_height = 0;
        float reportPanel_orig_width = 0;

#if IMPLEMENTED_CUSTOM_ICON_ON_REPORT_PANEL
        readonly Texture2D Logo;
#endif
        ExceptionPanel reportPanel = null;
        UILabel reportText = null;
        UISprite summarySprite = null;

        int reportNumber;
        internal readonly bool[] selfDiag_problems;

       public Report()
       {
            selfDiag_problems = new bool[(int)SelfProblemType.Last];

            m_modReports = new Dictionary<PluginInfo, ModReportBase>();
            m_removedMods = new Dictionary<string, PluginInfo>();
            m_pathToAssemblyMap = new Dictionary<string, List<Assembly>>();

            haveAssemblies = new HashSet<AssemblyName>(new SameAssemblyName());
            missingAssemblies = new HashSet<AssemblyName>(new SameAssemblyName());
            activities = new List<string>();

            m_Plugins = (Dictionary<string, PluginInfo>)typeof(PluginManager)
                .GetField("m_Plugins", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                .GetValue(Singleton<PluginManager>.instance);

            /* Looping twice because on the 2nd loop when the modReport is constructed for self,
             * the list of haveAssemblies must already exist;
             */
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.GetName();
                haveAssemblies.Add(name);
            }

#if IMPLEMENTED_CUSTOM_ICON_ON_REPORT_PANEL
            Logo = LoadDllResource("HarmonyLogo.png", 800, 800);
#endif
#if REPORT_AUTHORS
            workshop.eventUGCRequestUGCDetailsCompleted += AuthorNameCache;
#endif
        }

#if REPORT_AUTHORS
        private void AuthorNameCache(UGCDetails details, bool ioError)
        {
            var creator = details.creatorID.ToString();
            new SavedString(details.publishedFileId.ToString(), Settings.userGameState, string.Empty, true).value = creator;
            new SavedString(creator, Settings.userGameState, string.Empty, true).value = new Friend(details.creatorID).personaName;
        }
#endif
        private void Decorate(bool forDisplay, ReportFormat reportFormat)
        {
            string headline = string.Empty;
            if (forDisplay && report.Length > 25000)
            {
                headline = "(Built-in Mods are not shown; truncated to fit window - see log for full report)";
                report.Remove(25000);
            }
            else
            {
                if (forDisplay)
                {
                    headline = "(Built-in Mods are not shown; see log for additional detail)";
                }
                else
                {
                }
            }
            switch (reportFormat)
            {
                case ReportFormat.Gist:
                    report =
                        $"###{headline}\n" +
#if REPORT_AUTHORS
                        "No. | e | Location | Class Type | Issues | Name | Author\n" +
#else
                        "No. | e | Location | Class Type | Issues | Name | Lib.Harmony | API\n" +
#endif
                        "----|---|----------|------------|--------|------|-------------|-----\n" +
                        report;
                    break;
                default:
                    report =
#if REPORT_AUTHORS
                         "No.|e| Location           | Class Type              | Name                         | Author\n" +
                         "---+-+--------------------+-------------------------+------------------------------+-------------\n" +
#else
                         "No.|e| Location           | Class Type              | Name                         | Lib.Harmony | API ---\n" +
                         "---+-+--------------------+-------------------------+------------------------------+-------------+--------\n" +
#endif
                         $"                                      {headline}\n" +
                        report;
                    break;
            }
        }

        public void Output(bool final, ReportFormat reportFormat, string subTitle = null)
        {
            Decorate(false, reportFormat);
            ++reportNumber;
            string title = final ? $"Final {REPORT_TITLE}" : $"Interim {REPORT_TITLE} #{reportNumber}";

            if (subTitle != null)
            {
                title += " (" + subTitle + ")";
            }

            switch (reportFormat)
            {
                case ReportFormat.Gist:
                    Log($"[{Versioning.FULL_PACKAGE_NAME}] === {title} === paste as FinalReport.md at https://gist.github.com/\n## {title}\n" +
                        report +
                        "\n\n=========================================================================================================================");
                    break;
                default:
                    Log($"[{Versioning.FULL_PACKAGE_NAME}]  ================================= {title} ==================================\n" +
                        report +
                        "=================================================================================================");
                    break;
            }
        }

        string PrepareActivityReport(ReportFormat reportFormat)
        {
            string report = string.Empty;
            if (activities.Count != 0)
            {

                switch (reportFormat)
                {
                    case ReportFormat.Gist:
                        report += "## Chronology\n";
                        activities.Do((x) => report += $" * {x}\n");
                        break;
                    default:
                        report += $"\n          ========================= Chronolology ======================\n\n";
                        activities.Do((x) => report += $" * {x}\n");
                        report += "\n";
                        break;
                }
            }
            return report;
        }

        private uint PrepareReport(bool showOnlyProblems, ReportFormat reportFormat)
        {

            int i = 1;

            uint totalProblems = 0;
            int problematicMods = 0;
            missingAssemblies.Clear();

            foreach (PluginInfo mod in Singleton<PluginManager>.instance.GetPluginsInfo())
            {
                if (mod.isBuiltin)
                {
                    continue;
                }
                var modReport = GetReport(mod);
                if (!showOnlyProblems || modReport.numProblems != 0)
                {
                    totalProblems += modReport.numProblems;
                    switch (reportFormat)
                    {
                        case ReportFormat.Gist:
                            report += $"{i,3} | {modReport.Summary(showOnlyProblems, reportFormat)}";
                            break;
                        default:
                            report += $"{i,3} {modReport.Summary(showOnlyProblems, reportFormat)}";
                            break;
                    }

                    if (modReport.missingAssemblies.Count != 0)
                    {
                        missingAssemblies.UnionWith(modReport.missingAssemblies);
                    }
                    problematicMods++;
                }
                ++i;
            }

            if (!showOnlyProblems)
            {
                foreach (var mod in m_removedMods.Values)
                {
                    var modReport = GetReport(mod);
                    switch (reportFormat)
                    {
                        case ReportFormat.Gist:
                            report += $"{"rem",3} | {modReport.Summary(showOnlyProblems, reportFormat)}";
                            break;
                        default:
                            report += $"{"rem",3} {modReport.Summary(showOnlyProblems, reportFormat)}";
                            break;
                    }

                    if (modReport.missingAssemblies.Count != 0)
                    {
                        missingAssemblies.UnionWith(modReport.missingAssemblies);
                    }

                }
                if (missingAssemblies.Count != 0)
                {
                    switch (reportFormat)
                    {
                        case ReportFormat.Gist:
                            report += $"\n## Missing Dependencies\n";
                            foreach (var m in missingAssemblies)
                            {
                                report += $" * `{m}`\n";
                            }
                            break;
                        default:
                            report += $"\n          ========================= Missing Dependencies  ======================\n\n";
                            foreach (var m in missingAssemblies)
                            {
                                report += $"   {m}\n";
                            }
                            break;
                    }

                }
            }
            else
            {
                if (i > 1 && problematicMods < (i - 1))
                {
                    report += $"         ---  Not shown: {i - 1 - problematicMods} other mods with no obvious issues ---\n";
                }
            }

            report += "\n";

            return totalProblems;
        }

        internal ModReport GetReport(PluginInfo plugin, bool needNew = false)
        {
            if (needNew || !m_modReports.TryGetValue(plugin, out ModReportBase modReport))
            {
                modReport = new ModReport(plugin, haveAssemblies, unsupportedLibs);
                m_modReports[plugin] = modReport;

                if (plugin.isEnabled)
                {
                    m_pathToAssemblyMap[plugin.modPath] = plugin.GetAssemblies();
                }
            } else if (!(modReport as ModReport).isEnumerated)
            {
                if (plugin.isEnabled)
                {
                    (modReport as ModReport).isEnumerated = true;
                    m_pathToAssemblyMap[plugin.modPath] = plugin.GetAssemblies();
                }
            }
            return modReport as ModReport;
        }

        /* When the report is made due to an exception, a new ModReport is created
         * before the mod was seen.
         * these reports need to be marked as "first seen"
         */
        internal uint GetReport(PluginInfo plugin, ModReport.ProblemType problem, Exception e, string detail = null)
        {
            if (!m_modReports.TryGetValue(plugin, out ModReportBase modReport))
            {
                modReport = new ModReport(plugin, haveAssemblies, unsupportedLibs, false);
                m_modReports[plugin] = modReport;
            }

            return (modReport as ModReport).ReportProblem(problem, e, detail);
        }

        internal void OutputReport(Mod self, bool final, string stepName = null)
        {
            /* also update the mod list, this is called when the plugin list changes */
            if (final)
            {

                PrepareReport(false, ReportFormat.Gist);
                report += PrepareActivityReport(ReportFormat.Gist);
                Output(final, ReportFormat.Gist, stepName);
                report = string.Empty;
            }

            PrepareReport(false, ReportFormat.PlainText);
            if (final)
            {
                report += PrepareActivityReport(ReportFormat.PlainText);
            }
            SelfDiagnostics(self);
            Output(final, ReportFormat.PlainText, stepName);
            report = string.Empty;
        }

        internal void OnModListChanged(bool inGame)
        {

            Singleton<PluginManager>.instance.GetPluginsInfo()
                .Where((p) => !p.isBuiltin)
                .Do((p) => {
                    /* FIXME: Handle self-enabling mods, which enable themselves after this call.
                    * so this call is made with isEnabled=false, but later if the mod is removed
                    * the call is made with isEnabled=true
                    * Need a path->assemblyName lookup table.
                    */
                    ModReport modReport;
                    if (m_modReports.TryGetValue(p, out ModReportBase reportBase)) {
                        modReport = reportBase as ModReport;
                        if (modReport.isEnumerated) return;
                        modReport.isEnumerated = true;
                    }
                    else {
                        modReport = GetReport(p, true);
                    }


                    string key = p.modPath;
                    try
                    {
                        if (p.isEnabled)
                            key = p.GetAssemblies()[0].FullName; //userModInstance.GetType().Assembly.FullName;
                    }
#pragma warning disable CS0168 // The variable 'ex' is declared but never used
                    catch (Exception ex)
#pragma warning restore CS0168 // The variable 'ex' is declared but never used
                    {
                    }

                    if (m_removedMods.TryGetValue(key, out PluginInfo oldMod)/* || p.isEnabled*/)
                    {
                        /* Ie, for mods that are added but are broken and do not have a loadable assembly */
                        key = oldMod.modPath;
                    }
                    if (m_removedMods.TryGetValue(key, out oldMod))
                    {
                        modReport.Merge(GetReport(oldMod));
                            m_removedMods.Remove(key);
                            m_modReports.Remove(oldMod);
                        }
                    /* FIXME: If they throw from their OnEnabled(), they
                        * are added to m_modReports due to exception handling before getting here.
                        */
                    var meta = Singleton<SimulationManager>.instance.m_metaData;
                    var paused = Singleton<SimulationManager>.instance.SimulationPaused;
                    ReportActivity($"Mod '{p.name}'" + 
                        (inGame ? " was added at " + meta.m_currentDateTime + (paused ? ", paused" : ", **running**")
                            : string.Empty));
                });

            m_modReports.Keys.Except(Singleton<PluginManager>.instance.GetPluginsInfo()).
                Do((p) =>
                {
                    string key = p.modPath;
                    if (!m_pathToAssemblyMap.TryGetValue(p.modPath, out var assemblies))
                    {
                        /* Ie, for mods that are removed but were never enabled; they are known by path */
                        assemblies.Exists((a) => { key = a.FullName; return true; });
                    }
                    if (!m_removedMods.TryGetValue(key, out var removedMod))
                    {
                        m_removedMods[key] = p;
                        var meta = Singleton<SimulationManager>.instance.m_metaData;
                        var paused = Singleton<SimulationManager>.instance.SimulationPaused;
                        ReportActivity($"Mod '{p.name}'" +
                            (inGame ? " was removed at " + meta.m_currentDateTime + (paused ? ", paused" : ", **running**")
                                : string.Empty));
                    }
                });
        }
        internal void OnModsLoaded()
        {
            Singleton<PluginManager>.instance.GetPluginsInfo()
                .Where((p) => !p.isBuiltin)
                .Do((p) =>
                {
                    GetReport(p).CacheModInfo();
                });
        }

            void UpdateReport()
        {
            if (reportPanel != null && reportPanel.component.isVisible)
            {
                report = string.Empty;
                var problems = PrepareReport(true, ReportFormat.PlainText);
                Decorate(true, ReportFormat.PlainText);

                reportText.text = "[code][noparse]\n" + report + "[/noparse][/code]";
                summarySprite.spriteName = ProblemSummarySprite(problems);
                report = string.Empty;

            }
        }
        internal void ShowReport(UIComponent c, UIMouseEventParameter p)
        {
            try
            {
                if (reportPanel == null)
                {
                    reportPanel = UIView.library.Get<ExceptionPanel>("ExceptionPanel");
                    reportText = reportPanel.Find<UIScrollablePanel>("Scrollable Panel").Find<UILabel>("Message");
                    summarySprite = reportPanel.Find<UISprite>("Sprite");
                }
                UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel", delegate (UIComponent cp, int r)
                {
                    if (r > 0)
                    {
                    }
                    else
                    {
                    }
                });

                if (reportPanel != null)
                {
                    var binding = reportPanel.GetComponent<BindPropertyByKey>();
                    binding.SetProperties(TooltipHelper.Format("title", REPORT_TITLE, "img", "IconWarning"));


                    if (reportPanel_orig_width == 0)
                    {

                        reportPanel_orig_width = reportPanel.component.width;
                        reportPanel_orig_height = reportPanel.component.height;

                        reportPanel.component.width = 1000;
                        reportPanel.component.height = 600;

                        var widthIncrease = reportPanel.component.width - reportPanel_orig_width;
                        var heightIncrease = reportPanel.component.height - reportPanel_orig_height;


                        var textArea = reportPanel.Find<UIScrollablePanel>("Scrollable Panel");
                        textArea.width += widthIncrease;
                        textArea.height += heightIncrease;


                        var sb = reportPanel.Find<UIScrollbar>("Scrollbar");
                        var sbpos = sb.position;
                        sb.position = new Vector3(sbpos.x + widthIncrease, sbpos.y, sbpos.z);
                        sb.height += reportPanel.component.height - reportPanel_orig_height;

                        reportText.maximumSize += new Vector2(widthIncrease, 0);
                        reportText.width += widthIncrease;

                        var okButton = reportPanel.Find<UIButton>("Ok");
                        if (okButton != null)
                        {
                            var cpos = okButton.position;
                            okButton.position = new Vector3(cpos.x + widthIncrease / 4, cpos.y, cpos.z);
                        }


                        var cancelButton = reportPanel.Find<UIButton>("Copy");
                        if (cancelButton != null)
                        {
                            var cpos = cancelButton.position;
                            cancelButton.position = new Vector3(cpos.x + widthIncrease / 4, cpos.y, cpos.z);
                        }
                    }
                    UpdateReport();
                }
            }
            catch (Exception ex)
            {
                Mod.SelfReport(SelfProblemType.FailedToReport, ex);
            }
        }

        internal void OnScene(Scene scene, LoadSceneMode loadMode)
        {
            if (scene.name == "MainMenu")
            {
                OnModsLoaded();
            }
        }

        internal void OnEnabled()
        {
            CheckConflicts();

            Singleton<PluginManager>.instance.eventPluginsChanged += UpdateReport;
            SceneManager.sceneLoaded += OnScene;

            try
            {
                /* If called at application start, UIView.library is null, but
                 * if called live, mid game, it is set
                 */
                if (UIView.library)
                {
                    ContentManagerPanel contentManagerPanel = UIView.library.Get<ContentManagerPanel>("ContentManagerPanel");

                    reportPanel = UIView.library.Get<ExceptionPanel>("ExceptionPanel");
                    reportText = reportPanel.Find<UIScrollablePanel>("Scrollable Panel").Find<UILabel>("Message");
                    summarySprite = reportPanel.Find<UISprite>("Sprite");

                    if (contentManagerPanel != null)
                    {
                        var mods = contentManagerPanel.Find<UITabContainer>("CategoryContainer").Find("Mods").Find("Content");
                        if (mods != null)
                        {
                            foreach (var i in mods.components)
                            {
                                var entry = i.GetComponent<PackageEntry>();
                                if (entry != null)
                                {
                                    OnContentManagerSetEntry(entry, entry.pluginInfo);
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] WARNING -  OnEnable() failed: ({Report.ExMessage(ex, true)})");
            }

        }

        internal void OnDisabled(PluginInfo self, bool yieldToMain)
        {
            try
            {
#if REPORT_AUTHORS
                workshop.eventUGCRequestUGCDetailsCompleted -= AuthorNameCache;
#endif
                Singleton<PluginManager>.instance.eventPluginsChanged -= UpdateReport;
                SceneManager.sceneLoaded -= OnScene;

                OutputReport(self.userModInstance as Mod, true);

                /* If the Content manager was not shown, nothing to restore */
                if (m_optionsButtonOriginalText != null)
                {
                    ContentManagerPanel contentManagerPanel = UIView.library?.Get<ContentManagerPanel>("ContentManagerPanel");
                    if (contentManagerPanel != null)
                    {
                        var mods = contentManagerPanel.Find<UITabContainer>("CategoryContainer").Find("Mods").Find("Content");
                        if (mods != null)
                        {
                            foreach (var entry in mods.GetComponentsInChildren<PackageEntry>())
                            {
                                if (!yieldToMain)
                                {
                                    entry.component.tooltip = null;
                                    var name = entry.Find<UILabel>("Name");
                                    if (name != null)
                                    {
                                        name.textColor = COLOR_NORMAL;
                                    }
                                }

                                if (entry.pluginInfo == self)
                                {
                                    var optionsButton = entry.Find<UIButton>("Options");
                                    if (optionsButton != null)
                                    {
                                        optionsButton.isVisible = false;
                                        optionsButton.text = m_optionsButtonOriginalText;
                                        optionsButton.eventClick -= ShowReport;
                                        optionsButton.eventClick += entry.OpenOptions;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] WARNING -  Report.OnDisabled failed: ({Report.ExMessage(ex, true)})");
            }
        }

        internal void OnContentManagerSetEntry(PackageEntry entry, PluginInfo plugin)
        {

            if (plugin != null)
            {
                var modReport = GetReport(plugin);

                Color32 color;
                string tooltip = string.Empty;

                if (modReport.numProblems != 0)
                {

                    if (modReport.missingAssemblies.Count != 0)
                    {
                        if (modReport.missingAssemblies.Count <= 3)
                        {
                            foreach (var missing in modReport.missingAssemblies)
                            {
                                tooltip += $"Missing dependency: {missing.Name}[{missing.Version}]\n";
                            }
                        }
                        else
                        {
                            tooltip += $"Missing {modReport.missingAssemblies.Count} dependencies\n";
                        }
                    }
                    tooltip += modReport.ProblemSummary(ReportFormat.PlainText, string.Empty, MAX_PROBLEMS_IN_TOOLTIP);

                    color = COLOR_WARNING;
                }
                else
                {

                    if (plugin != Mod.mainMod)
                    {
                        if (modReport.usesHarmony)
                        {
                            if (plugin == Mod.helperMod)
                            {
#if DEVELOPER
                                tooltip = "Ensures Harmony loads first. ";
                                if ((Mod.mainMod.userModInstance as Mod).isHelperFirst)
                                {
                                    color = COLOR_USES_HARMONY_GOOD;
                                    tooltip += "It is functioning correctly.";
                                } else
                                {
                                    color = COLOR_WARNING;
                                    tooltip += "It's NOT set up correctly; Please fix it as described in the log, for reliability.";
                                }
#else
                                color = COLOR_USES_HARMONY_GOOD;
                                tooltip = "This copy is inactive.";
#endif
                            }
                            else
                            {
                                color = COLOR_USES_HARMONY_GOOD;
                                // tooltip = "Uses Harmony";
                            }
                        } else
                        {
                            color = COLOR_NORMAL;
                        }
                    }
                    else
                    {
#if DEVELOPER
                        if (Mod.helperMod == null && !(Mod.mainMod.userModInstance as Mod).isFirst)
                        {
                            color = COLOR_WARNING;
                            tooltip = "Provides Harmony to mods that need it. Not loaded first! Please install local HELPER";
                        } else
#endif
                        {
                            color = Mod.mainModInstance.IsFreshInstall ? COLOR_SELF_FRESH_GOOD : COLOR_SELF_GOOD;
                            tooltip = "Provides Harmony to mods that need it. Installed and working optimally.";
                        }
                    }
                }
                if (tooltip != string.Empty)
                {
                    tooltip += $"\n\nDetails in '{REPORT_TITLE}'";
                } else
                {
                    tooltip = null;
                }

                (entry.Find("Name") as UILabel).textColor = color;
                entry.component.tooltip = tooltip;

                var optionsButton = entry.Find<UIButton>("Options");
                if (optionsButton != null)
                {
                    if (m_optionsButtonOriginalText == null)
                    {
                        m_optionsButtonOriginalText = optionsButton.text;
                    }

                    if (plugin == Mod.mainMod)
                    {
                        /* FIXME: After disabling and re-enabling, the Report button remains unshown.
                         * If you scroll up and down, the package entry is redrawn with the button
                         */
                        optionsButton.text = "REPORT";
                        optionsButton.isEnabled = true;
                        optionsButton.isVisible = true;

                        optionsButton.eventClick -= entry.OpenOptions;
                        optionsButton.eventClick += ShowReport;
                    }
                    else
                    {
                        optionsButton.text = m_optionsButtonOriginalText;
                        optionsButton.eventClick -= ShowReport;
                    }
                }
            }
        }

        string ProblemSummarySprite(uint problems)
        {
            if (selfDiag_problems[(int)SelfProblemType.WrongHarmonyLib] ||
                selfDiag_problems[(int)SelfProblemType.ManifestMismatch] ||
                selfDiag_problems[(int)SelfProblemType.NotLoadedFirst] ||
                selfDiag_problems[(int)SelfProblemType.HarmonyLibNotFunctional])
            {
                return "NotificationIconVeryUnhappy";
            }
            else
            /* Will add a more nuanced health indicator as more thorough
             * diagnosis is available.
             * At current release, only some basic vitals are checked.
             */
            if (problems > 5)
            {
                return "NotificationIconNotHappy";
            }
            else
            {
                return "NotificationIconHappy";
            }
        }

        internal void SelfDiagnostics(Mod self)
        {
#if DEVELOPER
            if (Mod.helperMod == null && (!self.isFirst || !self.isLocal))
            {
                report += "IMPORTANT: Harmony must be loaded first to capture errors reliably. To this end, it takes the\n" +
                    "              following steps:\n" +
                    "\n" +
                    $"              * It keeps a helper local copy of itself at {ColossalFramework.IO.DataLocation.modsPath}\\{Mod.RECOMMENDED_LOCAL_HELPER_DIRNAME}\n" +
                    "               * The local helper is automatically updated when the workshop version runs,\n" +
                    "                 when the workshop updates.\n" +
                    $"               * The workshop mod includes a file named {Assembly.GetExecutingAssembly().GetName().Name}_helper_dll;\n" +
                    $"                 this file gets copied as {Assembly.GetExecutingAssembly().GetName().Name}.dll in the local helper folder\n" +
                    "               * The helper_dll file is identical to the main DLL, but with a version\n" +
                    "                 number higher by one which enables the game to recognize the two\n" + 
                    "                 instances as distinct\n" +
                    "\n" +

                    $"  Your active Harmony is currently '{Mod.mainMod.modPath}'\n" +
                    "\n" +
                    " Explanation: Mods in the local folder are loaded before workshop mods. They are also\n" +
                    "              loaded in alphabetical order. The Harmony mod monitors the load order, and\n" +
                    "              will alert you if another mod is ever loaded before it, which will need\n" +
                    "              manual intervertion. It does this by moving to the top of the mod list in\n" +
                    "              the Content Manager, coloring its description in orange, and showing a brief\n" +
                    "              problem description instead of the regular description.\n" +
                    "\n" +
                    "              Whenever you run in offline mode, with the Workshop disabled, the local\n" +
                    "              HELPER copy will behave as a full Harmony mod (its title changes from\n" +
                    "              'Harmony HELPER' to 'Harmony' in the Content Manager, depending on what\n" +
                    "              role it is playing). If you do play offline, you should update Harmony\n" +
                    "              at the same time as you update your other assets/mods. It doesn't require\n" +
                    "              more frequent updates.\n" +
                    "\n" +
                    "\n NOTE: The Harmony mod is aware if you have multiple copies installed, and will do the\n" +
                    "              \'correct\' thing, which is:\n" +
                    "              * The most recent copy will be the main mod, and provides updated fault reporting.\n" +
                    "              * The first-loaded copy will be a HELPER, doing nothing but loading the main copy\n" +
                    "                then remaining inactive.\n" +
                    "              * any other copy will remain inactive.\n" +
                    "              * The HELPER copy (which is an identical copy of the workshop version) will\n" +
                    "                automatically become active if it is the only installed copy.\n\n";
            }

            if (Mod.helperMod != null && (!self.isHelperFirst || !self.isHelperLocal))
            {
                report += "IMPORTANT: Harmony must be loaded first to work reliably by using a local HELPER copy\n" +
                    "\n" +
                    $" On this system, a {(self.isHelperLocal ? "local " : string.Empty)} helper exists, but it is not\n" +
                    $" {(!self.isHelperFirst ? "loaded first" : "locally installed")}" +
                    $"{(self.isHelperLocal ? string.Empty : " (possibly because it is not locally installed)")}; this makes it undependable.\n" +
                    " To remedy this problem, copy the Workshop folder to your local mod folders:\n" +
                    "\n" +
                    $"  Your Workshop Harmony is at '{Mod.mainMod.modPath}'\n" +
                    $"  It should be copied to '{ColossalFramework.IO.DataLocation.modsPath}\\{Mod.RECOMMENDED_LOCAL_HELPER_DIRNAME}\\\n\n";
            }
#endif
            if (selfDiag_problems[(int)SelfProblemType.ManifestMismatch])
            {
                report += "MANIFEST MISMATCH\n" +
                    "              The Harmony Mod is distributed with several libraries, which it uses.\n" +
                    "              Some of these distributed libraries were not loaded, or equivalent libraries\n" +
                    "              were loaded which were not distributed with the Harmony Mod, and cannot be\n" +
                    "              assumed to be compatible\n" +
                    "              The conflict list may be useful. If removing conflicting mods does not resolve this issue,\n" +
                    "              please make a bug report to the Harmony Mod author and include this full log file\n" +
                    "\n";

            }
            if (selfDiag_problems[(int)SelfProblemType.HarmonyLibNotFunctional])
            {
                report += "HARMONY LIBRARY NOT FUNCTIONAL\n" +
                    "              The Harmony library included with the mod is not functioning on your system\n" +
                    "              This is due to an unforeseen incompatibility, and is a bug in the Harmony Mod.\n" +
                    "              Please make a bug report to the Harmony Mod author and include this full log file\n" +
                    "\n";
            }
            /* FIXME: This should be the list of conflicting mods */
            if (selfDiag_problems[(int)SelfProblemType.WrongHarmonyLib]) 
            {
                report += "WRONG HARMONY LIBRARY LOADED\n" +
                    "              A Harmony2 library was found, but not the expected one (missing enable feature)\n" +
                    $"              It is most likely coming from another mod, and should be listed in the {REPORT_TITLE}\n" +
                    "              You should remove the offending mod.\n" +
                    "              If you believe this conflict should not have happened, please submit a bug report to the\n" +
                    "              Harmony Mod author, including a copy of this log file.\n" +
                     "\n";
            }
            if (selfDiag_problems[(int)SelfProblemType.FailedToGetAssemblyLocations] ||
                selfDiag_problems[(int)SelfProblemType.FailedToInitialize] ||
                selfDiag_problems[(int)SelfProblemType.FailedToUninstall] ||
                selfDiag_problems[(int)SelfProblemType.FailedToReceiveReports] ||
                selfDiag_problems[(int)SelfProblemType.FailedToYieldToMain])
            {
                report += "UNFORESEEN FAULT\n" +
                    "              Some fault happened that could have been foreseen and handled, but wasn't.\n" +
                    "              This is a bug in the Harmony Mod.\n" +
                    "              Please make a bug report to the Harmony Mod author and include this full log file\n" +
                    "\n";

            }
            if (selfDiag_problems[(int)SelfProblemType.OwnPatchInstallation] ||
                selfDiag_problems[(int)SelfProblemType.OwnPatchRemoval])
            {
                report += "FAILED TO INSTALL OWN PATCHES\n" +
                    "              The Harmony Library failed to install or remove its own patches.\n" +
                    "              This is likely due some functional conflict with another mod or a recent game update\n" +
                    "              The circumstances require investigation, and will likely result in a compatibility update\n" +
                    "              It is MOST LIKELY that the Harmony Mod you are using is out-of-date. Please check for a newer\n" +
                    "              version.\n" +
                    "              If this is the most recent release, please forward the full log file to the Harmony Mod author\n" +
                    "\n";
            }

        }

        internal static string SelfProblemText(SelfProblemType problem)
        {
            return problem switch
            {
                SelfProblemType.CompatibilityPatchesFailed => "Failed to patch old Harmony Lib",
                SelfProblemType.FailedToGetAssemblyLocations => "Failed to get assembly locations",
                SelfProblemType.FailedToInitialize => "Failed to initialize Harmony Mod",
                SelfProblemType.FailedToReceiveReports => "Failed to receive mod reports",
                SelfProblemType.FailedToReport => $"Failed to generate the {REPORT_TITLE}",
                SelfProblemType.FailedToUninstall => "Failed to disable Harmony Mod",
                SelfProblemType.FailedToYieldToMain => "Failed to yield to main Harmony Mod",
                SelfProblemType.HarmonyLibNotFunctional => "Harmony Lib is not functional",
                SelfProblemType.ManifestMismatch => "Loaded Assemblies don't match manifest",
                SelfProblemType.OwnPatchInstallation => "Failed to install own patches",
                SelfProblemType.OwnPatchRemoval => "Failed to revert own patches",
                SelfProblemType.WrongHarmonyLib => "Wrong Harmony library is loaded(missing Harmony.isEnabled)",
#if DEVELOPER
                SelfProblemType.NotLoadedFirst => "Is not loaded first",
                SelfProblemType.HelperInstallationFailed => "Installing local HELPER failed (will try again next mod start)",
#endif
                _ => problem.ToString(),
            };
        }

        internal void ReportSelf(SelfProblemType problem, Exception e)
        {
            selfDiag_problems[(int)problem] = true;
            if (e != null)
            {
                GetReport(Mod.mainMod).ReportProblem(ModReport.ProblemType.ExceptionThrown, e, SelfProblemText(problem));
#if DEBUG || TRACE
                LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] ERROR: Exception ({e.GetType().Name}): {e.Message}:\n{e.StackTrace}");
#endif
            }
#if DEVELOPER
            else
            {
                GetReport(Mod.mainMod).ReportProblem(ModReport.ProblemType.HelperNotLoadedFirst);
            }
#endif

        }

        internal void ReportUnsupportedHarmony(HarmonyModSupportException e)
        {
            unsupportedLibs = e;
            modReports.Do((r) => (r.Value as ModReport).ReportUnsupportedHarmony(unsupportedLibs));
        }

        /* Returns the number of problems reported so far to the plugin
         * If a responsible plugin is not found, return 0
         */
        internal uint PluginFailureCount(Exception exception)
        {
            int level = 0;
            for (var e = exception; e != null; e = e.InnerException)
            {
                var stackTrace = new System.Diagnostics.StackTrace(e, true);
                /* This prints a duplicate exception in the log */

                if (e is HarmonyModSupportException)
                {
                    break;
                }
                ++level;

                PluginInfo mod = e.TargetSite != null ? Singleton<PluginManager>.instance.FindPluginInfo(e.TargetSite.GetType().Assembly) : null;
                if (mod != null)
                {
                    var modReport = GetReport(mod);
                    IsNotNull(modReport, $"A ModReport should already exist for {mod.modPath}");
                    return modReport.numProblems;
                }
                else
                {
                    bool done = false;
                    for (int i = 0; i < stackTrace.FrameCount && !done; ++i)
                    {
                        var frame = stackTrace.GetFrame(i);
                        var method = frame.GetMethod();
                        var a = method.ReflectedType.Assembly;
                        mod = Singleton<PluginManager>.instance.FindPluginInfo(a);
                        if (mod != null)
                        {
                            var modReport = GetReport(mod);
                            IsNotNull(modReport, $"A ModReport should already exist for {mod.modPath}");
                            return modReport.numProblems;
                        }
                    }
                }
                if (e is HarmonyModSupportException || e is HarmonyModACLException || Patcher.isHarmonyUserException(e)) break;
            }

            return 0;
        }

        /* Returns the number of problems reported so far to the plugin
         * If a responsible plugin is not found, return 0
         */
        internal uint TryReportPlugin (Exception exception)
        {
            uint firstFailureProblemCount = 0, firstTriggerCount = 0;

            PluginInfo triggeringMod = null,
                failingMod = null;
            string triggerInfo = string.Empty;
            bool triggerIsFailure = false;

            int level = 0;
            for (var e = exception; e != null; e = e.InnerException)
            {
                var stackTrace = new System.Diagnostics.StackTrace(e, true);
                /* This prints a duplicate exception in the log */

                triggerIsFailure = e is HarmonyModSupportException || e is HarmonyModACLException || Patcher.isHarmonyUserException(e);
                if (e is HarmonyModSupportException)
                {
                    ReportUnsupportedHarmony(e as HarmonyModSupportException);
                    break;
                }
                ++level;
                if (firstFailureProblemCount != 0) continue;

                PluginInfo mod = e.TargetSite != null ? Singleton<PluginManager>.instance.FindPluginInfo(e.TargetSite.GetType().Assembly) : null;
                if (mod != null)
                {
                    var n = GetReport(mod, ModReport.ProblemType.ExceptionThrown, e, $"Caught Exception @{e.TargetSite.GetType().FullName}");
                    if (firstFailureProblemCount == 0)
                    {
                        firstFailureProblemCount = n;
                    }
                }
                else
                {
                    bool done = false;
                    for (int i = 0; i < stackTrace.FrameCount && !done; ++i)
                    {
                        var frame = stackTrace.GetFrame(i);
                        var method = frame.GetMethod();
                        var a = method.ReflectedType.Assembly;
                        mod = Singleton<PluginManager>.instance.FindPluginInfo(a);
                        if (mod != null)
                        {
                            uint n = 0;
                            string location = frame.GetFileName() != null ? $" in {frame.GetFileName()}:{frame.GetFileLineNumber()}" : string.Empty;
                            {
                                if (!triggerIsFailure && (i == 0 || e is AssertionException))
                                {
                                    string desc;
                                    if (e is AssertionException)
                                    {
                                        var assertion = e as AssertionException;
                                        desc = $"{e.GetType().Name}: {assertion.Message.Replace(System.Environment.NewLine, "; ")} at {method.DeclaringType.FullName}.{method.Name}{location}";
                                        done = true;
                                    }
                                    else
                                    {
                                        desc = $"{e.GetType().Name} at {method.DeclaringType.FullName}.{method.Name}{location}";
                                    }
                                    n = GetReport(mod, ModReport.ProblemType.ExceptionThrown, e, desc); // @ {frame.GetFileName()} : {frame.GetFileLineNumber()}");
                                    failingMod = mod;
                                }
                                else
                                {
                                    triggerInfo = $"{e.GetType().Name}: {e.Message} from {method.DeclaringType.FullName}.{method.Name}{location}";
                                    triggeringMod = mod;
                                }
                            }

                            if (firstFailureProblemCount == 0)
                            {
                                firstFailureProblemCount = n;
                            }
                        }
                    }
                }
                if (e is HarmonyModSupportException || e is HarmonyModACLException || Patcher.isHarmonyUserException(e)) break;
            }
            string triggerStr = string.Empty;
            if (triggeringMod != null)
            {
                firstTriggerCount = GetReport(triggeringMod, ModReport.ProblemType.ExceptionTriggered, exception, triggerInfo);
                if (triggerIsFailure)
                    firstFailureProblemCount = firstTriggerCount;
            }

            return firstFailureProblemCount;
        }

        internal static PluginInfo FindCallerMod(System.Diagnostics.StackTrace stackTrace, out AssemblyName guiltyAssembly)
        {
            PluginInfo origin = null;
            AssemblyName from = default(AssemblyName);

            if (stackTrace.GetFrames().Any((x) =>
            {
                var callerAssembly = x.GetMethod().ReflectedType.Assembly;

                var mod = Mod.mainModInstance.report.m_Plugins.Values.FirstOrDefault((p) => p.ContainsAssembly(callerAssembly));
                if (mod != null)
                {
                    origin = mod;
                    from = callerAssembly.GetName();
                    return true;
                }
                return false;
            }))
            {
                guiltyAssembly = from;
            }
            else
            {
                guiltyAssembly = default(AssemblyName);
            }

            return origin;
        }

        internal void ReportPlugin (PluginInfo plugin, ModReport.ProblemType problem, string detail = null)
        {
            GetReport(plugin).ReportProblem(problem, detail);
        }
        internal void ReportPlugin(PluginInfo plugin, ModReport.ProblemType problem, Exception ex, string detail = null)
        {
            GetReport(plugin).ReportProblem(problem, ex, detail);
        }
        internal void ReportPlugin(ModReport.ProblemType problem, AssemblyName modAssembly, Exception ex)
        {
            var plugin = Mod.mainModInstance.report.m_Plugins.Values.FirstOrDefault((p) => p.ContainsAssembly(modAssembly));

            if (plugin != null)
                GetReport(plugin).ReportProblem(problem, ex);
            else
            {
                LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR - no responsible mod found; detail: {Report.ExMessage(ex.InnerException, true, 1)}");
                TryReportPlugin(ex);
            }
        }
        internal void ReportPlugin(ModReport.ProblemType problem, Exception ex, int skipframes, out AssemblyName guiltyMod, AssemblyName assemblyName)
        {
            var plugin = FindCallerMod(new System.Diagnostics.StackTrace(skipframes + 1, true), out guiltyMod);
            if (plugin != null)
                GetReport(plugin).ReportProblem(problem, ex, assemblyName);
            else
            {
                LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR - no responsible mod found; detail: {Report.ExMessage(ex.InnerException, true, 1)}");
                TryReportPlugin(ex);
            }
        }
        internal void ReportActivity (string activity)
        {
            activities.Add(activity);
        }

        internal uint numSelfProblems
        {
            get
            {
                if (m_modReports.TryGetValue(Mod.mainMod, out ModReportBase r))
                {
                    return r.numProblems;
                }
                return 0;
            }
        }

        internal Dictionary<PluginInfo, ModReportBase> modReports { get { return m_modReports; } }
        internal void PutModReports(Dictionary<PluginInfo, ModReportBase> reports)
        {
            foreach (var i in reports)
            {
                if (m_modReports.TryGetValue(i.Key, out ModReportBase modReport))
                {
                    (modReport as ModReport).Merge(i.Value);
                } else {
                    modReport = new ModReport(i.Key, i.Value);
                    m_modReports[i.Key] = modReport;
                }

            }
        }

        void CheckConflicts()
        {
            var missingAssemblies = manifest;
            var mytoken = Assembly.GetExecutingAssembly().GetName().GetPublicKeyToken();
            if (mytoken != null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {

                    var name = assembly.GetName();
                    var nameVer = $"{name.Name}, Version={name.Version}";
                    if (missingAssemblies.Contains(nameVer))
                    {
                        if (name.GetPublicKeyToken() == null || !name.GetPublicKeyToken().SequenceEqual(mytoken))
                        {
                            Mod.SelfReport(SelfProblemType.ManifestMismatch, new Exception($"Wrong Assembly Loaded: {name.FullName}"));
                            GetReport(Mod.mainMod).ReportProblem(ModReport.ProblemType.ModConflict, assembly);
                            LogError($"[{Versioning.FULL_PACKAGE_NAME} ERROR Conflicting Assembly: {name.FullName}");
                        } else
                        {
                            missingAssemblies.Remove(nameVer);
                        }
                    }
                }

                foreach (var a in missingAssemblies)
                {
                    GetReport(Mod.mainMod).missingAssemblies.Add(new AssemblyName(a));
                    Mod.SelfReport(SelfProblemType.ManifestMismatch, new Exception($"Expected assembly not loaded: {a}"));
                }
            }
        }


        internal static string ExMessage(Exception e, bool wantBugStackTrace, int skipFrames = 0)
        {
            string message = string.Empty;
            for (var inner = e; inner != null; inner = inner.InnerException)
            {
                if (inner is NullReferenceException ||
                    inner is DivideByZeroException ||
                    inner is IndexOutOfRangeException)
                {
                    message += ((e != inner) ? ": " : string.Empty) + "[bug] " + inner.Message;
                    if (wantBugStackTrace)
                    {
                        if (e != inner)
                        {
                            var failureLocation = (e.TargetSite != null) ? ":\n{ e.StackTrace}" : $" reported:\n{new System.Diagnostics.StackTrace(1 + skipFrames, true)}";
                            LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR - " +
                                $"A {inner.GetType().Name} ({inner.Message}) Bug:\n{inner.StackTrace}\n\n" +
                                $"... Caused a {e.GetType().Name} ({e.Message}){failureLocation}");
                        } else
                        {
                            LogError($"[{Versioning.FULL_PACKAGE_NAME}] ERROR - " +
                                $"a {inner.GetType().Name} ({inner.Message}) Bug exists:\n{inner.StackTrace}");
                        }
                    }
                    break;
                } else
                {
                    message += ((e != inner) ? ": " : string.Empty) + inner?.Message;
                }

            }
            return message;
        }
        internal static HarmonyModACLException GetAPIMisuseException(AssemblyName caller, out string reason)
        {
            reason = "CitiesHarmony.API misuse by " + caller.Name + "[" + caller.Version + "]";
            var ex = new HarmonyModACLException(reason + " is prohibited");
            ex.HelpLink = $"{Versioning.ISSUES_URL}/8";
            return ex;
        }

    }
}
