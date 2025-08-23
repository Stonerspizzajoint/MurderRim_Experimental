using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class CompProperties_AbilityHeatEffect : CompProperties_AbilityEffect
    {
        // A float range specifying the minimum and maximum heat to add.
        public FloatRange heatRange = new FloatRange(5f, 10f);

        // Amount of corruption to add when this ability is used (default 0)
        public FloatRange corruptionRange = new FloatRange(0f, 0f);

        public CompProperties_AbilityHeatEffect()
        {
            compClass = typeof(Comp_AbilityHeatEffect);
        }
    }
}


