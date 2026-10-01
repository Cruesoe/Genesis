using HarmonyLib;
using UnityEngine;
using Verse;

namespace Genesis
{
    // The mod: settings and every Harmony patch in the assembly, applied once when mods are constructed
    public class GenesisMod : Mod
    {
        public static GenesisSettings settings = null!;

        public GenesisMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<GenesisSettings>();
            new Harmony("cruesoe.genesis").PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => Content.Name;
    }

    // Builds the era tree once defs are loaded
    [StaticConstructorOnStartup]
    public static class GenesisStartup
    {
        static GenesisStartup()
        {
            SupersededResearch.Init();
            EraTree.Build();
            Era.Init();
        }
    }
}
