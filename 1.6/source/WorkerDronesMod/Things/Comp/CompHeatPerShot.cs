using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    public class CompHeatPerShot : ThingComp
    {
        public CompProperties_HeatPerShot Props => (CompProperties_HeatPerShot)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public void AddHeatOnShot()
        {
            Pawn shooter = ParentHolder as Pawn;
            if (shooter == null && parent is ThingWithComps thing)
            {
                CompEquippable equippable = thing.TryGetComp<CompEquippable>();
                shooter = equippable?.PrimaryVerb?.caster as Pawn;
            }

            if (shooter == null)
            {
                Log.Warning("[CompHeatPerShot] Shooter pawn is null.");
                return;
            }

            var basicSolverGene = shooter.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (basicSolverGene == null)
            {
                Log.Warning($"[CompHeatPerShot] Pawn {shooter.LabelShort} does not have a Gene_BasicSolver.");
                return;
            }

            float heatToAdd = Props.heatPerShot;
            var ext = basicSolverGene.ext;

            // Always use HeatUtil.AddHeat for consistency
            HeatUtil.AddHeat(shooter, heatToAdd, ext);
        }

        public void AddHeatOnShotGradual(float heatIncrement)
        {
            Pawn shooter = ParentHolder as Pawn;
            if (shooter == null && parent is ThingWithComps thing)
            {
                CompEquippable equippable = thing.TryGetComp<CompEquippable>();
                shooter = equippable?.PrimaryVerb?.caster as Pawn;
            }

            if (shooter == null)
            {
                Log.Warning("[CompHeatPerShot] Shooter pawn is null.");
                return;
            }

            var basicSolverGene = shooter.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (basicSolverGene == null)
            {
                Log.Warning($"[CompHeatPerShot] Pawn {shooter.LabelShort} does not have a Gene_BasicSolver.");
                return;
            }

            float heatToAdd = heatIncrement;
            var ext = basicSolverGene.ext;

            // Always use HeatUtil.AddHeat for consistency
            HeatUtil.AddHeat(shooter, heatToAdd, ext);
        }
    }
}

