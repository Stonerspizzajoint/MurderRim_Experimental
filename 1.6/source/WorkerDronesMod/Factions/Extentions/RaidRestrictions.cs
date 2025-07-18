using Verse;

namespace WorkerDronesMod
{
    public class RaidRestrictions : DefModExtension
    {
        // If true, raids from this faction can only occur at nighttime.
        public bool onlyNighttime = false;

        // Maximum allowed skyglow for a raid to occur.
        // (Skyglow values range from 0 (dark) to 1 (bright).)
        public float maxSkyGlow = 1f;

    }
}

