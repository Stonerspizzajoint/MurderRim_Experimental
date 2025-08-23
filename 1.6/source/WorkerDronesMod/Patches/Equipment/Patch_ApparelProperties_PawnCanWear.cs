using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(ApparelProperties), nameof(ApparelProperties.PawnCanWear), new[] { typeof(Pawn), typeof(bool) })]
    public static class Patch_ApparelProperties_PawnCanWear
    {
        static void Postfix(ApparelProperties __instance, Pawn pawn, bool ignoreGender, ref bool __result)
        {
            if (pawn != null && pawn.def == MD_DefOf.MD_CoreHeartRace)
            {
                // Only allow hats (Overhead layer)
                if (__instance.LastLayer != ApparelLayerDefOf.Overhead)
                {
                    __result = false;
                }
            }
        }
    }
}
