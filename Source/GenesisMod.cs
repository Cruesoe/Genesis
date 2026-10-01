using HarmonyLib;
using Verse;

namespace Genesis
{
    [StaticConstructorOnStartup]
    public static class GenesisMod
    {
        static GenesisMod()
        {
            ResearchRemap.Apply();
            SupersededResearch.Init();
            EraTree.Build();
            Era.Init();
            new Harmony("cruesoe.genesis").PatchAll();
        }
    }
}
