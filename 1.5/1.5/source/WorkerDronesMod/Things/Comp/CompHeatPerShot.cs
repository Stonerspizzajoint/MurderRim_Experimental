using Verse;

namespace WorkerDronesMod
{
    public class CompHeatPerShot : ThingComp
    {
        public CompProperties_HeatPerShot Props => (CompProperties_HeatPerShot)props;
        private Gene_HeatBuildup linkedHeatGene;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        // Existing method for immediate heat addition (if used elsewhere).
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

            if (linkedHeatGene == null)
            {
                linkedHeatGene = shooter.genes?.GetFirstGeneOfType<Gene_HeatBuildup>();
                if (linkedHeatGene == null)
                {
                    Log.Warning($"[CompHeatPerShot] Pawn {shooter.LabelShort} does not have a Gene_HeatBuildup.");
                    return;
                }
            }

            float before = linkedHeatGene.Value;
            linkedHeatGene.IncreaseHeat(Props.heatPerShot);
            float after = linkedHeatGene.Value;
        }

        // New method: add heat gradually.
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

            if (linkedHeatGene == null)
            {
                linkedHeatGene = shooter.genes?.GetFirstGeneOfType<Gene_HeatBuildup>();
                if (linkedHeatGene == null)
                {
                    Log.Warning($"[CompHeatPerShot] Pawn {shooter.LabelShort} does not have a Gene_HeatBuildup.");
                    return;
                }
            }

            float before = linkedHeatGene.Value;
            linkedHeatGene.IncreaseHeat(heatIncrement);
            float after = linkedHeatGene.Value;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref linkedHeatGene, "linkedHeatGene");
        }
    }
}

