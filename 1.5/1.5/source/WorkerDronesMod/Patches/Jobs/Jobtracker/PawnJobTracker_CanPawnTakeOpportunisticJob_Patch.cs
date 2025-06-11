using HarmonyLib;
using Verse;
using Verse.AI;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn_JobTracker), "CanPawnTakeOpportunisticJob")]
    public static class PawnJobTracker_CanPawnTakeOpportunisticJob_Patch
    {
        // The postfix runs after the original method.
        public static void Postfix(Pawn pawn, ref bool __result)
        {
            // If the pawn has our mod flags (interchangeable melee or ranged), force the result to false.
            if (SolverGeneUtility.HasInterchangeableMelee(pawn) || SolverGeneUtility.HasInterchangeableRanged(pawn))
            {
                __result = false;
            }
        }
    }
}
