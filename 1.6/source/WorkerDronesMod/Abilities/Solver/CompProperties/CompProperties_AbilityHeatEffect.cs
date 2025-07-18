using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class CompProperties_AbilityHeatEffect : CompProperties_AbilityEffect
    {
        // A float range specifying the minimum and maximum heat to add.
        public FloatRange heatRange = new FloatRange(5f, 10f);

        public CompProperties_AbilityHeatEffect()
        {
            compClass = typeof(Comp_AbilityHeatEffect);
        }
    }
}


