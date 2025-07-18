using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class JobGiver_AICastMeleeAbilityOnSelf : JobGiver_AICastAbilityOnSelf
    {
        private const float MeleeRange = 5f;
        private const float ExtendedRange = 10f;
        private const float RandomSwitchChance = 0.5f;
        private static readonly float MeleeSq = MeleeRange * MeleeRange;
        private static readonly float ExtendedSq = ExtendedRange * ExtendedRange;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
                return null;

            // Find the closest hostile pawn (no allocations)
            Pawn closestEnemy = null;
            float closestDistSq = float.MaxValue;

            foreach (var target in pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn))
            {
                if (target is Pawn enemy && enemy.HostileTo(pawn))
                {
                    float distSq = pawn.Position.DistanceToSquared(enemy.Position);
                    if (distSq < closestDistSq)
                    {
                        closestDistSq = distSq;
                        closestEnemy = enemy;
                    }
                }
            }

            if (closestEnemy == null)
                return null;

            // Only end the current job if we're about to give a new one
            if (closestDistSq <= MeleeSq || (closestDistSq <= ExtendedSq && Rand.Chance(RandomSwitchChance)))
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                return base.TryGiveJob(pawn);
            }

            return null;
        }
    }
}



