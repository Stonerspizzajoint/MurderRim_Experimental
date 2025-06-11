using Verse;

namespace WorkerDronesMod
{
    // The properties class that holds configuration for the HediffComp.
    public class HediffCompProperties_ForceDropWeapon : HediffCompProperties
    {
        // You can change this interval via XML if desired.
        public int checkIntervalTicks = 60; // Default: check every 60 ticks.

        public HediffCompProperties_ForceDropWeapon()
        {
            // IMPORTANT: It sets the compClass so that RimWorld knows which HediffComp to instantiate.
            this.compClass = typeof(HediffComp_ForceDropWeapon);
        }
    }

}
