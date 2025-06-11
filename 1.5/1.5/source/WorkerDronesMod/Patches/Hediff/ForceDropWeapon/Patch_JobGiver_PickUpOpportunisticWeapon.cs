using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(JobGiver_PickUpOpportunisticWeapon), "TryGiveJob")]
    public static class Patch_JobGiver_PickUpOpportunisticWeapon
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (pawn == null)
                return true;

            // Check all hediffs on this pawn.
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                // If the pawn has our custom component, cancel the job.
                if (hediff.TryGetComp<HediffComp_ForceDropWeapon>() != null)
                {
                    Log.Message($"[WorkerDronesMod] Prevented opportunistic weapon pickup for pawn {pawn.LabelShort} due to ForceDropWeapon hediff.");
                    __result = null;
                    return false; // Skip the original method so no job is given.
                }
            }
            return true; // Allow normal processing if no hediff triggers our condition.
        }
    }
}
