using Verse;

namespace Genesis
{
    // Hides a building from the architect menu (god mode still shows it); the def, existing buildings and saves are untouched
    public class PlaceWorker_NotBuildable : PlaceWorker
    {
        public override bool IsBuildDesignatorVisible(BuildableDef def) => false;
    }
}
