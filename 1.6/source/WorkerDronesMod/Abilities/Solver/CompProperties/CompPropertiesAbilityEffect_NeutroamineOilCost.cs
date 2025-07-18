using RimWorld;

namespace WorkerDronesMod
{
    // Properties for the NeutroamineOil cost component.
    public class CompPropertiesAbilityEffect_NeutroamineOilCost : CompProperties_AbilityEffect
    {
        // The cost (amount of NeutroamineOil) that the ability requires.
        public float neutroamineOilCost = 0f;

        public CompPropertiesAbilityEffect_NeutroamineOilCost()
        {
            // Associate this properties class with its corresponding component.
            this.compClass = typeof(CompAbilityEffect_NeutroamineOilCost);
        }
    }
}

