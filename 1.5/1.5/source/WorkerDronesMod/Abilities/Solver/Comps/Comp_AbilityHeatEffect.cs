using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class Comp_AbilityHeatEffect : CompAbilityEffect
    {
        public CompProperties_AbilityHeatEffect Props => (CompProperties_AbilityHeatEffect)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = parent.pawn;
            if (pawn == null)
            {
                Log.Error("Comp_AbilityHeatEffect: Pawn is null.");
                return;
            }

            Gene_HeatBuildup heatGene = pawn.genes?.GetFirstGeneOfType<Gene_HeatBuildup>();
            if (heatGene != null)
            {
                float heatToAdd = Props.heatRange.RandomInRange;
                heatGene.IncreaseHeat(heatToAdd);
            }
            else
            {
                Log.Error($"Comp_AbilityHeatEffect: Pawn {pawn.LabelShortCap} does not have a heat gene.");
            }
        }
    }
}




