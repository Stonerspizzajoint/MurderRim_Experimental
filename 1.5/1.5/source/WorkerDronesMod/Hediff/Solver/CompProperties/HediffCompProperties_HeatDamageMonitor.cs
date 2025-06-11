using Verse;

namespace WorkerDronesMod
{
    public class HediffCompProperties_HeatDamageMonitor : HediffCompProperties
    {
        public float heatMultiplier = 1.0f; // Extra heat per point of damage.

        public HediffCompProperties_HeatDamageMonitor()
        {
            this.compClass = typeof(HediffComp_HeatDamageMonitor);
        }
    }
}

