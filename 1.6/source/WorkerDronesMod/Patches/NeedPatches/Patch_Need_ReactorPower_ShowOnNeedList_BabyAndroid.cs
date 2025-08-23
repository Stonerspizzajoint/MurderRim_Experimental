using HarmonyLib;
using RimWorld;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
    public static class Patch_Pawn_NeedsTracker_ShouldHaveNeed_BabyAndroid
    {
        static bool Prefix(Pawn_NeedsTracker __instance, NeedDef nd, ref bool __result)
        {
            // Access the private pawn field via reflection
            var pawnField = AccessTools.Field(typeof(Pawn_NeedsTracker), "pawn");
            Pawn pawn = pawnField.GetValue(__instance) as Pawn;

            // Only affect ReactorPower need
            if (nd == VREA_DefOf.VREA_ReactorPower && pawn != null)
            {
                // If baby android or baby stage, always allow the need
                if (BabyAndroidUtil.IsBabyAndroid(pawn) || pawn.DevelopmentalStage == DevelopmentalStage.Baby)
                {
                    __result = true;
                    return false; // Skip vanilla logic
                }
            }
            return true; // Use vanilla logic otherwise
        }
    }
}

