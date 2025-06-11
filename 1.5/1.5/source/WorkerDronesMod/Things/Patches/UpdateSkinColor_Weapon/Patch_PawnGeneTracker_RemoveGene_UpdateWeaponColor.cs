using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn_GeneTracker), "RemoveGene", new[] { typeof(Gene) })]
    public static class Patch_PawnGeneTracker_RemoveGene_UpdateWeaponColor
    {
        static void Postfix(Pawn_GeneTracker __instance, Gene gene)
        {
            Pawn pawn = __instance?.pawn;
            if (pawn == null)
                return;

            // Retrieve the pawn's current skin color (fallback to pawn.DrawColor if no story).
            Color newSkinColor = (pawn.story != null) ? pawn.story.SkinColor : pawn.DrawColor;

            // Loop through each equipment item on the pawn.
            if (pawn.equipment != null)
            {
                foreach (ThingWithComps eq in pawn.equipment.AllEquipmentListForReading)
                {
                    // Check if this equipment has our marker component.
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
                    skinColor,           // new primary color: pawn's skin color
                    eq.DrawColorTwo,     // keep secondary color unchanged
                    gd,
                    null);

                // Use reflection to update the private graphic field.
                FieldInfo graphicIntField = typeof(Thing).GetField("graphicInt", BindingFlags.Instance | BindingFlags.NonPublic);
                if (graphicIntField != null)
                {
                    graphicIntField.SetValue(eq, newGraphic);
                }
                else
                {
                    Log.Error("Patch_PawnGeneTracker_RemoveGene_UpdateWeaponColor: graphicInt field not found on equipment.");
                }
            }
            else
            {
                eq.DrawColor = skinColor;
            }
        }
    }
}

