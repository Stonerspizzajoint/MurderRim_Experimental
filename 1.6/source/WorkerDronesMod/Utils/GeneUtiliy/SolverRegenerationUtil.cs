using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using static WorkerDronesMod.SolverGeneExtension;

namespace WorkerDronesMod
{
    public static class SolverRegenerationUtil
    {
        // Static temp lists to avoid allocations
        private static readonly List<Hediff_Injury> tempInjuries = new List<Hediff_Injury>();
        private static readonly List<Hediff_Injury> tempReadyInjuries = new List<Hediff_Injury>();
        private static readonly List<Hediff_MissingPart> tempMissingParts = new List<Hediff_MissingPart>();

        public static void HandleHealingAndRegeneration(Pawn pawn, Gene_BasicSolver gene, RegenOptions regenOptions)
        {
            if (pawn.health.hediffSet.HasHediff(MD_DefOf.MD_DigitalLobotomy))
            {
                if (DebugSettings.godMode)
                    Log.Message($"[Gene_BasicSolver] Healing disabled: {pawn.LabelShort} is digitally lobotomized.");
                return;
            }

            // --- Death prevention logic ---
            bool hasDeathPrevention = HasDeathPrevention(pawn);
            bool brainSafe = IsBrainSafe(pawn);

            if (!brainSafe)
            {
                if (!hasDeathPrevention)
                    pawn.health.AddHediff(MD_DefOf.MD_SolverDeathPrevention);
            }
            else if (CanPreventDeathPrevention(pawn))
            {
                if (hasDeathPrevention)
                {
                    var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(MD_DefOf.MD_SolverDeathPrevention);
                    if (hediff != null)
                        pawn.health.RemoveHediff(hediff);
                }
            }
            else
            {
                if (!hasDeathPrevention)
                    pawn.health.AddHediff(MD_DefOf.MD_SolverDeathPrevention);
            }

            var hediffs = pawn.health.hediffSet.hediffs;

            // --- Injuries ---
            tempInjuries.Clear();
            foreach (var h in hediffs)
            {
                if (h is Hediff_Injury injury && injury.Severity >= 0f)
                    tempInjuries.Add(injury);
            }
            if (tempInjuries.Count > 0)
                HandleWoundHealing(pawn, gene, hediffs, tempInjuries);

            // --- Missing limbs ---
            tempMissingParts.Clear();
            bool missingLimbExists = false;
            foreach (var h in hediffs)
            {
                if (h is Hediff_MissingPart mp && mp.Part != null && mp.Part.def != MD_DefOf.Stomach)
                {
                    tempMissingParts.Add(mp);
                    missingLimbExists = true;
                }
            }
            bool hasReconstruction = false;
            foreach (var h in hediffs)
            {
                if (h.def == MD_DefOf.MD_RoboticReconstruction)
                {
                    hasReconstruction = true;
                    break;
                }
            }
            if (missingLimbExists || hasReconstruction)
                HandleLimbRegeneration(pawn, gene, hediffs, regenOptions);
        }

