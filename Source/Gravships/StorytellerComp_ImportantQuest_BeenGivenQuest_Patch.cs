#nullable disable
using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;
using System.Reflection;
using System;

namespace Genesis.Gravships
{
    [HarmonyPatch(typeof(StorytellerComp_ImportantQuest), nameof(StorytellerComp_ImportantQuest.BeenGivenQuest), MethodType.Getter)]
    public static class StorytellerComp_ImportantQuest_BeenGivenQuest_Patch
    {
        public static void Postfix(StorytellerComp_ImportantQuest __instance, ref bool __result)
        {
            if (__instance.Props.questDef == QuestScriptDefOf.MechanoidSignal && Faction.OfPlayer.def.techLevel < TechLevel.Spacer)
            {
                __result = true;
            }
        }
    }


    [HarmonyPatch]
    public static class VanillaGravshipExpanded_StorytellerComp_ImportantQuestAfterResearch_BeenGivenQuest_Patch
    {
        private static MethodBase targetMethod;

        public static bool Prepare()
        {
            if (!ModsConfig.IsActive("vanillaexpanded.gravship"))
            {
                return false;
            }
            targetMethod = TargetMethod();
            return targetMethod != null;
        }

        public static MethodBase TargetMethod()
        {
            if (targetMethod == null)
            {
                var targetType = AccessTools.TypeByName("VanillaGravshipExpanded.StorytellerComp_ImportantQuestAfterResearch");
                if (targetType != null)
                {
                    var property = AccessTools.Property(targetType, "BeenGivenQuest");
                    if (property != null)
                    {
                        targetMethod = property.GetGetMethod(true);
                    }
                    else
                    {
                        Log.Error("[Genesis] Could not find property 'BeenGivenQuest' in type 'VanillaGravshipExpanded.StorytellerComp_ImportantQuestAfterResearch' for patching.");
                    }
                }
                else
                {
                    Log.Error("[Genesis] Could not find type 'VanillaGravshipExpanded.StorytellerComp_ImportantQuestAfterResearch' for patching.");
                }
            }
            return targetMethod;
        }

        public static void Postfix(ref bool __result)
        {
            if (Faction.OfPlayer.def.techLevel < TechLevel.Spacer)
            {
                __result = true;
            }
        }
    }
}
