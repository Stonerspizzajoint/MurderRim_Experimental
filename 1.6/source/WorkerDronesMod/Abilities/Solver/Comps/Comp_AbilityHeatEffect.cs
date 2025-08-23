using RimWorld;
using Verse;
using UnityEngine;


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

            var basicSolverGene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (basicSolverGene != null)
            {
                float heatToAdd = Props.heatRange.RandomInRange;
                float heatGainMultiplier = pawn.GetStatValue(MD_DefOf.MD_HeatGainMultiplier, true);
                float abilityHeatGainMultiplier = pawn.GetStatValue(MD_DefOf.MD_AbilityHeatGainMultiplier, true);
                heatToAdd *= heatGainMultiplier * abilityHeatGainMultiplier;


                basicSolverGene.Heat = Mathf.Min(
                    basicSolverGene.Heat + heatToAdd,
                    basicSolverGene.InitialResourceMax * 1.3f
                );

                // Always use corruptionRange for corruption gain
                float corruptionToAdd = Props.corruptionRange.RandomInRange;
                float corruptionGainMultiplier = pawn.GetStatValue(MD_DefOf.MD_AbilityCorruptionGainMultiplier, true);
                corruptionToAdd *= corruptionGainMultiplier;
                SolverCorruptionUtil.OnSolverAbilityUsed(pawn, corruptionToAdd);
            }
            else
            {
                Log.Error($"Comp_AbilityHeatEffect: Pawn {pawn.LabelShortCap} does not have a Gene_BasicSolver.");
            }
        }
    }
}




