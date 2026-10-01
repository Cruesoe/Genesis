#nullable disable
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Genesis.Furniture
{
    [StaticConstructorOnStartup]
    public static class ResearchPrerequisiteOverride
    {
        static ResearchPrerequisiteOverride()
        {
            LongEventHandler.ExecuteWhenFinished(Apply);
        }

        private static void Apply()
        {
            SetPrerequisites("RB_GlassWall", "Genesis_WindowWall", "Smithing");
            SetPrerequisites("RB_ReinforcedGlassWall", "Genesis_WindowWall", "Smithing");
            SetPrerequisites("RB_ClerestoryWall", "Genesis_WindowWall", "Smithing");
            SetPrerequisites("RB_ReinforcedClerestoryWall", "Genesis_WindowWall", "Smithing");
            SetPrerequisites("VFE_LongPlantPot", "Genesis_PlantPots");
            SetPrerequisites("Table_Counter", "ComplexFurniture", "Genesis_Tables");
            SetPrerequisites("Crib", "Genesis_Crib", "Ferny_RoughCrib");
        }

        // Replaces the building's research with the given projects; skipped if the building or first project is missing, later projects only when loaded
        private static void SetPrerequisites(string defName, string project, params string[] extras)
        {
            ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            ResearchProjectDef main = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(project);
            if (thingDef == null || main == null)
            {
                return;
            }

            var prerequisites = new List<ResearchProjectDef> { main };
            foreach (string extra in extras)
            {
                ResearchProjectDef extraDef = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(extra);
                if (extraDef != null && !prerequisites.Contains(extraDef))
                {
                    prerequisites.Add(extraDef);
                }
            }

            thingDef.researchPrerequisites = prerequisites;
        }
    }
}
