using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Genesis
{
    // A research branch: one lane on every era tab, ordered top to bottom by order. Lists projects by defName, so inactive mods' names are ignored.
    public class BranchDef : Def
    {
        public int order;
        public List<string> projects = new List<string>();
        public List<string> skipIfModActive = new List<string>();

        public bool Active => !skipIfModActive.Any(ModsConfig.IsActive);
    }
}
