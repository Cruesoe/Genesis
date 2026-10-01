#nullable disable
using HarmonyLib;
using Verse.Profile;

namespace Genesis.Basics
{
    [HarmonyPatch(typeof(MemoryUtility), "ClearAllMapsAndWorld")]
    public static class MemoryUtility_ClearAllMapsAndWorld_Patch
    {
        private static void Prefix()
        {
            PlayerFactionTechLevelCache.RestoreAll();
        }
    }
}
