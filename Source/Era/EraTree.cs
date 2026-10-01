using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Genesis
{
    // On a ResearchTabDef: its projects move onto the Genesis era tab matching their tech level
    public class FoldIntoEras : DefModExtension
    {
    }

    // Builds the era tree at startup, so it fits whichever mods are active:
    // folds projects onto era tabs, lays each tab out in branch lanes, gives each Capstone its era as hidden prerequisites,
    // and makes each era's starting projects need the previous Capstone.
    public static class EraTree
    {
        private const float RowStep = 0.7f;
        private const float LaneGap = 0.4f;

        private static readonly AccessTools.FieldRef<ResearchProjectDef, float> X = AccessTools.FieldRefAccess<ResearchProjectDef, float>("x");
        private static readonly AccessTools.FieldRef<ResearchProjectDef, float> Y = AccessTools.FieldRefAccess<ResearchProjectDef, float>("y");

        public static readonly Dictionary<TechLevel, ResearchTabDef> Tabs = new Dictionary<TechLevel, ResearchTabDef>();
        private static readonly Dictionary<ResearchProjectDef, bool> specialCache = new Dictionary<ResearchProjectDef, bool>();
        private static HashSet<ResearchProjectDef> codexProjects = new HashSet<ResearchProjectDef>();

        // Project -> (branch order, position in the branch's list)
        private static readonly Dictionary<ResearchProjectDef, (int Lane, int Index)> branchOf = new Dictionary<ResearchProjectDef, (int, int)>();

        public static bool IsEraTab(ResearchTabDef? tab) => tab != null && Tabs.ContainsValue(tab);

        public static void Build()
        {
            foreach (var level in new[] { TechLevel.Animal, TechLevel.Neolithic, TechLevel.Medieval, TechLevel.Industrial, TechLevel.Spacer, TechLevel.Ultra })
            {
                var tab = DefDatabase<ResearchTabDef>.GetNamedSilentFail(level == TechLevel.Animal ? "Genesis_Tribal" : "Genesis_" + level);
                if (tab != null)
                {
                    Tabs[level] = tab;
                }
            }
            codexProjects = new HashSet<ResearchProjectDef>(DefDatabase<EntityCodexEntryDef>.AllDefs.SelectMany(e => e.discoveredResearchProjects ?? new List<ResearchProjectDef>()));

            var all = DefDatabase<ResearchProjectDef>.AllDefsListForReading;
            var capstones = all.Where(p => p.HasModExtension<CapstoneExtension>()).ToList();

            foreach (var branch in DefDatabase<BranchDef>.AllDefsListForReading.Where(b => b.Active))
            {
                for (var i = 0; i < branch.projects.Count; i++)
                {
                    var p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(branch.projects[i]);
                    if (p != null && !branchOf.ContainsKey(p))
                    {
                        branchOf[p] = (branch.order, i);
                    }
                }
            }

            foreach (var p in all)
            {
                var fold = branchOf.ContainsKey(p) || (p.tab != null && p.tab.HasModExtension<FoldIntoEras>());
                if (!capstones.Contains(p) && fold && Tabs.TryGetValue(p.techLevel, out var eraTab))
                {
                    p.tab = eraTab;
                }
            }

            foreach (var tab in Tabs.Values)
            {
                Layout(tab, all, capstones);
            }

            foreach (var capstone in capstones)
            {
                var members = all.Where(p => p.tab != null && IsEraTab(p.tab) && p.techLevel == capstone.techLevel && !capstones.Contains(p)
                    && !SupersededResearch.IsSuperseded(p) && !IsSpecial(p)).ToList();
                capstone.hiddenPrerequisites = (capstone.hiddenPrerequisites ?? new List<ResearchProjectDef>()).Union(members).ToList();
            }

            foreach (var capstone in capstones)
            {
                var era = Era.Target(capstone);
                foreach (var p in all)
                {
                    if (p.techLevel != era || !IsEraTab(p.tab) || capstones.Contains(p) || SupersededResearch.IsSuperseded(p))
                    {
                        continue;
                    }
                    var prereqs = (p.prerequisites ?? new List<ResearchProjectDef>()).Concat(p.hiddenPrerequisites ?? new List<ResearchProjectDef>());
                    if (!prereqs.Any(q => q.techLevel >= era))
                    {
                        p.prerequisites ??= new List<ResearchProjectDef>();
                        p.prerequisites.Add(capstone);
                    }
                }
            }
        }

        // A project the Capstone can't wait on: techprints, a mechanitor, studying items, grav engine inspection,
        // Anomaly knowledge, codex discovery, a difficulty setting that can hide it, or any prerequisite like that
        public static bool IsSpecial(ResearchProjectDef p)
        {
            if (specialCache.TryGetValue(p, out var cached))
            {
                return cached;
            }
            specialCache[p] = false;
            var h = p.hideWhen;
            var special = p.TechprintCount > 0 || p.requiresMechanitor || !p.requiredAnalyzed.NullOrEmpty() || p.requireGravEngineInspected
                || p.knowledgeCost > 0f || codexProjects.Contains(p)
                || (h != null && (h.bigThreatsDisabled || h.trapsDisabled || h.turretsDisabled || h.mortarsDisabled || h.extremeWeatherIncidentsDisabled))
                || (p.prerequisites ?? new List<ResearchProjectDef>()).Concat(p.hiddenPrerequisites ?? new List<ResearchProjectDef>()).Any(IsSpecial);
            specialCache[p] = special;
            return special;
        }

        // One lane per branch, top to bottom; projects without a branch go last. Column = prerequisite depth on this tab.
        // Projects sharing a lane and column stack in their branch's listed order. The Capstone goes one column after the rest.
        private static void Layout(ResearchTabDef tab, List<ResearchProjectDef> all, List<ResearchProjectDef> capstones)
        {
            var projects = all.Where(p => p.tab == tab && !capstones.Contains(p) && !SupersededResearch.IsSuperseded(p)).ToList();
            var onTab = new HashSet<ResearchProjectDef>(projects);
            var depth = new Dictionary<ResearchProjectDef, int>();
            int Depth(ResearchProjectDef p)
            {
                if (depth.TryGetValue(p, out var d))
                {
                    return d;
                }
                depth[p] = 0;
                var prereqs = (p.prerequisites ?? new List<ResearchProjectDef>()).Where(onTab.Contains).ToList();
                d = prereqs.Count == 0 ? 0 : prereqs.Max(Depth) + 1;
                depth[p] = d;
                return d;
            }

            var cursor = 0f;
            var maxColumn = -1;
            var lanes = projects.GroupBy(p => branchOf.TryGetValue(p, out var b) ? b.Lane : int.MaxValue).OrderBy(g => g.Key);
            foreach (var lane in lanes)
            {
                var columns = lane.GroupBy(Depth).ToList();
                var rows = columns.Max(c => c.Count());
                foreach (var column in columns)
                {
                    var ordered = column.OrderBy(p => branchOf.TryGetValue(p, out var b) ? b.Index : int.MaxValue).ThenBy(p => Y(p)).ThenBy(p => p.defName).ToList();
                    for (var i = 0; i < ordered.Count; i++)
                    {
                        SetPosition(ordered[i], column.Key, cursor + i * RowStep);
                    }
                    maxColumn = Mathf.Max(maxColumn, column.Key);
                }
                cursor += rows * RowStep + LaneGap;
            }

            var capstone = capstones.FirstOrDefault(c => c.tab == tab);
            if (capstone != null)
            {
                var midY = projects.Count > 0 ? (projects.Min(p => Y(p)) + projects.Max(p => Y(p))) / 2f : 0f;
                SetPosition(capstone, maxColumn + 1, Mathf.Round(midY * 10f) / 10f);
            }
        }

        private static void SetPosition(ResearchProjectDef p, float x, float y)
        {
            p.researchViewX = x;
            p.researchViewY = y;
            X(p) = x;
            Y(p) = y;
        }
    }
}
