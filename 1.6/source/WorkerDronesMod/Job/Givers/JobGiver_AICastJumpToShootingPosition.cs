using System.Collections.Generic;
using Verse;
using RimWorld;
using Verse.AI;
using System.Linq;

namespace WorkerDronesMod
{
    public class JobGiver_AICastJumpToShootingPosition : JobGiver_AICastAbility
    {
        public int ActivationIntervalTicks = 1200;
        private static readonly Dictionary<Pawn, int> lastActivationTick = new Dictionary<Pawn, int>();

        protected override LocalTargetInfo GetTarget(Pawn caster, Ability abilityInstance)
        {
            if (caster.CurJob != null && (
                caster.CurJob.def == abilityInstance.def.jobDef
            ))
                return LocalTargetInfo.Invalid;

            if (caster?.Map == null || abilityInstance == null)
                return LocalTargetInfo.Invalid;

            int now = Find.TickManager.TicksGame;
            int lastTick = lastActivationTick.TryGetValue(caster, out int t) ? t : -ActivationIntervalTicks;
            if (now - lastTick < ActivationIntervalTicks)
                return LocalTargetInfo.Invalid;

            if (!JumpAbilityAIUtil.IsCasterUsingRangedWeapon(caster))
                return LocalTargetInfo.Invalid;

            float range = abilityInstance.verb.verbProps.range;
            Pawn targetPawn = JumpAbilityAIUtil.FindBestRangedTarget(caster, range * 2); // Look for targets up to double jump range

            if (targetPawn == null)
                return LocalTargetInfo.Invalid;

            // Calculate weapon range
            float weaponRange = caster.equipment?.Primary?.def.Verbs?
                .Where(v => !v.IsMeleeAttack)
                .Select(v => v.range)
                .DefaultIfEmpty(0f)
                .Max() ?? 0f;

            var shootPos = JumpAbilityAIUtil.FindShootingPositionWithCover(
                caster, targetPawn, weaponRange, minDistanceFromEnemy: 8f, searchRadius: (int)range);

            if (shootPos.HasValue)
            {
                lastActivationTick[caster] = now;
                return new LocalTargetInfo(shootPos.Value);
            }

            return LocalTargetInfo.Invalid;
        }
    }
}

