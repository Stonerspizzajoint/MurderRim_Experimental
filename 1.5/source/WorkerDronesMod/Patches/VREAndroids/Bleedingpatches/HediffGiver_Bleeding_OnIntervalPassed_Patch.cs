using HarmonyLib;
using RimWorld;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(HediffGiver_Bleeding), "OnIntervalPassed")]
    public static class HediffGiver_Bleeding_OnIntervalPassed_Patch
    {
        [HarmonyPriority(2147483647)]
        public static bool Prefix(Pawn pawn, Hediff cause)
        {
            // If the pawn has the WeakenedSolver gene, do not apply any bleeding hediff.
            if (pawn.HasActiveGene(MD_DefOf.MD_WeakenedSolver))
            {
                if (DebugSettings.godMode)
                    Log.Message($"[HediffGiver_Bleeding] {pawn.LabelShort} has MD_WeakenedSolver; skipping bleeding hediff.");
                return false; // Skip the original method completely.
            }

            // Next, check if the pawn has the MD_NeutroamineOil gene.
            if (pawn.HasActiveGene(MD_DefOf.MD_NeutroamineOil))
            {
                HediffSet hediffSet = pawn.health.hediffSet;

                // Update or add VREA_NeutroLoss based on the pawn’s bleed rate.
                if (hediffSet.BleedRateTotal >= 0f)
                {
                    HealthUtility.AdjustSeverity(pawn, VREA_DefOf.VREA_NeutroLoss, hediffSet.BleedRateTotal * 0.001f);
                }

                return false; // Skip the original method.
            }

            // For pawns without either gene, run the original method.
            return true;
        }
    }
}
