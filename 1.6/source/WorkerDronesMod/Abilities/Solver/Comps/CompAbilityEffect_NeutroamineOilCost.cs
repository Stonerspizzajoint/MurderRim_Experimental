using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    // This component applies the NeutroamineOil cost for an ability.
    public class CompAbilityEffect_NeutroamineOilCost : CompAbilityEffect
    {
        // Provides strongly typed access to the properties.
        public new CompPropertiesAbilityEffect_NeutroamineOilCost Props
        {
            get { return (CompPropertiesAbilityEffect_NeutroamineOilCost)this.props; }
        }

        // Override Apply to implement any custom logic when the ability is used.
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            // Implement any additional logic here (e.g., deduct NeutroamineOil from the pawn).
        }
    }
}

