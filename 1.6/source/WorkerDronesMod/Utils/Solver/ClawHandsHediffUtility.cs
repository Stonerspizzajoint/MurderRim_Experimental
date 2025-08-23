using RimWorld;
using Verse;
using System.Linq;

namespace WorkerDronesMod
{
    public static class ClawHandsHediffUtility
    {
        // Applies the Hediff to both hands if not already present
        public static void ApplyClawHandsIfNeeded(Pawn pawn)
        {
            if (pawn == null || pawn.health == null) return;

            // Only apply if the pawn has the ability
            if (pawn.abilities?.GetAbility(MD_DefOf.MD_InterchangeableHandsAbility) == null)
                return;

            var hands = pawn.RaceProps.body.AllParts
                .Where(part => part.def.defName == "Hand" && !pawn.health.hediffSet.PartIsMissing(part));
            foreach (var hand in hands)
            {
                if (pawn.health.hediffSet.hediffs.All(h => h.def != MD_DefOf.MD_interchangeable_ClawHands || h.Part != hand))
                {
                    var hediff = HediffMaker.MakeHediff(MD_DefOf.MD_interchangeable_ClawHands, pawn, hand);
                    pawn.health.AddHediff(hediff, hand);
                }
            }
        }

        // Removes the Hediff from both hands
        public static void RemoveClawHands(Pawn pawn)
        {
            if (pawn == null || pawn.health == null) return;
            var hediffs = pawn.health.hediffSet.hediffs
                .Where(h => h.def == MD_DefOf.MD_interchangeable_ClawHands)
                .ToList();
            foreach (var h in hediffs)
            {
                pawn.health.RemoveHediff(h);
            }
        }

        // Checks if the pawn is currently in the oil craving mental state
        public static bool IsInOilCravingMentalState(Pawn pawn)
        {
            return pawn?.MentalStateDef == MD_DefOf.MD_RefuelMadness;
        }
    }
}
