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

                // Apply globalAbilityHeatMultiplier if sufficiently covered
                if (SolarUtil.IsSufficientlyCovered(pawn))
                {
                    var ext = basicSolverGene.ext;
                    if (ext != null)
                    {
                        heatToAdd *= ext.heatOptions.globalDefaultHeatMultiplier;
                    }
                }

                basicSolverGene.Heat = Mathf.Min(
                    basicSolverGene.Heat + heatToAdd,
                    basicSolverGene.InitialResourceMax * 1.3f
                );
            }
            else
            {
                Log.Error($"Comp_AbilityHeatEffect: Pawn {pawn.LabelShortCap} does not have a Gene_BasicSolver.");
            }
        }

    }
}




