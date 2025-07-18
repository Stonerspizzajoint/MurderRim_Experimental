using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(PawnGenerator))]
    [HarmonyPatch(nameof(PawnGenerator.GeneratePawn))]
    [HarmonyPatch(new Type[] { typeof(PawnGenerationRequest) })]
    public static class PawnGenerator_GeneratePawn_Patch
    {
        public static void Postfix(Pawn __result)
        {
            // --- Skin color logic: check for both base and alt display color gene ---
            var displayColorGenes = GeneDefHelper.GetGeneDefAndAlternative(
                MD_DefOf.MD_DisplayColor_Random,
                MD_DefOf.VREA_MD_DisplayColor_Random
            ).ToArray();

            var randomSkinGene = __result.genes?.GenesListForReading
                .FirstOrDefault(g => displayColorGenes.Contains(g.def) && !g.Overridden);

            if (randomSkinGene != null)
            {
                var customColors = new List<Color>
                {
                    new Color32(255, 255, 255, 255), // pale (white)
                    new Color32(255, 0, 222, 255),
                    new Color32(163, 102, 255, 255),
                    new Color32(255, 0, 0, 255),
                    new Color32(255, 147, 15, 255),
                    new Color32(15, 119, 255, 255),
                    new Color32(57, 244, 247, 255),
                    new Color32(126, 255, 79, 255),
                    new Color32(247, 219, 36, 255)
                };
                int mixCount = Rand.RangeInclusive(2, 3);
                List<Color> chosenColors = new List<Color>();

                // Ensure yellow is never the only color chosen and yellow never mixes with white
                do
                {
                    chosenColors.Clear();
                    for (int i = 0; i < mixCount; i++)
                    {
                        chosenColors.Add(customColors[Rand.Range(0, customColors.Count)]);
                    }
                }
                while (
                    (chosenColors.Count > 1 && chosenColors.All(c => c == customColors[8])) || // Only yellow (last in list)
                    (chosenColors.Contains(customColors[8]) && chosenColors.Contains(customColors[0])) // Yellow and white together
                );


                Color mixedColor = chosenColors[0];
                for (int i = 1; i < chosenColors.Count; i++)
                {
                    mixedColor = Color.Lerp(mixedColor, chosenColors[i], 0.5f);
                }
                float maxChannel = Mathf.Max(mixedColor.r, mixedColor.g, mixedColor.b);
                if (maxChannel > 0f)
                {
                    mixedColor.r /= maxChannel;
                    mixedColor.g /= maxChannel;
                    mixedColor.b /= maxChannel;
                }
                if (__result.story != null)
                {
                    __result.story.skinColorOverride = mixedColor;
                }
                if (__result.Drawer != null && __result.Drawer.renderer != null)
                {
                    __result.Drawer.renderer.SetAllGraphicsDirty();
                }
            }

            // --- Hair color logic: only apply if pawn has MD_DroneBody or its alternative ---
            var droneGenes = GeneDefHelper.GetGeneDefAndAlternative(
                MD_DefOf.MD_DroneBody,
                MD_DefOf.VREA_MD_DroneBody
            ).ToArray();

            if (GeneDefHelper.PawnHasAnyGene(__result, droneGenes) && Rand.Value < 0.1f)
            {
                if (__result.story != null)
                {
                    Color baseColor = __result.story.skinColorOverride.HasValue ? __result.story.skinColorOverride.Value : __result.story.SkinColor;
                    Color darkenedColor = DarkenColor(baseColor, 0.6f);
                    __result.story.HairColor = darkenedColor;

                    if (__result.Drawer != null && __result.Drawer.renderer != null && __result.Drawer.renderer.renderTree != null)
                    {
                        __result.Drawer.renderer.renderTree.SetDirty();
                    }
                }
            }

            // --- Helmet logic: only apply if pawn has MD_DroneBody or its alternative ---
            if (GeneDefHelper.PawnHasAnyGene(__result, droneGenes) && Rand.Value < 0.25f)
            {
                ThingDef helmetDef = MD_DefOf.MD_Headgear_Hardhat;
                ThingDef steel = ThingDefOf.Steel;

                if (helmetDef != null && steel != null)
                {
                    Apparel helmet = ThingMaker.MakeThing(helmetDef) as Apparel;

                    if (helmet != null)
                    {
                        if (__result.apparel != null)
                        {
                            try
                            {
                                __result.apparel.Wear(helmet, false);
                                return;
                            }
                            catch
                            {
                                // Wear failed, continue to other methods
                            }
                        }

                        if (__result.inventory?.innerContainer.TryAdd(helmet) == true)
                        {
                            return;
                        }

                        GenPlace.TryPlaceThing(helmet, __result.PositionHeld, __result.MapHeld, ThingPlaceMode.Near);
                    }
                }
            }
        }

        private static Color DarkenColor(Color original, float factor)
        {
            float r = Mathf.Clamp01(original.r * factor);
            float g = Mathf.Clamp01(original.g * factor);
            float b = Mathf.Clamp01(original.b * factor);
            return new Color(r, g, b, original.a);
        }
    }
}




