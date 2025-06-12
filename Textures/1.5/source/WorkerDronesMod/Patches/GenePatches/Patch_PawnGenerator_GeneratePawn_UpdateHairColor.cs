using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
    public static class Patch_PawnGenerator_GeneratePawn_UpdateHairColor
    {
        static void Postfix(ref Pawn __result)
        {
            Pawn pawn = __result;
            if (pawn == null)
                return;

            // Ensure the pawn has genes and a story.
            if (pawn.genes != null && pawn.story != null)
            {
                // Look up the gene definition for "MD_DroneBody".
                GeneDef droneBodyGene = DefDatabase<GeneDef>.GetNamed("MD_DroneBody", false);
                if (droneBodyGene != null && pawn.genes.HasActiveGene(droneBodyGene))
                {
                    // Apply the effect with a chance (here 10% chance).
                    if (Rand.Value < 0.1f)
                    {
                        // Create a darker version of the skin color.
                        Color darkenedColor = DarkenColor(pawn.story.SkinColor, 0.6f);
                        pawn.story.HairColor = darkenedColor;
                        Log.Message($"[PawnGenerator Patch] {pawn.LabelShortCap}'s hair color set to a darker version: {darkenedColor}");

                        // Force a refresh of the pawn's graphics.
                        if (pawn.Drawer != null && pawn.Drawer.renderer != null && pawn.Drawer.renderer.renderTree != null)
                        {
                            pawn.Drawer.renderer.renderTree.SetDirty();
                        }
                        else
                        {
                            Log.Error("[PawnGenerator Patch] Unable to refresh graphics for pawn.");
                        }
                    }
                }
            }
        }

        private static Color DarkenColor(Color original, float factor)
        {
            // Multiply RGB by the factor, clamping between 0 and 1.
            float r = Mathf.Clamp01(original.r * factor);
            float g = Mathf.Clamp01(original.g * factor);
            float b = Mathf.Clamp01(original.b * factor);
            return new Color(r, g, b, original.a);
        }
    }
}




