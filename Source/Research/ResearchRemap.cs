using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Genesis
{
    // On a research project: items (things, terrain or recipes, by defName) that move to it from a superseded project
    public class UnlocksItems : DefModExtension
    {
        public List<string> items = new List<string>();
    }

    // Replaces superseded projects in every research gate: buildable and plant prerequisites, item recipes and recipe defs.
    // An item listed by a project (UnlocksItems) goes there, ungated items included; otherwise it keeps its other gates, or takes its old project's itemsTo.
    public static class ResearchRemap
    {
        private static readonly Dictionary<string, ResearchProjectDef> listed = new Dictionary<string, ResearchProjectDef>();
        private static readonly Dictionary<ResearchProjectDef, SupersededResearch> superseded = new Dictionary<ResearchProjectDef, SupersededResearch>();
        private static readonly HashSet<string> dropOnly = new HashSet<string>();

        public static void Apply()
        {
            foreach (var proj in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
            {
                var ext = proj.GetModExtension<SupersededResearch>();
                if (ext?.by != null)
                {
                    superseded[proj] = ext;
                    dropOnly.UnionWith(ext.dropOnly ?? new List<string>());
                }
                foreach (var name in proj.GetModExtension<UnlocksItems>()?.items ?? new List<string>())
                {
                    if (listed.TryGetValue(name, out var other) && other != proj)
                    {
                        Log.Warning($"[Genesis] {name} is listed by both {other.defName} and {proj.defName}; using {proj.defName}.");
                    }
                    listed[name] = proj;
                }
            }
            if (superseded.Count == 0)
            {
                return;
            }

            // A listed item also gains its project where nothing superseded gated it: on construction if it's buildable, on sowing if
            // it's sowable, and on every recipe that makes it
            foreach (var thing in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                thing.researchPrerequisites = Remap(thing.researchPrerequisites, thing.defName, thing.BuildableByPlayer);
                if (thing.plant != null)
                {
                    thing.plant.sowResearchPrerequisites = Remap(thing.plant.sowResearchPrerequisites, thing.defName, thing.plant.Sowable);
                }
                if (thing.recipeMaker != null)
                {
                    var gates = Combine(thing.recipeMaker.researchPrerequisite, thing.recipeMaker.researchPrerequisites);
                    if (gates.Any(superseded.ContainsKey))
                    {
                        thing.recipeMaker.researchPrerequisite = null;
                        thing.recipeMaker.researchPrerequisites = Remap(gates, thing.defName, false);
                    }
                }
            }
            foreach (var terrain in DefDatabase<TerrainDef>.AllDefsListForReading)
            {
                terrain.researchPrerequisites = Remap(terrain.researchPrerequisites, terrain.defName, terrain.BuildableByPlayer);
            }
            foreach (var recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                // A recipe takes the destination of the first listed product, or its own listing
                var name = recipe.products?.Select(p => p.thingDef?.defName).FirstOrDefault(n => n != null && listed.ContainsKey(n)) ?? recipe.defName;
                var gates = Combine(recipe.researchPrerequisite, recipe.researchPrerequisites);
                if (!gates.Any(superseded.ContainsKey) && !listed.ContainsKey(name))
                {
                    continue;
                }
                recipe.researchPrerequisite = null;
                recipe.researchPrerequisites = Remap(gates, name, true);
            }
        }

        private static List<ResearchProjectDef> Combine(ResearchProjectDef? one, List<ResearchProjectDef>? list)
        {
            var all = new List<ResearchProjectDef>();
            if (one != null)
            {
                all.Add(one);
            }
            if (list != null)
            {
                all.AddRange(list.Where(p => p != null && !all.Contains(p)));
            }
            return all;
        }

        // Returns the gates with superseded projects replaced, and the listed project added when addListed; unchanged lists come back as they were
        private static List<ResearchProjectDef>? Remap(List<ResearchProjectDef>? gates, string name, bool addListed)
        {
            if (gates == null || !gates.Any(superseded.ContainsKey))
            {
                if (addListed && listed.TryGetValue(name, out var add) && (gates == null || !gates.Contains(add)))
                {
                    return (gates ?? new List<ResearchProjectDef>()).Append(add).ToList();
                }
                return gates;
            }
            var old = gates.Where(superseded.ContainsKey).ToList();
            var kept = gates.Where(p => !superseded.ContainsKey(p)).ToList();
            ResearchProjectDef? dest = null;
            if (listed.TryGetValue(name, out var listedProj))
            {
                dest = listedProj;
            }
            else if (kept.Count == 0 && !dropOnly.Contains(name))
            {
                dest = old.Select(p => superseded[p].itemsTo ?? superseded[p].by).OrderByDescending(Rank).First();
            }
            if (dest != null && !kept.Contains(dest))
            {
                kept.Add(dest);
            }
            return kept.Count > 0 ? kept : null;
        }

        // Later eras rank higher; a Capstone ranks after the rest of its era
        private static int Rank(ResearchProjectDef? p) => p == null ? -1 : (int)p.techLevel * 2 + (p.HasModExtension<CapstoneExtension>() ? 1 : 0);
    }
}
