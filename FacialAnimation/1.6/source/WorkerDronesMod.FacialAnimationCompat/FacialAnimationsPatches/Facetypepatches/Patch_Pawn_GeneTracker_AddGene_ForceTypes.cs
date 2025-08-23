using HarmonyLib;
using System.Linq;
using Verse;
using FacialAnimation;
using WorkerDronesMod.FacialAnimationCompat;
using RimWorld;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace WorkerDronesMod.Patches.FacialAnimations
{
    [HarmonyPatch(typeof(Pawn_GeneTracker), "AddGene", new[] { typeof(Gene), typeof(bool) })]
    public static class Patch_PawnGeneTracker_AddGene_ForceTypes
    {
        private static readonly Type FacType = AccessTools.TypeByName("FacialAnimation.FacialAnimationControllerComp");
        private static readonly Type FaHelperType = AccessTools.TypeByName("FacialAnimation.FAHelper");
        private static readonly FieldInfo AnimDictField = FacType?.GetField("animationDict", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly MethodInfo CreateAnimDict = FaHelperType?.GetMethod("CreateAnimationDict", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        static void Postfix(Gene __result, Pawn_GeneTracker __instance)
        {
            if (__result == null) return;
            var pawn = __instance.pawn;
            if (pawn == null) return;

            var allExts = FacialAnimationGeneUtil.GetAllGeneFacetypesExtensions(pawn);
            bool hasAnyExtensions = allExts.Count > 0;
            string fallback = pawn.def.defName;
            var tagsForPart = FacialAnimationGeneUtil.BuildTagsForParts(allExts, hasAnyExtensions, fallback);

            FacialAnimationGeneUtil.SetIfChanged<HeadControllerComp, FacialAnimation.HeadTypeDef>(pawn, FacePartType.Head, allExts, hasAnyExtensions, tagsForPart, fallback, ext => ext.forcedHeadTypes);
            FacialAnimationGeneUtil.SetIfChanged<BrowControllerComp, BrowTypeDef>(pawn, FacePartType.Brow, allExts, hasAnyExtensions, tagsForPart, fallback, ext => ext.forcedBrowTypes);
            FacialAnimationGeneUtil.SetIfChanged<MouthControllerComp, MouthTypeDef>(pawn, FacePartType.Mouth, allExts, hasAnyExtensions, tagsForPart, fallback, ext => ext.forcedMouthTypes);
            FacialAnimationGeneUtil.SetIfChanged<EyeballControllerComp, EyeballTypeDef>(pawn, FacePartType.Eye, allExts, hasAnyExtensions, tagsForPart, fallback, ext => ext.forcedEyeTypes);
            FacialAnimationGeneUtil.SetIfChanged<LidControllerComp, LidTypeDef>(pawn, FacePartType.Lid, allExts, hasAnyExtensions, tagsForPart, fallback, ext => ext.forcedLidTypes);
            FacialAnimationGeneUtil.SetIfChanged<LidOptionControllerComp, LidOptionTypeDef>(pawn, FacePartType.LidOption, allExts, hasAnyExtensions, tagsForPart, fallback, ext => ext.forcedLidOptionTypes);
            FacialAnimationGeneUtil.SetIfChanged<SkinControllerComp, SkinTypeDef>(pawn, FacePartType.Skin, allExts, hasAnyExtensions, tagsForPart, fallback, ext => ext.forcedSkinTypes);

            // Update FacialAnimationControllerComp after gene add
            FacialAnimationBatcher.QueueAnimationRebuild(pawn);

            // Reload all face part controllers
            LongEventHandler.ExecuteWhenFinished(() => FacialAnimationGeneUtil.ReloadAllFacePartControllers(pawn));
        }
    }
}











