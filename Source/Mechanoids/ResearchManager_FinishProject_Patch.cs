#nullable disable
using HarmonyLib;
using RimWorld;
using Verse;

namespace Genesis.Mechanoids
{
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
    public static class ResearchManager_FinishProject_Patch
    {
        public static void Postfix(ResearchProjectDef proj, Pawn researcher)
        {
            var extension = proj.GetModExtension<QuestOnResearchExtension>();
            if (extension?.questDef != null)
            {
                var delayTicks = (int)(extension.delayHours * 2500);
                var target = researcher?.Map ?? Find.RandomPlayerHomeMap;
                var parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.GiveQuest, target);
                parms.forced = true;
                parms.target = target;
                parms.questScriptDef = extension.questDef;
                var qi = new QueuedIncident(new FiringIncident(DefsOf.GiveQuest_Random, null, parms), Find.TickManager.TicksGame + delayTicks);
                Find.Storyteller.incidentQueue.Add(qi);
            }
        }
    }
}
