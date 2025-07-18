using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WorkerDronesMod
{
    public class Hediff_NeutroOilCraving : HediffWithComps
    {
        public override string SeverityLabel
        {
            get
            {
                if (this.Severity == 0f)
                {
                    return null;
                }
                return this.Severity.ToStringPercent();
            }
        }

        public override void Tick()
        {
            base.Tick();

            // Run every 10 ticks to slow down severity gain
            if (this.pawn.IsHashIntervalTick(10))
            {
                var gene = this.pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
                if (gene == null || gene.InitialResourceMax <= 0f)
                    return;

                float oilPercent = gene.Oil / gene.InitialResourceMax;

                // Get oilCravingSpeed from SolverGeneExtension, default to 1.0f if not found
                float oilCravingSpeed = 1.0f;
                var geneExt = this.pawn.def.GetModExtension<SolverGeneExtension>();
                if (geneExt != null)
                {
                    oilCravingSpeed = geneExt.oilOptions.oilCravingSpeed;
                }

                // Lowered rates for slower severity gain
                float baseRate = 0.0001f * oilCravingSpeed;
                float zeroOilRate = 0.001f * oilCravingSpeed;
                float decayRate = 0.0002f * oilCravingSpeed;

                if (oilPercent < 0.2f)
                {
                    float severityPerTick;
                    if (OilUtil.HasNoOil(gene))
                    {
                        severityPerTick = zeroOilRate;
                    } 
                    else
                    {
                        // Linear interpolation: faster as oil approaches 0
                        severityPerTick = baseRate + (zeroOilRate - baseRate) * (1f - (oilPercent / 0.2f));
                    }

                    this.Severity += severityPerTick;
                }
                else
                {
                    // Decrease severity per tick if oil is above 20%
                    this.Severity -= decayRate;
                    if (this.Severity <= 0f)
                    {
                        this.pawn.health.RemoveHediff(this);
                    }
                }
            }
        }
    }
}
