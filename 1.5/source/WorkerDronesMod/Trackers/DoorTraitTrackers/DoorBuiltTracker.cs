using System.Collections.Generic;
using Verse;

namespace WorkerDronesMod
{
    // Tracks the last tick when a pawn gained the door-built memory.
    // TRACKER 1: Stores the last tick when a pawn received the door-built memory.
    public static class DoorBuiltTracker
    {
        public static Dictionary<Pawn, int> LastDoorBuiltTick = new Dictionary<Pawn, int>();
    }
}
