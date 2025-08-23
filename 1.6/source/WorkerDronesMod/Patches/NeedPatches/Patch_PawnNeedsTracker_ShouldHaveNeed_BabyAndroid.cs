using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
    public static class Patch_PawnNeedsTracker_ShouldHaveNeed_BabyAndroid
    {
        private static readonly FieldInfo PawnField = typeof(Pawn_NeedsTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);

        static bool Prefix(Pawn_NeedsTracker __instance, NeedDef nd, ref bool __result)
        {
            var pawn = PawnField?.GetValue(__instance) as Pawn;
            if (pawn != null && BabyAndroidUtil.IsBabyAndroid(pawn))
            {
                // Always give Play need
                if (nd == MD_DefOf.Play)
                {
                    __result = true;
                    return false; // Skip vanilla logic
                }
                // Never give Joy, Beauty, Comfort
                if (nd == MD_DefOf.Joy || nd == MD_DefOf.Beauty || nd == MD_DefOf.Comfort)
                {
                    __result = false;
                    return false; // Skip vanilla logic
                }
            }
            return true; // Use vanilla logic otherwise
        }
    }
}

