using HarmonyLib;
using UnityEngine;
using Verse;
using System.Linq;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn), "DrawAt")]
    public static class Patch_Pawn_DrawAt_Telekinesis
    {
        public static void Postfix(Pawn __instance, Vector3 drawLoc)
        {
            var abilityTracker = __instance.abilities;
            if (abilityTracker == null || abilityTracker.abilities == null)
                return;

            var abilityComp = abilityTracker.abilities
                .Where(a => a != null && a.comps != null)
                .SelectMany(a => a.comps.Where(c => c != null))
                .OfType<Comp_AbilityTelekinesisEffect>()
                .FirstOrDefault();

            if (abilityComp != null && abilityComp.IsHoldingThing)
            {
                abilityComp.DrawHeldThings();
            }
        }
    }
    [HarmonyPatch(typeof(Pawn), "Tick")]
    public static class Patch_Pawn_Tick_Telekinesis
    {
        public static void Postfix(Pawn __instance)
        {
            var abilityTracker = __instance.abilities;
            if (abilityTracker == null || abilityTracker.abilities == null)
                return;

            var abilityComp = abilityTracker.abilities
                .Where(a => a != null && a.comps != null)
                .SelectMany(a => a.comps.Where(c => c != null))
                .OfType<Comp_AbilityTelekinesisEffect>()
                .FirstOrDefault();

            // Only tick if there is a held object
            if (abilityComp != null && abilityComp.IsHoldingThing)
            {
                abilityComp.CompTick();
            }
        }
    }
}

