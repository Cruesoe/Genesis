using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Profile;

namespace Genesis
{
    [HarmonyPatch(typeof(MemoryUtility), nameof(MemoryUtility.ClearAllMapsAndWorld))]
    public static class Patch_MemoryUtility_ClearAllMapsAndWorld
    {
        public static void Postfix() => Era.RestorePristine();
    }

    // VFE Tribals: Capstones replace its advancement rituals, so their obligations aren't raised and their gizmos are hidden
    [HarmonyPatch]
    public static class Patch_VFETribals_TryRegisterAdvancementObligation
    {
        public static bool Prepare() => ModsConfig.IsActive("OskarPotocki.VFE.Tribals");

        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("VFETribals.GameComponent_Tribals"), "TryRegisterAdvancementObligation");

        public static bool Prefix() => false;
    }

    [HarmonyPatch(typeof(Precept_Ritual), nameof(Precept_Ritual.ShouldShowGizmo))]
    public static class Patch_Precept_Ritual_ShouldShowGizmo
    {
        public static bool Prepare() => ModsConfig.IsActive("OskarPotocki.VFE.Tribals");

        public static void Postfix(Precept_Ritual __instance, ref bool __result)
        {
            if (__result && __instance.def.defName.StartsWith("VFET_AdvanceTo"))
            {
                __result = false;
            }
        }
    }
}
