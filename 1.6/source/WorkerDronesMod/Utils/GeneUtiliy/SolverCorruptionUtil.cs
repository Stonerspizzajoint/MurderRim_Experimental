using RimWorld;
using Verse;
using UnityEngine;

namespace WorkerDronesMod
{
    public static class SolverCorruptionUtil
    {
        /// <summary>
        /// Get the Gene_BasicSolver instance for a pawn, if present and not nerfed.
        /// </summary>
        public static Gene_BasicSolver GetActiveNonNerfedSolverGene(Pawn pawn)
        {
            if (pawn?.genes == null) return null;
            var gene = pawn.genes.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene != null && gene.ext != null && !gene.ext.isNerfedSolver)
                return gene;
            return null;
        }

        /// <summary>
        /// Get the current corruption (0-1) for a pawn, or 0 if not applicable.
        /// </summary>
        public static float GetCorruption(Pawn pawn)
        {
            var gene = GetActiveNonNerfedSolverGene(pawn);
            return gene?.Corruption ?? 0f;
        }

        /// <summary>
        /// Set the corruption (clamped 0-1) for a pawn, if applicable.
        /// </summary>
        public static void SetCorruption(Pawn pawn, float value)
        {
            var gene = GetActiveNonNerfedSolverGene(pawn);
            if (gene != null)
                gene.Corruption = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Add (or subtract) corruption by amount (positive or negative).
        /// </summary>
        public static void AddCorruption(Pawn pawn, float amount)
        {
            var gene = GetActiveNonNerfedSolverGene(pawn);
            if (gene != null)
            {
                float before = gene.Corruption;
                float after = Mathf.Clamp01(gene.Corruption + amount);
                gene.Corruption = after;
            }
        }

        /// <summary>
        /// Remove corruption (set to zero).
        /// </summary>
        public static void ClearCorruption(Pawn pawn)
        {
            var gene = GetActiveNonNerfedSolverGene(pawn);
            if (gene != null)
                gene.Corruption = 0f;
        }

        /// <summary>
        /// Returns true if pawn's corruption is at or above a threshold (e.g. 0.8f for "high risk").
        /// </summary>
        public static bool IsCorruptionHigh(Pawn pawn, float threshold = 0.8f)
        {
            return GetCorruption(pawn) >= threshold;
        }

        /// <summary>
        /// Returns true if pawn's corruption is at or above the "collapse" threshold (e.g. 1.0f).
        /// </summary>
        public static bool IsCorruptionMaxed(Pawn pawn)
        {
            return GetCorruption(pawn) >= 1.0f;
        }

        /// <summary>
        /// Call this when a solver ability is used to handle corruption gain and possible XP gain.
        /// </summary>
        public static void OnSolverAbilityUsed(Pawn pawn, float baseCorruptionGain, float xpPerCorruption = 100f)
        {
            var gene = GetActiveNonNerfedSolverGene(pawn);
            if (gene == null) return;

            // Apply the ability corruption gain multiplier stat
            float corruptionMultiplier = pawn.GetStatValue(MD_DefOf.MD_AbilityCorruptionGainMultiplier, true);
            float finalCorruptionGain = baseCorruptionGain * corruptionMultiplier;

            AddCorruption(pawn, finalCorruptionGain);

            // If corruption is high, grant extra XP (example logic)
            if (IsCorruptionHigh(pawn))
            {
                SolverLevelingUtil.GainSolverXP(pawn, xpPerCorruption * 2f);
            }
            else
            {
                SolverLevelingUtil.GainSolverXP(pawn, xpPerCorruption);
            }
        }

        /// <summary>
        /// Triggers a mental break if corruption is too high and control is too low.
        /// </summary>
        public static void CheckForCorruptionBreak(Pawn pawn, float controlLevel)
        {
            float corruption = GetCorruption(pawn);
            if (corruption >= 0.8f && controlLevel < corruption * 20f)
            {
                if (pawn.InMentalState) return;
                pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "Solver corruption overload!", forceWake: true);
            }
        }
        public static void DrainCorruption(Pawn pawn, float baseDrainAmount)
        {
            var gene = GetActiveNonNerfedSolverGene(pawn);
            if (gene == null)
            {
                return;
            }

            float drainMultiplier = pawn.GetStatValue(MD_DefOf.MD_CorruptionDrainMultiplier, true);
            float finalDrain = baseDrainAmount * drainMultiplier;
            float before = gene.Corruption;
            gene.Corruption = Mathf.Max(0f, gene.Corruption - finalDrain);
            float after = gene.Corruption;
        }
    }
}

