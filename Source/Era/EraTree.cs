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
    // folds projects onto era tabs, lays each tab out, gives each Capstone its era as hidden prerequisites,
    // and makes each era's starting projects need the previous Capstone.
    public static class EraTree
    {
        private static readonly AccessTools.FieldRef<ResearchProjectDef, float> X = AccessTools.FieldRefAccess<ResearchProjectDef, float>("x");
        private static readonly AccessTools.FieldRef<ResearchProjectDef, float> Y = AccessTools.FieldRefAccess<ResearchProjectDef, float>("y");

        public static readonly Dictionary<TechLevel, ResearchTabDef> Tabs = new Dictionary<TechLevel, ResearchTabDef>();
        private static readonly Dictionary<ResearchProjectDef, bool> specialCache = new Dictionary<ResearchProjectDef, bool>();
        private static HashSet<ResearchProjectDef> codexProjects = new HashSet<ResearchProjectDef>();

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
            var tabOrder = DefDatabase<ResearchTabDef>.AllDefsListForReading;

            var folded = new Dictionary<ResearchProjectDef, ResearchTabDef>();
            foreach (var p in all)
            {
                if (!capstones.Contains(p) && p.tab != null && p.tab.HasModExtension<FoldIntoEras>() && Tabs.TryGetValue(p.techLevel, out var eraTab))
                {
                    folded[p] = p.tab;
                    p.tab = eraTab;
                }
            }

            foreach (var pair in Tabs)
            {
                Layout(pair.Key, pair.Value, folded, capstones, tabOrder);
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

        // Each source tab's projects keep their layout (after the game's own overlap pass), placed left to right in tab order; the Capstone goes last
        private static void Layout(TechLevel era, ResearchTabDef tab, Dictionary<ResearchProjectDef, ResearchTabDef> folded,
            List<ResearchProjectDef> capstones, List<ResearchTabDef> tabOrder)
        {
            var cursor = 0f;
            var groups = folded.Where(f => f.Key.tab == tab).GroupBy(f => f.Value, f => f.Key).OrderBy(g => tabOrder.IndexOf(g.Key));
            var placed = new List<ResearchProjectDef>();
            foreach (var group in groups)
            {
                var minX = group.Min(p => X(p));
                var maxX = group.Max(p => X(p));
                foreach (var p in group)
                {
                    SetPosition(p, X(p) - minX + cursor, Y(p));
                }
                placed.AddRange(group);
                cursor += maxX - minX + 1f;
            }
            var capstone = capstones.FirstOrDefault(c => c.tab == tab && c.techLevel == era);
            if (capstone != null)
            {
                var midY = placed.Count > 0 ? (placed.Min(p => Y(p)) + placed.Max(p => Y(p))) / 2f : 0f;
                SetPosition(capstone, cursor, Mathf.Round(midY * 10f) / 10f);
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
