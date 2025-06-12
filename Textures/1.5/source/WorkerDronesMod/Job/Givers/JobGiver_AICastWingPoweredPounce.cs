using System;
using System.Linq;
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

        private int lastJumpTick = -CooldownTicks;

        protected override LocalTargetInfo GetTarget(Pawn caster, Ability abilityInstance)
        {
            if (caster?.Map == null)
                return LocalTargetInfo.Invalid;

            // precompute
            int now = Find.TickManager.TicksGame;
            float maxRange = abilityInstance.verb.verbProps.range;
            float maxRangeSq = maxRange * maxRange;
            float minJumpSq = MinJumpDistance * MinJumpDistance;

            // collect real hostile pawns once
            var enemies = caster.Map
                .attackTargetsCache
                .GetPotentialTargetsFor(caster)
                .OfType<Pawn>()
                .Where(p =>
                    p.Spawned && !p.Dead && !p.Downed &&
                    p.Faction != null && p.Faction.HostileTo(caster.Faction)
                )
                .ToList();
            if (!enemies.Any())
                return LocalTargetInfo.Invalid;

            Pawn chosen = null;

            // 1) Try a “ranged‐weapon” target
            if (Rand.Value < ShooterTargetChance)
            {
                chosen = enemies
                    .Where(p =>
                        (p.Position - caster.Position).LengthHorizontalSquared <= maxRangeSq
                        && p.equipment?.Primary != null
                        && p.equipment.Primary.def.Verbs.Any(v => v.range > 1f)
                    )
                    .RandomElementWithFallback(null);
            }

            // 2) If no shooter picked, pick the farthest valid pawn
            if (chosen == null)
            {
                chosen = enemies
                    .Where(p =>
                    {
                        float dsq = (p.Position - caster.Position).LengthHorizontalSquared;
                        return dsq > minJumpSq && dsq <= maxRangeSq;
                    })
                    .OrderByDescending(p =>
                        (p.Position - caster.Position).LengthHorizontalSquared
                    )
                    .FirstOrDefault();
            }

            if (chosen == null)
                return LocalTargetInfo.Invalid;

            // decide which cooldown applies
            bool isFleeing = IsPawnFleeing(chosen);
            int requiredTicks = isFleeing ? FleeCooldownTicks : CooldownTicks;
            if (now - lastJumpTick < requiredTicks)
                return LocalTargetInfo.Invalid;

            // OK to pounce
            lastJumpTick = now;
            return new LocalTargetInfo(chosen);
        }

        /// <summary>
        /// Very basic “fleeing” check: any job whose defName contains “Flee”.
        /// You can refine this to specific JobDefs if you want.
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










