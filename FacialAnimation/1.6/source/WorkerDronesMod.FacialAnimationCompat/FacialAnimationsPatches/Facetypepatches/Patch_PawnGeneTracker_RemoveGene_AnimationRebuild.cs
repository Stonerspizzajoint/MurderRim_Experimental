using HarmonyLib;
using System.Linq;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System;
using FacialAnimation;
using WorkerDronesMod.FacialAnimationCompat;
using System.Reflection;

namespace WorkerDronesMod.FacialAnimationCompat
{
    [HarmonyPatch(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.RemoveGene))]
    public static class Patch_PawnGeneTracker_RemoveGene_AnimationRebuild
    {
        static void Postfix(Gene gene, Pawn_GeneTracker __instance)
        {
            var pawn = __instance.pawn;
            if (pawn == null) return;

            // Queue animation dictionary rebuild for this pawn
            FacialAnimationBatcher.QueueAnimationRebuild(pawn);
        }
    }
}



