using HarmonyLib;
using Verse;
using Verse.AI;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(ThinkNode_ConditionalCanPickupOpportunisticWeapon), "Satisfied")]
    public static class ThinkNode_ConditionalCanPickupOpportunisticWeapon_Patch
    {
        // This prefix runs before the original Satisfied method.
        // It will only allow the condition to be satisfied if the pawn does NOT have either interchangeable melee or ranged parts.
        public static bool Prefix(Pawn pawn, ref bool __result)
        {
            // If the pawn has any interchangeable melee or ranged parts, force the condition to false.
            if (SolverGeneUtility.HasInterchangeableMelee(pawn) || SolverGeneUtility.HasInterchangeableRanged(pawn))
            {
                __result = false;
                return false; // Skip original method.
            }
            return true; // Continue with original behavior.
        }

    }
}
