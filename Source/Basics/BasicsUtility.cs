#nullable disable
using RimWorld;
using Verse;

namespace Genesis.Basics
{
    public static class BasicsUtility
    {
        public static bool StartingOnAnimalTech => StartingTechLevel() == TechLevel.Animal;

        public static TechLevel StartingTechLevel()
        {
            var playerFaction = Find.GameInitData?.playerFaction;
            if (playerFaction?.def != null)
            {
                return playerFaction.def.techLevel;
            }
            var factionDef = Find.Scenario?.playerFaction?.factionDef;
            if (factionDef != null)
            {
                return factionDef.techLevel;
            }
            return Faction.OfPlayerSilentFail?.def?.techLevel ?? TechLevel.Undefined;
        }
    }
}
