using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    /// <summary>
    /// This HediffComp adjusts the parent hediff’s severity based on the pawn’s Neutro Oil level.
    /// When oil is below 50%, severity increases in proportion to how low the oil level is.
    /// When oil is above 50%, severity decreases in proportion to how high the oil level is.
    /// A small dead zone around 50% prevents constant oscillation.
    /// If the gene is missing, the hediff is automatically removed.
    /// </summary>
    public class HediffComp_SeverityFromNeutroOil : HediffComp
    {
        public HediffCompProperties_SeverityFromNeutroOil Props => (HediffCompProperties_SeverityFromNeutroOil)this.props;

        // Cached reference for efficiency.
        private Gene_NeutroamineOil cachedOilGene;
        private Gene_NeutroamineOil OilGene
        {
            get
            {
                if (cachedOilGene == null && Pawn?.genes != null)
                {
                    cachedOilGene = Pawn.genes.GetFirstGeneOfType<Gene_NeutroamineOil>();
                }
                return cachedOilGene;
            }
        }

        /// <summary>
        /// If the pawn no longer has the Neutro Oil gene, this hediff should be removed.
        /// </summary>
        public override bool CompShouldRemove
        {
            get
            {
                return Pawn?.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>() == null;
            }
        }

        /// <summary>
        /// Called every tick; adjusts severity based on the current Neutro Oil level.
        /// Below 50% oil, severity increases in proportion to the oil deficiency.
        /// Above 50%, severity decreases in proportion to the surplus.
        /// </summary>
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            var oilGene = OilGene;
            if (oilGene == null)
                return;

            float oilFraction = oilGene.Value / oilGene.InitialResourceMax;

            if (oilFraction >= 0.5f)
            {
                // At 50% or more, severity decreases as oil increases.
                // "scale" progresses from 0 (when oilFraction is 0.5) to 1 (when oilFraction is 1).
                float scale = (oilFraction - 0.5f) / 0.5f;
                severityAdjustment -= (Props.severityPerHourAbove50 / 2500f) * scale;
            }
            else
            {
                // Below 50%, severity increases as oil decreases.
                // "scale" progresses from 0 (when oilFraction is 0.5) to 1 (when oilFraction is 0).
                float scale = (0.5f - oilFraction) / 0.5f;
                // If oil is completely depleted (oilFraction is 0), double the increase rate.
                float multiplier = (oilFraction <= 0f) ? 2f : 1f;
                severityAdjustment += (Props.severityPerHourBelow50 / 2500f) * scale * multiplier;
            }
        }
    }
}

