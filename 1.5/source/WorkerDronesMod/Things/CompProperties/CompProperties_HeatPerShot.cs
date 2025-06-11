using Verse;

namespace WorkerDronesMod
{
    public class CompProperties_HeatPerShot : CompProperties
    {
        public float heatPerShot = 1f;

        public CompProperties_HeatPerShot()
        {
            compClass = typeof(CompHeatPerShot);
        }
    }
}
