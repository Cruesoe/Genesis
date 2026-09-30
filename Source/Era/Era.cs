using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Genesis
{
    // Marks a Capstone: finishing it advances the colony to the era after the project's own tech level
    public class CapstoneExtension : DefModExtension
    {
    }

    public static class Era
    {
        private static readonly AccessTools.FieldRef<ResearchManager, Dictionary<ResearchProjectDef, float>> Progress =
            AccessTools.FieldRefAccess<ResearchManager, Dictionary<ResearchProjectDef, float>>("progress");

        private static readonly Dictionary<FactionDef, TechLevel> pristine = new Dictionary<FactionDef, TechLevel>();
        private static List<ResearchProjectDef> capstones = new List<ResearchProjectDef>();

        public static TechLevel Current => Faction.OfPlayer.def.techLevel;

        public static TechLevel Target(ResearchProjectDef capstone) => capstone.techLevel + 1;

        public static void Init()
        {
            foreach (var def in DefDatabase<FactionDef>.AllDefs.Where(f => f.isPlayer))
            {
                pristine[def] = def.techLevel;
            }
            capstones = DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(p => p.HasModExtension<CapstoneExtension>()).ToList();
        }

        // Player faction defs are shared across games, so put back the loaded values when a game is left
        public static void RestorePristine()
        {
            foreach (var pair in pristine)
            {
                pair.Key.techLevel = pair.Value;
            }
        }

        public static void Notify_ProjectFinished(ResearchProjectDef proj)
        {
            if (proj.HasModExtension<CapstoneExtension>() && Target(proj) > Current)
            {
                Set(Target(proj));
                Find.WindowStack.Add(new Window_EraAdvance(Target(proj)));
            }
        }

        // VFE Tribals sees the raise on its next tick and awards its Cornerstone point for the new era
        public static void Set(TechLevel level)
        {
            Faction.OfPlayer.def.techLevel = level;
            Verse.Current.Game.GetComponent<GameComponent_Genesis>().era = level;
        }

        // Capstones for eras already reached are marked done without finishing their prerequisites (FinishProject would)
        public static void MarkReachedCapstones()
        {
            var progress = Progress(Find.ResearchManager);
            foreach (var capstone in capstones)
            {
                if (Target(capstone) <= Current && !capstone.IsFinished)
                {
                    progress[capstone] = capstone.baseCost;
                }
            }
        }
    }

    public class GameComponent_Genesis : GameComponent
    {
        public TechLevel? era;

        public GameComponent_Genesis(Game game)
        {
        }

        public override void FinalizeInit()
        {
            if (era.HasValue)
            {
                Faction.OfPlayer.def.techLevel = era.Value;
            }
            else
            {
                era = Faction.OfPlayer.def.techLevel;
            }
            Era.MarkReachedCapstones();
            SupersededResearch.SyncAll();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref era, "era");
        }
    }
}
