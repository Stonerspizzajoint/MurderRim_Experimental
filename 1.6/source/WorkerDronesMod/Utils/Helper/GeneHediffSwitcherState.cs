using Verse;

namespace WorkerDronesMod
{
    /// <summary>
    /// Represents a mapping from a state (e.g. "Drafted", "HighHeat", etc.) to a HediffDef.
    /// </summary>
    public class GeneHediffSwitcherState
    {
        public HediffDef hediffDef;
        public GeneHediffSwitcherUtility.PawnState state;

        // New field: if true, this mapping overrides any others when active.
        public bool overrideMapping = false;

        // Optionally, you could add an integer priority instead:
        // public int priority = 0;
    }
}
