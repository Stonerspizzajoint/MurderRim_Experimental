using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;
using FacialAnimation;
using WorkerDronesMod;

namespace WorkerDronesMod.Patches.FacialAnimations
{
    // Patch BrowControllerComp.CompDefaultResetColor()
    [HarmonyPatch(typeof(BrowControllerComp), "CompDefaultResetColor")]
    static class Brow_DefaultResetColor_Patch
    {
        static bool Prefix(BrowControllerComp __instance, ref Color __result)
        {
            var pawn = __instance.parent as Pawn;
            var ext = pawn?.genes?.GenesListForReading
                .Select(g => g.def.GetModExtension<GeneForcedFacetypesExtension>())
                .FirstOrDefault(e => e != null);
            if (ext?.BrowColorMatchesSkinColor == true)
            {
                __result = pawn.story.SkinColor;
                return false;  // skip original hair-color
            }
            return true;       // else run original
        }
    }

    // Patch BrowControllerComp.CompDefaultCurrentColor()
    [HarmonyPatch(typeof(BrowControllerComp), "CompDefaultCurrentColor")]
    static class Brow_DefaultCurrentColor_Patch
    {
        static bool Prefix(BrowControllerComp __instance, ref Color __result)
        {
            var pawn = __instance.parent as Pawn;
            var ext = pawn?.genes?.GenesListForReading
                .Select(g => g.def.GetModExtension<GeneForcedFacetypesExtension>())
                .FirstOrDefault(e => e != null);
            if (ext?.BrowColorMatchesSkinColor == true)
            {
                __result = pawn.story.SkinColor;
                return false;
            }
            return true;
        }
    }
}

