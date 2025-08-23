using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    public static class OilUtil
    {

        private static readonly Dictionary<Gene_BasicSolver, int> oilCoolingWarmupTicks = new Dictionary<Gene_BasicSolver, int>();
        private const int OilCoolingWarmupDuration = 60; // e.g., 60 ticks = 1 second

        public static void HandleOilCooling(Gene_BasicSolver gene, Pawn pawn, SolverGeneExtension ext)
        {
            if (gene.Oil <= 0f || gene.Heat <= 0f ||
                !HeatUtil.IsAboveMinimumHeat(gene.Heat, ext.heatOptions.minimumSafeHeat))
            {
                if (oilCoolingWarmupTicks.ContainsKey(gene))
                    oilCoolingWarmupTicks.Remove(gene);
                return;
            }

            if (SolarUtil.IsInAnySun(pawn) || SolarUtil.IsExtremeAmbientTemperature(pawn))
            {
                if (oilCoolingWarmupTicks.ContainsKey(gene))
                    oilCoolingWarmupTicks.Remove(gene);
                return;
            }

            // Warmup logic
            if (!oilCoolingWarmupTicks.TryGetValue(gene, out int warmup))
                warmup = 0;

            if (warmup < OilCoolingWarmupDuration)
            {
                oilCoolingWarmupTicks[gene] = warmup + 1;
                return;
            }

            float costMultiplier = 1f + Mathf.Max(0, pawn.AmbientTemperature - 21f) * 0.01f;
            float baseOilUse = ext.oilOptions.oilUsePerHeatUnit * costMultiplier;
            float heatRatio = gene.Heat / gene.InitialResourceMax;
            float efficiencyMultiplier = heatRatio > 1f ? 1.5f : 1f;
            float oilUse = Mathf.Min(gene.Oil, baseOilUse);

            gene.Oil -= oilUse;
            HeatUtil.AddHeat(pawn, -oilUse * ext.oilOptions.heatPerOil * efficiencyMultiplier, ext);
        }

        public static void HandleOilLossHediff(Gene_BasicSolver gene, Pawn pawn)
        {
            if (gene == null || pawn == null || pawn.Dead)
                return;

            float oilPercent = gene.Oil / gene.InitialResourceMax;

            if (oilPercent < 0.2f)
            {
                // Only add the hediff if not already present
                if (pawn.health.hediffSet.GetFirstHediffOfDef(MD_DefOf.MD_OilLoss) == null)
                {
                    Hediff hediff = HediffMaker.MakeHediff(MD_DefOf.MD_OilLoss, pawn);
                    pawn.health.AddHediff(hediff);
                }
            }
        }

        public static bool HasNoOil(Gene_BasicSolver gene)
        {
            return gene == null || gene.Oil <= 0f;
        }

    }
}
