using Verse;
using VREAndroids;

namespace WorkerDronesMod
{
    public class HediffComp_HeatDamageMonitor : HediffComp
    {
        public HediffCompProperties_HeatDamageMonitor Props => (HediffCompProperties_HeatDamageMonitor)props;

        // Override the damage notification method.
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);

            Pawn pawn = parent.pawn;
            if (pawn == null)
                return;

            // Only proceed for Androids.
            if (!pawn.IsAndroid())
                return;

            // Check if the damage's armor category is "Heat".
            if (dinfo.Def.armorCategory != null && dinfo.Def.armorCategory == MD_DefOf.Heat)
            {
                // Look for the Gene_NeutroamineOil on the pawn.
                Gene_NeutroamineOil gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
                if (gene != null)
                {
                    // Retrieve the pawn's heat gene.
                    Gene_HeatBuildup heatGene = pawn.genes?.GetFirstGeneOfType<Gene_HeatBuildup>();
                    if (heatGene != null)
                    {
                        // Calculate extra heat based on total damage dealt.
                        float extraHeat = totalDamageDealt * Props.heatMultiplier;
                        heatGene.IncreaseHeat(extraHeat);
                        Log.Message($"[HediffComp_HeatDamageMonitor] {pawn.LabelShort} (Android) took {totalDamageDealt} heat damage, adding extra heat: {extraHeat}.");
                    }
                }
            }
        }
    }
}



