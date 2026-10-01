#nullable disable
using HarmonyLib;
using RimWorld;

namespace Genesis.Basics
{
    [HarmonyPatch(typeof(ScenPart_PlayerFaction), "PostWorldGenerate")]
    public static class ScenPart_PlayerFaction_PostWorldGenerate_Patch
    {
        private static void Prefix()
        {
            PlayerFactionTechLevelCache.RestoreAll();
        }
    }
}
