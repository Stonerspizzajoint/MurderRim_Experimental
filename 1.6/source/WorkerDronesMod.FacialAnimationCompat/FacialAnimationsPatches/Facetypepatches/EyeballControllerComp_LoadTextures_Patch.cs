using FacialAnimation;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using WorkerDronesMod.FacialAnimationCompat;

[HarmonyPatch(typeof(EyeballControllerComp), nameof(EyeballControllerComp.LoadTextures))]
public static class EyeballControllerComp_LoadTextures_Patch
{
    static bool Prefix(EyeballControllerComp __instance)
    {
        // if we’re not biotech‐forcing, bail out
        if (!ModsConfig.BiotechActive)
            return true;

        // grab the pawn and see if they even have genes
        Pawn pawn = AccessTools.FieldRefAccess<EyeballControllerComp, Pawn>(__instance, "pawn");
        if (pawn?.genes?.GenesListForReading == null)
            return true;

        // find our extension
        var ext = pawn.genes.GenesListForReading
            .Select(g => g.def.GetModExtension<GeneForcedFacetypesExtension>())
            .FirstOrDefault(e => e != null && e.EyeColorMatchesSkinColor);
        if (ext == null)
            return true;

        // 1) Force both color slots to skinColor
        Color skinColor = pawn.story.SkinColor;
        AccessTools.FieldRefAccess<EyeballControllerComp, Color>(__instance, "color") = skinColor;
        __instance.FaceSecondColor = skinColor;

        // 2) **Clear out the old graphics** so LoadTextures starts from scratch
        var graphicList = AccessTools.FieldRefAccess<EyeballControllerComp,
            Dictionary<NLFacialAnimationLayerType, NLGraphic_Collection<EyeballShapeDef>>>(__instance, "graphicList");
        graphicList.Clear();

        // 3) Let the original LoadTextures run with our forced colors
        return true;
    }
}







