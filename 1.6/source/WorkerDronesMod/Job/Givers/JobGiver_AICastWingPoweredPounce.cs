using System.Collections.Generic;
using Verse;
using RimWorld;
using Verse.AI;

namespace WorkerDronesMod
{
    public class JobGiver_AICastWingPoweredPounce : JobGiver_AICastAbility
    {
        // Editable interval in ticks (default: 1200 = 20 seconds)
        public int ActivationIntervalTicks = 1200;
        public int CatchupActivationIntervalTicks = 300; // e.g., 5 seconds

        private static readonly Dictionary<Pawn, int> lastCatchupActivationTick = new Dictionary<Pawn, int>();
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
            float range = abilityInstance.verb.verbProps.range;
            Pawn targetPawn = JumpAbilityAIUtil.FindBestJumpTargetInRange(caster, range);

            // Attack pounce logic
            if (targetPawn != null)
            {
                if (now - lastTick < ActivationIntervalTicks)
                    return LocalTargetInfo.Invalid;

                lastActivationTick[caster] = now;
                return new LocalTargetInfo(targetPawn.Position);
            }

            // Catch-up pounce logic
            Pawn closestHostile = JumpAbilityAIUtil.FindClosestHostilePawn(caster);
            if (closestHostile != null)
            {
                if (!JumpAbilityAIUtil.IsCasterAllowedToJump(caster))
                    return LocalTargetInfo.Invalid;

                int lastCatchupTick = lastCatchupActivationTick.TryGetValue(caster, out int ct) ? ct : -CatchupActivationIntervalTicks;
                if (now - lastCatchupTick < CatchupActivationIntervalTicks)
                    return LocalTargetInfo.Invalid;

                lastCatchupActivationTick[caster] = now;
                return new LocalTargetInfo(closestHostile.Position);
            }

            Log.Message($"[Pounce] No valid target found for {caster}.");
            return LocalTargetInfo.Invalid;
        }
    }
}


