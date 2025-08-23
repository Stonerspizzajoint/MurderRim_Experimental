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
    public static class Patch_PawnGeneTracker_RemoveGene_ClearForcedTypes
    {
        private static readonly Type FacType = AccessTools.TypeByName("FacialAnimation.FacialAnimationControllerComp");
        private static readonly Type FaHelperType = AccessTools.TypeByName("FacialAnimation.FAHelper");
        private static readonly FieldInfo AnimDictField = FacType?.GetField("animationDict", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly MethodInfo CreateAnimDict = FaHelperType?.GetMethod("CreateAnimationDict", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        static void Postfix(Gene gene, Pawn_GeneTracker __instance)
        {
            var pawn = __instance.pawn;
            if (pawn == null) return;

            var allExts = FacialAnimationGeneUtil.GetAllGeneFacetypesExtensions(pawn);
            bool hasAnyExtensions = allExts.Count > 0;
            string fallback = pawn.def.defName;
            var tagsForPart = FacialAnimationGeneUtil.BuildTagsForParts(allExts, hasAnyExtensions, fallback);

            if (!hasAnyExtensions)
            {
                FacialAnimationGeneUtil.ResetToBaseRace<HeadControllerComp, FacialAnimation.HeadTypeDef>(pawn, FacePartType.Head, fallback);
                FacialAnimationGeneUtil.ResetToBaseRace<BrowControllerComp, BrowTypeDef>(pawn, FacePartType.Brow, fallback);
                FacialAnimationGeneUtil.ResetToBaseRace<MouthControllerComp, MouthTypeDef>(pawn, FacePartType.Mouth, fallback);
                FacialAnimationGeneUtil.ResetToBaseRace<EyeballControllerComp, EyeballTypeDef>(pawn, FacePartType.Eye, fallback);
                FacialAnimationGeneUtil.ResetToBaseRace<LidControllerComp, LidTypeDef>(pawn, FacePartType.Lid, fallback);
                FacialAnimationGeneUtil.ResetToBaseRace<LidOptionControllerComp, LidOptionTypeDef>(pawn, FacePartType.LidOption, fallback);
                FacialAnimationGeneUtil.ResetToBaseRace<SkinControllerComp, SkinTypeDef>(pawn, FacePartType.Skin, fallback);
            }
            else
            {
                FacialAnimationGeneUtil.SetIfChanged<HeadControllerComp, FacialAnimation.HeadTypeDef>(pawn, FacePartType.Head, allExts, hasAnyExtensions, tagsForPart, fallback, ext => ext.forcedHeadTypes);
                FacialAnimationGeneUtil.SetIfChanged<BrowControllerComp, BrowTypeDef>(pawn, FacePartType.Brow, allExts, hasAnyExtensions, tagsForPart, fallback, ext => ext.forcedBrowTypes);
                FacialAnimationGeneUtil.SetIfChanged<MouthControllerComp, MouthTypeDef>(pawn, FacePartType.Mouth, allExts, hasAnyExtensions, tagsForPart, fallback, ext => ext.forcedMouthTypes);
                FacialAnimationGeneUtil.SetIfChanged<EyeballControllerComp, EyeballTypeDef>(pawn, FacePartType.Eye, allExts, hasAnyExtensions, tagsForPart, fallback, ext => ext.forcedEyeTypes);
                FacialAnimationGeneUtil.SetIfChanged<LidControllerComp, LidTypeDef>(pawn, FacePartType.Lid, allExts, hasAnyExtensions, tagsForPart, fallback, ext => ext.forcedLidTypes);
                FacialAnimationGeneUtil.SetIfChanged<LidOptionControllerComp, LidOptionTypeDef>(pawn, FacePartType.LidOption, allExts, hasAnyExtensions, tagsForPart, fallback, ext => ext.forcedLidOptionTypes);
                FacialAnimationGeneUtil.SetIfChanged<SkinControllerComp, SkinTypeDef>(pawn, FacePartType.Skin, allExts, hasAnyExtensions, tagsForPart, fallback, ext => ext.forcedSkinTypes);
            }

            // Update FacialAnimationControllerComp after gene removal
            FacialAnimationBatcher.QueueAnimationRebuild(pawn);

            // Reload all face part controllers
            LongEventHandler.ExecuteWhenFinished(() => FacialAnimationGeneUtil.ReloadAllFacePartControllers(pawn));
        }
    }
}



