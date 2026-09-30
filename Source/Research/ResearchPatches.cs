using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Genesis
{
    [HarmonyPatch(typeof(ResearchProjectDef), nameof(ResearchProjectDef.IsHidden), MethodType.Getter)]
    public static class Patch_ResearchProjectDef_IsHidden
    {
        public static void Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            if (!__result && SupersededResearch.IsSuperseded(__instance) && !__instance.IsFinished)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
    public static class Patch_ResearchManager_FinishProject
    {
        public static void Postfix(ResearchProjectDef proj)
        {
            SupersededResearch.FinishSupersededBy(proj);
            Era.Notify_ProjectFinished(proj);
        }
    }

    // Drops superseded projects from the research screen's (cached) project list
    [HarmonyPatch(typeof(MainTabWindow_Research), nameof(MainTabWindow_Research.VisibleResearchProjects), MethodType.Getter)]
    public static class Patch_MainTabWindow_Research_VisibleResearchProjects
    {
        private static List<ResearchProjectDef>? cleaned;

        public static void Postfix(List<ResearchProjectDef> __result)
        {
            if (!ReferenceEquals(__result, cleaned))
            {
                __result.RemoveAll(SupersededResearch.IsSuperseded);
                cleaned = __result;
            }
        }
    }

    // Removes tabs with no visible projects, puts the Genesis era tabs first, and moves off a removed current tab
    [HarmonyPatch(typeof(MainTabWindow_Research), nameof(MainTabWindow_Research.PostOpen))]
    public static class Patch_MainTabWindow_Research_PostOpen
    {
        private static readonly FieldInfo TabsField = AccessTools.Field(typeof(MainTabWindow_Research), "tabs");
        private static FieldInfo? recordDefField;

        public static void Postfix(MainTabWindow_Research __instance)
        {
            var tabs = (IList)TabsField.GetValue(__instance);
            var used = new HashSet<ResearchTabDef>(__instance.VisibleResearchProjects.Select(p => p.tab));
            var kept = new List<ResearchTabDef>();
            for (var i = tabs.Count - 1; i >= 0; i--)
            {
                recordDefField ??= AccessTools.Field(tabs[i].GetType(), "def");
                var def = (ResearchTabDef)recordDefField.GetValue(tabs[i]);
                if (used.Contains(def))
                {
                    kept.Add(def);
                }
                else
                {
                    tabs.RemoveAt(i);
                }
            }
            var ordered = tabs.Cast<object>().OrderBy(r => ((ResearchTabDef)recordDefField!.GetValue(r)).defName.StartsWith("Genesis_") ? 0 : 1).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                tabs[i] = ordered[i];
            }
            if (kept.Count > 0 && !kept.Contains(__instance.CurTab))
            {
                kept.Reverse();
                __instance.CurTab = __instance.VisibleResearchProjects.FirstOrDefault(p => p.CanStartNow && kept.Contains(p.tab))?.tab ?? kept[0];
            }
        }
    }
}
