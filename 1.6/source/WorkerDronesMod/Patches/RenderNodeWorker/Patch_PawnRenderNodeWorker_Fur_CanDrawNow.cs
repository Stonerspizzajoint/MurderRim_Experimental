using HarmonyLib;
using Verse;
using RimWorld;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker_Body), "CanDrawNow")]
    public static class Patch_PawnRenderNodeWorker_Fur_CanDrawNow
    {
        static bool Prefix(PawnRenderNode node, PawnDrawParms parms, ref bool __result)
        {
            // Only affect fur nodes
            if (node is PawnRenderNode_Fur && parms.pawn != null && BabyAndroidUtil.IsBabyAndroid(parms.pawn))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}

