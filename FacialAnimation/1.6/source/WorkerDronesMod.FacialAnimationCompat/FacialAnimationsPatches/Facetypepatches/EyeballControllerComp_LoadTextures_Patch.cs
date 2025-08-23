using FacialAnimation;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine; // <-- Add this line
using Verse;
using WorkerDronesMod.FacialAnimationCompat;


[HarmonyPatch(typeof(EyeballControllerComp), nameof(EyeballControllerComp.LoadTextures))]
public static class EyeballControllerComp_LoadTextures_Patch
{
    // Cache field info for performance
    private static readonly System.Reflection.FieldInfo PawnField =
        typeof(EyeballControllerComp).GetField("pawn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
    private static readonly System.Reflection.FieldInfo ColorField =
        typeof(EyeballControllerComp).GetField("color", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
    private static readonly System.Reflection.FieldInfo GraphicListField =
        typeof(EyeballControllerComp).GetField("graphicList", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

    static bool Prefix(EyeballControllerComp __instance)
    {
        if (!ModsConfig.BiotechActive)
            return true;

        // Fast field access
        var pawn = PawnField?.GetValue(__instance) as Pawn;
        if (pawn == null || pawn.genes?.GenesListForReading == null)
            return true;

        // Fast extension lookup
        GeneForcedFacetypesExtension ext = null;
        foreach (var gene in pawn.genes.GenesListForReading)
        {
            var e = gene.def.GetModExtension<GeneForcedFacetypesExtension>();
            if (e != null && e.EyeColorMatchesSkinColor)
            {
                ext = e;
                break;
            }
        }
        if (ext == null)
            return true;

        // Defensive: check story and skin color
        if (pawn.story == null)
            return true;
        Color skinColor = pawn.story.SkinColor;

        // Set both color slots
        if (ColorField != null)
            ColorField.SetValue(__instance, skinColor);
        __instance.FaceSecondColor = skinColor;

        // Defensive: clear graphics only if not null
        var graphicList = GraphicListField?.GetValue(__instance) as Dictionary<NLFacialAnimationLayerType, NLGraphic_Collection<EyeballShapeDef>>;
        if (graphicList != null)
            graphicList.Clear();

        // Let original LoadTextures run
        return true;
    }
}







