#nullable disable
using RimWorld;

namespace Genesis.Mechanoids
{
    [DefOf]
    public static class DefsOf
    {
        public static IncidentDef GiveQuest_Random;

        static DefsOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefsOf));
        }
    }
}
