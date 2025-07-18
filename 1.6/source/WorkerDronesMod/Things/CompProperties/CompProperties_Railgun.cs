using Verse;

namespace WorkerDronesMod
{
    public class CompProperties_Railgun : CompProperties
    {
        // how many ticks of cooldown; if zero, fall back to verbProps.AdjustedCooldownTicks
        public int customCooldownTicks = 0;

        // optional sound to play when the railgun finishes its burst
        public SoundDef finishedSound;

        public CompProperties_Railgun()
        {
            this.compClass = typeof(CompRailgun);
        }
    }
}

