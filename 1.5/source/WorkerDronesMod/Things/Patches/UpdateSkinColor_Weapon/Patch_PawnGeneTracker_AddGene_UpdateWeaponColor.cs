using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn_GeneTracker), "AddGene", new[] { typeof(Gene), typeof(bool) })]
    public static class Patch_PawnGeneTracker_AddGene_UpdateWeaponColor
    {
        static void Postfix(Pawn_GeneTracker __instance, Gene gene, bool addAsXenogene)
        {
            Pawn pawn = __instance?.pawn;
            if (pawn == null)
                return;

            // Get the pawn's skin color (fallback to pawn.DrawColor if no story exists).
            Color newSkinColor = (pawn.story != null) ? pawn.story.SkinColor : pawn.DrawColor;

            // Loop through all equipment on the pawn.
            if (pawn.equipment != null)
            {
                foreach (ThingWithComps eq in pawn.equipment.AllEquipmentListForReading)
                {
                    // Check if the equipment has our marker component.
                    if (eq.TryGetComp<Comp_WeaponColorUpdaterMarker>() != null)
                    {
                        UpdateWeaponColor(eq, newSkinColor);
                    }
                }
            }
        }

        private static void UpdateWeaponColor(ThingWithComps eq, Color skinColor)
        {
            GraphicData gd = eq.def.graphicData;
            if (gd != null)
            {
                Graphic newGraphic = GraphicDatabase.Get<Graphic_Single>(
                    gd.texPath,
                    eq.Graphic.Shader,
                    gd.drawSize,
                    skinColor,           // primary color set to pawn's skin color
                    eq.DrawColorTwo,     // secondary color remains unchanged
                    gd,
                    null);

                FieldInfo graphicIntField = typeof(Thing).GetField("graphicInt", BindingFlags.Instance | BindingFlags.NonPublic);
                if (graphicIntField != null)
                {
                    graphicIntField.SetValue(eq, newGraphic);
                }
                else
                {
                    Log.Error("Patch_PawnGeneTracker_AddGene_UpdateWeaponColor: graphicInt field not found on equipment.");
                }
            }
            else
            {
                eq.DrawColor = skinColor;
            }
        }
    }
}





