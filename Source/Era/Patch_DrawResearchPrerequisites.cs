using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Genesis
{
    // A Capstone lists its visible prerequisites, then how much of its era is done and the era projects still to finish
    [HarmonyPatch(typeof(MainTabWindow_Research), "DrawResearchPrerequisites")]
    public static class Patch_MainTabWindow_Research_DrawResearchPrerequisites
    {
        public static bool Prefix(Rect rect, ref float y, ResearchProjectDef project)
        {
            if (!project.HasModExtension<CapstoneExtension>() || project.hiddenPrerequisites.NullOrEmpty())
            {
                return true;
            }
            Widgets.Label(rect, ref y, "Prerequisites".Translate() + ":");
            rect.xMin += 6f;
            foreach (var p in project.prerequisites.OrElseEmptyEnumerable())
            {
                GUI.color = Status(p.IsFinished, project);
                Widgets.Label(rect, ref y, "- " + p.LabelCap);
            }
            var era = project.hiddenPrerequisites;
            var done = era.Count(p => p.IsFinished);
            GUI.color = Status(done == era.Count, project);
            Widgets.Label(rect, ref y, "- " + "Genesis_EraProjectsFinished".Translate(Window_EraAdvance.EraLabel(project.techLevel), done, era.Count));
            GUI.color = Status(false, project);
            foreach (var p in era.Where(p => !p.IsFinished))
            {
                Widgets.Label(rect, ref y, "    " + p.LabelCap);
            }
            GUI.color = Color.white;
            return false;
        }

        // Vanilla colours prerequisites only while the project is unfinished
        private static Color Status(bool met, ResearchProjectDef project) => project.IsFinished ? Color.white : met ? Color.green : ColorLibrary.RedReadable;
    }
}
