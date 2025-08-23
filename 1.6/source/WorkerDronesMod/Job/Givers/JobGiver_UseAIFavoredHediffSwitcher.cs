using Verse;
using RimWorld;
using Verse.AI;
using System.Linq;

namespace WorkerDronesMod
{
    public class JobGiver_UseAIFavoredHediffSwitcher : ThinkNode_JobGiver
    {
        public float MeleeSwitchDistance = 4f; // Default: switch to melee if enemy is closer than this
        public float RangedSwitchDistance = 4f; // Default: switch to ranged if enemy is farther than this
        public float MeleeHeatThresholdPercent = 0.9f; // Switch to melee if heat exceeds 90% of max

        protected override Job TryGiveJob(Pawn pawn)
        {
            var ability = pawn.abilities?.AllAbilitiesForReading
                .OfType<Ability_HediffSwitcher>()
                .FirstOrDefault();

            if (ability == null || ability.def == null)
                return null;

            if (pawn.CurJob != null && pawn.CurJob.ability == ability)
                return null;
            if (ability.Casting)
                return null;

            var ext = ability.def.GetModExtension<ModExtension_AbilityHediffSwitcher>();
            if (ext == null || ext.selectableHediffs == null)
                return null;

            var aiOptions = ext.selectableHediffs.Where(o => o.AIfavored).ToList();
            if (aiOptions.Count == 0)
                return null;

            var currentHediff = ability.selectedOption;

            // Gather all AI-favored ranged and melee options
            var aiRangedOptions = aiOptions.Where(o => o.IsRanged).ToList();
            var aiMeleeOptions = aiOptions.Where(o => o.IsMelee).ToList();

            // Switch to melee if pawn is about to do a melee attack job
            if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.AttackMelee)
            {
                if (aiMeleeOptions.Count > 0 && (currentHediff == null || !aiMeleeOptions.Contains(currentHediff)))
                {
                    var meleeOption = aiMeleeOptions.RandomElement();
                    ability.selectedOption = meleeOption;
                    if (!ability.CanCast.Accepted)
                        return null;
                    return ability.GetJob(new LocalTargetInfo(pawn), LocalTargetInfo.Invalid);
                }
                // Already melee, do nothing
                return null;
            }

            float heat = 0f;
            var geneSolver = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (geneSolver != null)
                heat = geneSolver.heat;

            Pawn enemy = FindClosestActiveHostilePawn(pawn);
            float dist = enemy != null ? (enemy.Position - pawn.Position).LengthHorizontal : float.MaxValue;

            // 0. If no active threats, switch to default and stay default
            if (enemy == null)
            {
                var defaultOption = aiOptions.FirstOrDefault(o => o.IsDefault);
                if (defaultOption != null && currentHediff != defaultOption)
                {
                    Log.Message($"[AI Hediff Switcher] Switching to default because no active enemies found.");
                    ability.selectedOption = defaultOption;
                    if (!ability.CanCast.Accepted)
                        return null;
                    return ability.GetJob(new LocalTargetInfo(pawn), LocalTargetInfo.Invalid);
                }
                // Already default, do nothing
                return null;
            }

            // 1. If heat > MeleeHeatThresholdPercent * max, switch to melee immediately
            if (geneSolver != null && geneSolver.InitialResourceMax > 0f &&
                heat > MeleeHeatThresholdPercent * geneSolver.InitialResourceMax)
            {
                if (aiMeleeOptions.Count > 0 && (currentHediff == null || !aiMeleeOptions.Contains(currentHediff)))
                {
                    var meleeOption = aiMeleeOptions.RandomElement();
                    ability.selectedOption = meleeOption;
                    if (!ability.CanCast.Accepted)
                        return null;
                    return ability.GetJob(new LocalTargetInfo(pawn), LocalTargetInfo.Invalid);
                }
                return null;
            }

            // 2. If enemy is very close (< MeleeSwitchDistance), prioritize melee
            if (dist < MeleeSwitchDistance)
            {
                if (aiMeleeOptions.Count > 0 && (currentHediff == null || !aiMeleeOptions.Contains(currentHediff)))
                {
                    var meleeOption = aiMeleeOptions.RandomElement();
                    ability.selectedOption = meleeOption;
                    if (!ability.CanCast.Accepted)
                        return null;
                    return ability.GetJob(new LocalTargetInfo(pawn), LocalTargetInfo.Invalid);
                }
                return null;
            }

            // 4. If enemy is not close (>= RangedSwitchDistance), switch to ranged for most, but some keep melee
            if (dist >= RangedSwitchDistance)
            {
                // 80% chance to switch to ranged, 20% chance to keep melee
                if (Rand.Value < 0.8f)
                {
                    if (aiRangedOptions.Count > 0 && (currentHediff == null || !aiRangedOptions.Contains(currentHediff)))
                    {
                        var rangedOption = aiRangedOptions.RandomElement();
                        ability.selectedOption = rangedOption;
                        if (!ability.CanCast.Accepted)
                            return null;
                        return ability.GetJob(new LocalTargetInfo(pawn), LocalTargetInfo.Invalid);
                    }
                }
                else
                {
                    if (aiMeleeOptions.Count > 0 && (currentHediff == null || !aiMeleeOptions.Contains(currentHediff)))
                    {
                        var meleeOption = aiMeleeOptions.RandomElement();
                        ability.selectedOption = meleeOption;
                        if (!ability.CanCast.Accepted)
                            return null;
                        return ability.GetJob(new LocalTargetInfo(pawn), LocalTargetInfo.Invalid);
                    }
                }
                return null;
            }

            // 5. Fallback to default if nothing else
            var fallbackDefault = aiOptions.FirstOrDefault(o => o.IsDefault);
            if (fallbackDefault != null && currentHediff != fallbackDefault)
            {
                ability.selectedOption = fallbackDefault;
                if (!ability.CanCast.Accepted)
                    return null;
                return ability.GetJob(new LocalTargetInfo(pawn), LocalTargetInfo.Invalid);
            }

            return null;
        }

        private Pawn FindClosestActiveHostilePawn(Pawn pawn)
        {
            if (pawn?.Map == null) return null;
            Pawn closest = null;
            float closestDist = float.MaxValue;
            foreach (var target in pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn))
            {
                if (target is Pawn enemy &&
                    enemy.Spawned && !enemy.Dead && !enemy.Downed &&
                    enemy.Faction != null && enemy.Faction.HostileTo(pawn.Faction))
                {
                    float dist = (enemy.Position - pawn.Position).LengthHorizontal;
                    if (dist < closestDist)
                    {
                        closest = enemy;
                        closestDist = dist;
                    }
                }
            }
            return closest;
        }

    }
}

