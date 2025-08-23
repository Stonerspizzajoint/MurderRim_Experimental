using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    public static class SolverLevelingUtil
    {
        // Reference to your Solver Control skill
        public static SkillDef SolverControlSkillDef => MD_DefOf.SolverControl;

        /// <summary>
        /// Get the Solver Control skill record for a pawn, if present.
        /// </summary>
        public static SkillRecord GetSolverControlSkill(Pawn pawn)
        {
            if (pawn?.skills == null || SolverControlSkillDef == null)
                return null;
            return pawn.skills.GetSkill(SolverControlSkillDef);
        }

        private static Dictionary<Pawn, SolverTraitProgress> progressByPawn = new Dictionary<Pawn, SolverTraitProgress>();

        public static SolverTraitProgress GetProgress(Pawn pawn)
        {
            if (pawn == null) return null;
            // Prefer the gene's field if present
            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene != null)
                return gene.solverTraitProgress;

            // Fallback to static dictionary for pawns without the gene
            if (!progressByPawn.TryGetValue(pawn, out var progress))
            {
                progress = new SolverTraitProgress();
                progressByPawn[pawn] = progress;
            }
            return progress;
        }

        /// <summary>
        /// Get the Solver Control skill level (0-20), or 0 if not present.
        /// </summary>
        public static int GetSolverControlLevel(Pawn pawn)
        {
            var skill = GetSolverControlSkill(pawn);
            return skill?.Level ?? 0;
        }

        /// <summary>
        /// Grant XP to the Solver Control skill.
        /// </summary>
        public static void GainSolverXP(Pawn pawn, float amount)
        {
            var skill = GetSolverControlSkill(pawn);
            if (skill != null)
                skill.Learn(amount);
        }

        /// <summary>
        /// Returns a multiplier for ability success chance based on skill level.
        /// </summary>
        public static float GetAbilitySuccessChance(Pawn pawn)
        {
            int level = GetSolverControlLevel(pawn);
            return 0.5f + 0.025f * level; // 50% base, +2.5% per level (max 100%)
        }

        /// <summary>
        /// Returns a multiplier for heat gain based on skill level.
        /// </summary>
        public static float GetHeatMultiplier(Pawn pawn)
        {
            int level = GetSolverControlLevel(pawn);
            return Mathf.Lerp(1.0f, 0.5f, level / 20f); // 1.0 at level 0, 0.5 at level 20
        }

        /// <summary>
        /// Returns a multiplier for corruption gain based on skill level.
        /// </summary>
        public static float GetCorruptionGainMultiplier(Pawn pawn)
        {
            int level = GetSolverControlLevel(pawn);
            return Mathf.Lerp(1.0f, 0.5f, level / 20f); // 1.0 at level 0, 0.5 at level 20
        }

        /// <summary>
        /// Returns a multiplier for cooldown reduction based on skill level.
        /// </summary>
        public static float GetCooldownMultiplier(Pawn pawn)
        {
            int level = GetSolverControlLevel(pawn);
            return Mathf.Lerp(1.0f, 0.7f, level / 20f); // 1.0 at level 0, 0.7 at level 20
        }

        /// <summary>
        /// Returns the number of skill points gained for a specific level.
        /// Each level gives 1 point, every 2nd level gives an extra point (so 2, 4, 6, ... give 2 points).
        /// </summary>
        public static int SkillPointsForLevel(int level)
        {
            if (level < 1 || level > 20)
                return 0;
            // 1 point for every level, +1 extra if even
            return 1 + (level % 2 == 0 ? 1 : 0);
        }

        /// <summary>
        /// Returns the total skill points gained from level 1 up to and including the given level.
        /// </summary>
        public static int TotalSkillPointsUpToLevel(int level)
        {
            int total = 0;
            for (int i = 1; i <= level; i++)
                total += SkillPointsForLevel(i);
            return total;
        }

        /// <summary>
        /// Returns the number of skill points gained when leveling up from oldLevel to newLevel (exclusive of oldLevel, inclusive of newLevel).
        /// </summary>
        public static int SkillPointsGainedBetweenLevels(int oldLevel, int newLevel)
        {
            int total = 0;
            for (int i = oldLevel + 1; i <= newLevel; i++)
                total += SkillPointsForLevel(i);
            return total;
        }
    }
}

