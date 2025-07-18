using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static WorkerDronesMod.SolverGeneExtension;

namespace WorkerDronesMod
{
    public static class SolverRegenerationUtil
    {
        public static void HandleHealingAndRegeneration(Pawn pawn, Gene_BasicSolver gene, RegenOptions regenOptions)
        {
            if (pawn.health.hediffSet.HasHediff(MD_DefOf.MD_DigitalLobotomy))
            {
                if (DebugSettings.godMode)
                    Log.Message($"[Gene_BasicSolver] Healing disabled: {pawn.LabelShort} is digitally lobotomized.");
                return;
            }

            // Ensure death prevention hediff is present or removed as appropriate
            if (CanApplyDeathPrevention(pawn) && !HasDeathPrevention(pawn))
            {
                pawn.health.AddHediff(MD_DefOf.MD_SolverDeathPrevention);
            }
            else if (!CanApplyDeathPrevention(pawn) && HasDeathPrevention(pawn))
            {
                var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(MD_DefOf.MD_SolverDeathPrevention);
                if (hediff != null)
                    pawn.health.RemoveHediff(hediff);
            }

            var hediffs = pawn.health.hediffSet.hediffs;

            if (hediffs.OfType<Hediff_Injury>().Any())
                HandleWoundHealing(pawn, gene, hediffs);

            bool missingLimbExists = hediffs.OfType<Hediff_MissingPart>()
                .Any(h => h.Part != null && h.Part.def != MD_DefOf.Stomach);
            bool hasReconstruction = hediffs.Any(h => h.def == MD_DefOf.MD_RoboticReconstruction);

            if (missingLimbExists || hasReconstruction)
                HandleLimbRegeneration(pawn, gene, hediffs, regenOptions);
        }

        private static void HandleWoundHealing(Pawn pawn, Gene_BasicSolver gene, List<Hediff> hediffs)
        {
            // Heal VREA_NeutroLoss if present, and add passive heat
            var neutroLoss = hediffs.FirstOrDefault(h => h.def == MD_DefOf.VREA_NeutroLoss);
            if (neutroLoss != null && neutroLoss.Severity > 0f)
            {
                float neutroHealPerTick = 0.05f; // Configurable if desired
                float neutroHeatPerHeal = 0.005f; // Configurable if desired

                float healAmount = Math.Min(neutroHealPerTick, neutroLoss.Severity);
                neutroLoss.Severity -= healAmount;

                // Add heat for the healing
                HeatUtil.AddHeat(pawn, neutroHeatPerHeal * (healAmount / neutroHealPerTick));
            }

            var injuries = hediffs.OfType<Hediff_Injury>()
                .Where(injury => injury.Severity >= 0f)
                .ToArray();

            if (injuries.Length == 0)
                return;

            var modExt = gene?.def.GetModExtension<SolverGeneExtension>();
            var regenOpts = modExt?.regenOptions;
            var heatOpts = modExt?.heatOptions;

            int initialWoundDelay = regenOpts?.woundHealingWarmupTicks ?? 60;
            int additionalWoundDelay = regenOpts?.additionalWoundDamageWarmupTicks ?? 30;
            float regenSpeedMultiplier = regenOpts?.regenSpeedMultiplier ?? 10f;

            var readyInjuries = new List<Hediff_Injury>(injuries.Length);
            float totalSeverity = 0f;
            foreach (var injury in injuries)
            {
                if (IsInjuryReadyForHealing(injury, initialWoundDelay))
                {
                    readyInjuries.Add(injury);
                    totalSeverity += injury.Severity;
                }
                else if (DebugSettings.godMode)
                {
                    Log.Message($"[Gene_BasicSolver] Injury on {pawn.LabelShort} is still in delay period.");
                }
            }

            if (readyInjuries.Count == 0)
                return;

            float healingFactor = ExtraSolverUtils.IsACoreHeart(pawn) ? 0.25f : 1f;
            float effectiveRegen = regenSpeedMultiplier * healingFactor;

            if (HasHealingAffectingHediff(pawn))
            {
                effectiveRegen *= 0.5f;
                if (DebugSettings.godMode)
                    Log.Message($"[Gene_BasicSolver] Effective regen reduced due to overheating for {pawn.LabelShort}. New regen: {effectiveRegen}");
            }

            if (effectiveRegen > 0.0001f)
            {
                RegenerationUtilities.RegenerateWounds(pawn, effectiveRegen, readyInjuries);

                float heatPerSeverity = heatOpts?.heatPerSeverity ?? 0.1f;
                float ambientMultiplier = HeatUtil.HeatAmbientMultiplier(pawn, pawn.AmbientTemperature);
                HeatUtil.AddHeat(pawn, totalSeverity * heatPerSeverity * ambientMultiplier);
            }
        }

        private static void HandleLimbRegeneration(Pawn pawn, Gene_BasicSolver gene, List<Hediff> hediffs, RegenOptions regenOptions)
        {
            try
            {
                RoboticLimbRegenerator.ProcessRegeneration(pawn, regenOptions);
                float currentHeat = RoboticLimbRegenerator.CalculateHeatForPawn(pawn, regenOptions);
                float ambientMultiplier = HeatUtil.HeatAmbientMultiplier(pawn, pawn.AmbientTemperature);
                HeatUtil.AddHeat(pawn, currentHeat * ambientMultiplier);
            }
            catch (Exception ex)
            {
                Log.Error($"Limb regeneration error: {ex}");
            }
        }

        // --- Replacements for SolverGeneUtility ---

        // Checks if an injury is old enough for healing (simple version)
        private static bool IsInjuryReadyForHealing(Hediff_Injury injury, int initialDelay)
        {
            return injury != null && injury.ageTicks >= initialDelay;
        }

        // Checks for a hediff that affects healing (e.g., overheating)
        private static bool HasHealingAffectingHediff(Pawn pawn)
        {
            return pawn.health.hediffSet.HasHediff(MD_DefOf.VREA_Overheating);
        }

        // Checks if the pawn has the death prevention hediff
        public static bool HasDeathPrevention(Pawn pawn)
        {
            return pawn != null && pawn.health.hediffSet.HasHediff(MD_DefOf.MD_SolverDeathPrevention);
        }

        public static bool CanApplyDeathPrevention(Pawn pawn)
        {
            // Only allow if pawn is not null, not dead, not downed, and not overheating
            return pawn != null
                && !pawn.Dead
                && !pawn.Downed
                && !HeatUtil.IsOverheating(
                    pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>()?.Heat ?? 0f,
                    pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>()?.InitialResourceMax ?? 1f
                );
        }
    }
}
