using Verse;
using Verse.Sound;

namespace WorkerDronesMod
{
    public class HediffCompProperties_SustainedSound : HediffCompProperties
    {
        // The sound def to play as a sustained loop.
        public SoundDef sustainSound;
        // Optional volume multiplier.
        public float volume = 1f;
        // The maintenance type for the sound (typically PerTick for a continuous update).
        public MaintenanceType maintenanceType = MaintenanceType.PerTick;

        public HediffCompProperties_SustainedSound()
        {
            compClass = typeof(HediffComp_SustainedSound);
        }
    }
}

