using Verse;

namespace WorkerDronesMod
{
    // Rename the properties class to match our new theme.
    public class HediffCompProperties_DigitalAmnesiaTending : HediffCompProperties_TendDuration
    {
        // Factor by which the tend quality scales extra severity reduction.
        public float qualityMultiplier = 0.2f;

        public HediffCompProperties_DigitalAmnesiaTending()
        {
            compClass = typeof(HediffComp_DigitalAmnesiaTending);
        }
    }
}

