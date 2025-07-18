using Verse;

namespace WorkerDronesMod
{
    // Define the properties for our heat restriction comp.
    public class CompProperties_HeatRestriction : CompProperties
    {
        // The maximum allowed heat value (as a fraction, e.g., 0.5 = 50% of maximum heat).
        public float maxAllowedHeat = 0.5f;

        public CompProperties_HeatRestriction()
        {
            // Set the comp class that uses these properties.
            this.compClass = typeof(ThingComp_HeatRestriction);
        }
    }
}
