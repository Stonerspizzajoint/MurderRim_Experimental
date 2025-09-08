using HarmonyLib;
using Verse;
using RimWorld;
using FacialAnimation;
using WorkerDronesMod.FacialAnimationCompat;

namespace WorkerDronesMod.Patches.FacialAnimations
{
    [HarmonyPatch(typeof(Pawn_GeneTracker), "AddGene", new[] { typeof(Gene), typeof(bool) })]
    public static class Patch_PawnGeneTracker_AddGene_ForceTypes
    {
        static void Postfix(Gene __result, Pawn_GeneTracker __instance)
        {
            if (__result == null) return;
            var pawn = __instance.pawn;
            if (pawn == null) return;

            // Queue animation dictionary rebuild for this pawn
            FacialAnimationBatcher.QueueAnimationRebuild(pawn);

            // Reload eye graphics
            FacialAnimationGeneUtil.SafeReload(pawn.GetComp<EyeballControllerComp>());
        }
    }
}
