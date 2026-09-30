using HarmonyLib;
using Verse;

namespace Genesis
{
    [StaticConstructorOnStartup]
    public static class GenesisMod
    {
        static GenesisMod()
        {
            SupersededResearch.Init();
            Era.Init();
            new Harmony("cruesoe.genesis").PatchAll();
        }
    }
}
