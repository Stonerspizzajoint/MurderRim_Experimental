using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    // TRACKER 2: Stores which pawn built a specific door frame.
    public static class DoorBuilderTracker
    {
        public static Dictionary<Frame, Pawn> BuilderByFrame = new Dictionary<Frame, Pawn>();
    }
}
