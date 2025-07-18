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
            // Don’t cool if there’s no oil, no heat, or heat hasn’t reached the minimum threshold
            if (gene.Oil <= 0f || gene.Heat <= 0f ||
                !HeatUtil.IsAboveMinimumHeat(gene.Heat, ext.heatOptions.minimumSafeHeat))
            {
                // Reset warmup if not cooling
                oilCoolingWarmupTicks.Remove(gene);
                return;
            }

            // Also skip if in direct sun
            if (SolarUtil.IsInAnySun(pawn, ext) || SolarUtil.IsExtremeAmbientTemperature(pawn))
            {
                oilCoolingWarmupTicks.Remove(gene);
                return;
            }

            // Warmup logic
            int warmup;
            oilCoolingWarmupTicks.TryGetValue(gene, out warmup);
            if (warmup < OilCoolingWarmupDuration)
            {
                oilCoolingWarmupTicks[gene] = warmup + 1;
                return; // Still warming up, do not cool yet
            }

            // Oil cost scales slightly with ambient temperature
            float costMultiplier = 1f + Mathf.Max(0, pawn.AmbientTemperature - 21f) * 0.01f;

            // Base oil usage amount
            float baseOilUse = ext.oilOptions.oilUsePerHeatUnit * costMultiplier;

            // Check if heat is above 100%
            float heatRatio = gene.Heat / gene.InitialResourceMax;
            float efficiencyMultiplier = heatRatio > 1f ? 1.5f : 1f; // 50% more effective when above 100% heat

            // Oil used this tick
            float oilUse = Mathf.Min(gene.Oil, baseOilUse);

            gene.Oil -= oilUse;

            // Remove heat using AddHeat (negative value)
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
