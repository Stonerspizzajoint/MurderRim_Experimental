using System.Linq;
using Verse;

namespace WorkerDronesMod
{
    public static class AbilityHediffUtility
    {
        /// <summary>
        /// Returns true if the pawn has any hediff whose defName starts with "MD_interchangeable_"
        /// but not with "MD_interchangeableRanged_". This signals that the melee ability is active.
        /// </summary>
        public static bool HasMeleeHediff(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
                return false;

            return pawn.health.hediffSet.hediffs.Any(h =>
                h.def.defName.StartsWith("MD_interchangeable_") &&
                !h.def.defName.StartsWith("MD_interchangeableRanged_"));
        }

        /// <summary>
        /// Returns true if the pawn has any hediff whose defName starts with "MD_interchangeableRanged_"
        /// signaling that the ranged ability is active.
        /// </summary>
        public static bool HasRangedHediff(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
                return false;

            return pawn.health.hediffSet.hediffs.Any(h =>
                h.def.defName.StartsWith("MD_interchangeableRanged_"));
        }
    }
}
