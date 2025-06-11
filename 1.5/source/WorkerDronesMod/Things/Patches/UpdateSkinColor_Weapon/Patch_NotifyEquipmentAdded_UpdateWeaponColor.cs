using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "Notify_EquipmentAdded")]
    public static class Patch_NotifyEquipmentAdded_UpdateWeaponColor
    {
        static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            // Validate that we have a pawn and the equipment.
            Pawn pawn = __instance?.pawn;
            if (pawn == null || eq == null)
                return;

            // Check if this equipment has our marker component.
            if (eq.TryGetComp<Comp_WeaponColorUpdaterMarker>() == null)
                return;

            // Ensure the pawn has a story (which stores SkinColor).
            if (pawn.story == null)
                return;

            Color skinColor = pawn.story.SkinColor;

            // Retrieve the GraphicData from the equipment's ThingDef.
            GraphicData gd = eq.def.graphicData;
            if (gd != null)
            {
                // Build a new graphic using the pawn's skin color.
                Graphic newGraphic = GraphicDatabase.Get<Graphic_Single>(
                    gd.texPath,
                    eq.Graphic.Shader,
                    gd.drawSize,
                    skinColor,           // primary color set to pawn's skin color
                    eq.DrawColorTwo,     // secondary color remains unchanged
                    gd,
                    null);

                // Use reflection to set the private graphic field (commonly "graphicInt") so the change takes effect.
                FieldInfo graphicIntField = typeof(Thing).GetField("graphicInt", BindingFlags.Instance | BindingFlags.NonPublic);
                if (graphicIntField != null)
                {
                    graphicIntField.SetValue(eq, newGraphic);
                }
                else
                {
                    Log.Error("Patch_NotifyEquipmentAdded_UpdateWeaponColor: graphicInt field not found.");
                }
            }
            else
            {
                // Fallback: update the DrawColor property if no GraphicData is available.
                eq.DrawColor = skinColor;
            }
        }
    }
}


