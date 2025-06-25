using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;
using FacialAnimation;
using WorkerDronesMod;

namespace WorkerDronesMod.Patches.FacialAnimations
{
    // Patch the protected override Color CompDefaultResetColor()
    [HarmonyPatch(typeof(LidControllerComp), "CompDefaultResetColor")]
    static class Lid_DefaultResetColor_Patch
    {
        static bool Prefix(LidControllerComp __instance, ref Color __result)
        {
            var pawn = __instance.parent as Pawn;
            var ext = pawn?.genes?.GenesListForReading
                .Select(g => g.def.GetModExtension<GeneForcedFacetypesExtension>())
                .FirstOrDefault(e => e != null);
            if (ext?.LidColorMatchesSkinColor == true)
            {
                __result = pawn.story.SkinColor;
                return false;  // skip original
            }
            return true;       // run original
        }
    }

    // Patch the protected override Color CompDefaultCurrentColor()
    [HarmonyPatch(typeof(LidControllerComp), "CompDefaultCurrentColor")]
    static class Lid_DefaultCurrentColor_Patch
    {
        static bool Prefix(LidControllerComp __instance, ref Color __result)
        {
            var pawn = __instance.parent as Pawn;
            var ext = pawn?.genes?.GenesListForReading
                .Select(g => g.def.GetModExtension<GeneForcedFacetypesExtension>())
                .FirstOrDefault(e => e != null);
            if (ext?.LidColorMatchesSkinColor == true)
            {
                __result = pawn.story.SkinColor;
                return false;
            }
            return true;
        }
    }
}



