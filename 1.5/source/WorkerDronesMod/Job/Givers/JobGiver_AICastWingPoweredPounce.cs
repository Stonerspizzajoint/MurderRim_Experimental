using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class JobGiver_AICastWingPoweredPounce : JobGiver_AICastAbility
    {
        private const int CooldownTicks = 600;  // 10s
        private const int FleeCooldownTicks = 300;  // 5s when chasing a fleeing pawn
        private const float MinJumpDistance = 10f;
        private const float ShooterTargetChance = 0.5f;

        // Per-caster cooldowns to allow multiple pawns to use this jobgiver independently
        private static readonly Dictionary<Pawn, int> lastJumpTick = new Dictionary<Pawn, int>();

        protected override LocalTargetInfo GetTarget(Pawn caster, Ability abilityInstance)
        {
            if (caster?.Map == null)
                return LocalTargetInfo.Invalid;

            int now = Find.TickManager.TicksGame;
            float maxRange = abilityInstance.verb.verbProps.range;
            float maxRangeSq = maxRange * maxRange;
            float minJumpSq = MinJumpDistance * MinJumpDistance;

            Pawn chosen = null;
            Pawn farthestValid = null;
            float farthestDistSq = -1f;

            // Find the best target in a single pass
            foreach (var target in caster.Map.attackTargetsCache.GetPotentialTargetsFor(caster))
            {
                if (target is Pawn enemy &&
                    enemy.Spawned && !enemy.Dead && !enemy.Downed &&
                    enemy.Faction != null && enemy.Faction.HostileTo(caster.Faction))
                {
                    float distSq = (enemy.Position - caster.Position).LengthHorizontalSquared;

                    // Try to pick a ranged weapon user within range
                    if (Rand.Value < ShooterTargetChance &&
                        distSq <= maxRangeSq &&
                        enemy.equipment?.Primary != null &&
                        enemy.equipment.Primary.def.Verbs.Any(v => v.range > 1f))
                    {
                        chosen = enemy;
                        break; // Prefer shooter if found
                    }

                    // Otherwise, track the farthest valid pawn
                    if (distSq > minJumpSq && distSq <= maxRangeSq && distSq > farthestDistSq)
                    {
                        farthestValid = enemy;
                        farthestDistSq = distSq;
                    }
                }
            }

            if (chosen == null)
                chosen = farthestValid;

            if (chosen == null)
                return LocalTargetInfo.Invalid;

            // Cooldown logic (per-caster)
            bool isFleeing = IsPawnFleeing(chosen);
            int requiredTicks = isFleeing ? FleeCooldownTicks : CooldownTicks;
            int lastTick = lastJumpTick.TryGetValue(caster, out int t) ? t : -CooldownTicks;
            if (now - lastTick < requiredTicks)
                return LocalTargetInfo.Invalid;

            lastJumpTick[caster] = now;
            return new LocalTargetInfo(chosen);
        }

        /// <summary>
        /// Very basic “fleeing” check: any job whose defName contains “Flee”.
        /// </summary>
        private bool IsPawnFleeing(Pawn pawn)
        {
            var job = pawn.CurJob;
            if (job == null)
                return false;
            return job.def.defName.IndexOf("Flee", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}










