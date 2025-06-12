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
            if (pawn?.Map == null)
                return null;

            // Only iterate real attack targets
            var potentialTargets = pawn.Map
                .attackTargetsCache
                .GetPotentialTargetsFor(pawn)
                .OfType<Pawn>()
                .Where(e => e.HostileTo(pawn))
                .ToList();

            // Safely get the closest or bail out
            var closestEnemy = potentialTargets
                .OrderBy(e => pawn.Position.DistanceToSquared(e.Position))
                .FirstOrDefault();
            if (closestEnemy == null)
                return null;

            float distSq = pawn.Position.DistanceToSquared(closestEnemy.Position);

            // Immediate melee?
            if (distSq <= MeleeSq)
                return base.TryGiveJob(pawn);

            // Just outside melee but within extended?
            if (distSq <= ExtendedSq && Rand.Chance(RandomSwitchChance))
                return base.TryGiveJob(pawn);

            return null;
        }
    }
}



