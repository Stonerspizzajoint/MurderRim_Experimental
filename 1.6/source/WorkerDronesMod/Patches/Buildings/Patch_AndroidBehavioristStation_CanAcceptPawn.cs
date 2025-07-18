using HarmonyLib;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(VREAndroids.Building_AndroidBehavioristStation), "CanAcceptPawn")]
    public static class Patch_AndroidBehavioristStation_CanAcceptPawn
    {
        // Prefix: if pawn is an android with the Reprogrammable gene, immediately return accepted.
        public static bool Prefix(Pawn selPawn, ref AcceptanceReport __result)
        {
            if (selPawn != null
                && selPawn.IsAndroid()
                && selPawn.genes?.GetFirstGeneOfType<Gene_Reprogrammable>() != null)
            {
                __result = true; // Implicitly creates an accepted AcceptanceReport.
                return false;    // Skip original method.
            }
            return true;
        }
    }
}



