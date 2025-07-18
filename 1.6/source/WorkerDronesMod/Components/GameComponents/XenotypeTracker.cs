using System.Collections.Generic;
using Verse;

namespace WorkerDronesMod
{
    public class XenotypeTracker : GameComponent
    {
        // Dictionary mapping pawn thingIDNumber to its stored xenotype string.
        public Dictionary<int, string> PawnXenotypes = new Dictionary<int, string>();

        public XenotypeTracker(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref PawnXenotypes, "PawnXenotypes", LookMode.Value, LookMode.Value);
        }
    }
}
