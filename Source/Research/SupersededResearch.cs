using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Genesis
{
    // Marks a research project whose content moved to another project. It stays defined (so saves and other mods can still
    // reference it), is hidden from the research screen, can't be started, and finishes when "by" finishes.
    public class SupersededResearch : DefModExtension
    {
        public ResearchProjectDef? by;

        // Where an item gated only by this project goes, unless a Genesis project lists it (ResearchRemap); default: by
        public ResearchProjectDef? itemsTo;

        // Items that just lose this gate
        public List<string>? dropOnly;

        private static readonly Dictionary<ResearchProjectDef, List<ResearchProjectDef>> byTarget = new Dictionary<ResearchProjectDef, List<ResearchProjectDef>>();
        private static readonly HashSet<ResearchProjectDef> all = new HashSet<ResearchProjectDef>();

        public static bool IsSuperseded(ResearchProjectDef proj) => all.Contains(proj);

        public static void Init()
        {
            foreach (var proj in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
            {
                var ext = proj.GetModExtension<SupersededResearch>();
                if (ext == null)
                {
                    continue;
                }
                if (ext.by == null || ext.by == proj)
                {
                    Log.Error($"[Genesis] {proj.defName} is marked superseded without a valid replacement project.");
                    continue;
                }
                // The replacement becomes its only prerequisite, so FinishProject never completes the old prerequisites
                proj.prerequisites = new List<ResearchProjectDef> { ext.by };
                proj.hiddenPrerequisites = null;
                all.Add(proj);
                if (!byTarget.TryGetValue(ext.by, out var list))
                {
                    byTarget[ext.by] = list = new List<ResearchProjectDef>();
                }
                list.Add(proj);
            }
        }

        // Finishes every unfinished project superseded by proj, following chains
        public static void FinishSupersededBy(ResearchProjectDef proj)
        {
            if (!byTarget.TryGetValue(proj, out var list))
            {
                return;
            }
            foreach (var old in list)
            {
                if (!old.IsFinished)
                {
                    Find.ResearchManager.FinishProject(old, doCompletionDialog: false, researcher: null, doCompletionLetter: false);
                }
            }
        }

        // Catches up saves made before a project was superseded
        public static void SyncAll()
        {
            foreach (var target in byTarget.Keys)
            {
                if (target.IsFinished)
                {
                    FinishSupersededBy(target);
                }
            }
        }
    }
}