        private static void HandleWoundHealing(Pawn pawn, Gene_BasicSolver gene, List<Hediff> hediffs, List<Hediff_Injury> injuries)
        {
            // Heal VREA_NeutroLoss if present, and add passive heat
            Hediff neutroLoss = null;
            foreach (var h in hediffs)
            {
                if (h.def == MD_DefOf.VREA_NeutroLoss)
                {
                    neutroLoss = h;
                    break;
                }
            }
            if (neutroLoss != null && neutroLoss.Severity > 0f)
            {
                float neutroHealPerTick = 0.05f;
                float neutroHeatPerHeal = 0.005f;
                float healAmount = Math.Min(neutroHealPerTick, neutroLoss.Severity);
                neutroLoss.Severity -= healAmount;
                HeatUtil.AddHeat(pawn, neutroHeatPerHeal * (healAmount / neutroHealPerTick));
            }

            if (injuries.Count == 0)
                return;

            var modExt = gene?.def.GetModExtension<SolverGeneExtension>();
            var regenOpts = modExt?.regenOptions;
            var heatOpts = modExt?.heatOptions;

            int initialWoundDelay = regenOpts?.woundHealingWarmupTicks ?? 60;
            float regenSpeedMultiplier = pawn.GetStatValue(MD_DefOf.MD_RegenSpeedMultiplier, true);

            tempReadyInjuries.Clear();
            float totalSeverity = 0f;
            foreach (var injury in injuries)
            {
                if (IsInjuryReadyForHealing(injury, initialWoundDelay))
                {
                    tempReadyInjuries.Add(injury);
                    totalSeverity += injury.Severity;
                }
                else if (DebugSettings.godMode)
                {
                    Log.Message($"[Gene_BasicSolver] Injury on {pawn.LabelShort} is still in delay period.");
                }
            }

            if (tempReadyInjuries.Count == 0)
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
                RegenerationUtilities.RegenerateWounds(pawn, effectiveRegen, tempReadyInjuries);

                float heatPerSeverity = pawn.GetStatValue(MD_DefOf.MD_HeatPerSeverity, true);
                float ambientMultiplier = HeatUtil.HeatAmbientMultiplier(pawn, pawn.AmbientTemperature);
                HeatUtil.AddHeat(
                    pawn,
                    totalSeverity * heatPerSeverity * ambientMultiplier * regenSpeedMultiplier
                );
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

        private static bool IsInjuryReadyForHealing(Hediff_Injury injury, int initialDelay)
        {
            return injury != null && injury.ageTicks >= initialDelay;
        }

        private static bool HasHealingAffectingHediff(Pawn pawn)
        {
            return pawn.health.hediffSet.HasHediff(MD_DefOf.VREA_Overheating);
        }

        public static bool HasDeathPrevention(Pawn pawn)
        {
            return pawn != null && pawn.health.hediffSet.HasHediff(MD_DefOf.MD_SolverDeathPrevention);
        }

        public static bool CanDeathBePrevented(Pawn pawn, SolverGeneExtension ext)
        {
            if (SolarUtil.InTrueSunlight(pawn))
                return false;
            return true;
        }

        public static bool IsRebooting(Pawn pawn)
        {
            return pawn != null
                && pawn.InMentalState
                && pawn.MentalStateDef == MD_DefOf.MD_RecoverAndBootUp;
        }

        public static bool CanPreventDeathPrevention(Pawn pawn)
        {
            return pawn != null
                && (IsRebooting(pawn) || IsReactorMissing(pawn) || SolarUtil.InTrueSunlight(pawn));
        }

        public static bool IsBrainSafe(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
                return false;

            var brain = pawn.health?.hediffSet?.GetBrain();
            if (brain == null)
                return false;

            if (pawn.health.hediffSet.PartIsMissing(brain))
                return false;

            return true;
        }

        public static bool IsReactorMissing(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
                return true;

            var stomach = pawn.health?.hediffSet?.GetNotMissingParts();
            BodyPartRecord foundStomach = null;
            if (stomach != null)
            {
                foreach (var part in stomach)
                {
                    if (part.def == MD_DefOf.Stomach)
                    {
                        foundStomach = part;
                        break;
                    }
                }
            }
            if (foundStomach == null || pawn.health.hediffSet.PartIsMissing(foundStomach))
                return true;

            return false;
        }

        public static void TryBootUpIfBrainMissing(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null || pawn.Dead)
                return;

            if (pawn.health.hediffSet.GetBrain() == null)
            {
                if (pawn.mindState?.mentalStateHandler?.CurStateDef != MD_DefOf.MD_RecoverAndBootUp)
                {
                    pawn.mindState.mentalStateHandler.TryStartMentalState(
                        MD_DefOf.MD_RecoverAndBootUp,
                        reason: "Brain missing, booting up.",
                        forceWake: true
                    );
                }
            }
        }
    }
}
