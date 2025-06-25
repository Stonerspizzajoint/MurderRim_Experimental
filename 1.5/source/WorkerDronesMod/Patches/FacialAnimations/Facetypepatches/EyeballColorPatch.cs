using HarmonyLib;
using Verse;
using FacialAnimation;
using WorkerDronesMod;
using System.Linq;

namespace WorkerDronesMod.Patches.FacialAnimations
{
    [HarmonyPatch(typeof(EyeballControllerComp), nameof(EyeballControllerComp.LoadTextures))]
    static class EyeballColorPatch
    {
        static void Postfix(EyeballControllerComp __instance)
        {
            var pawn = __instance.parent as Pawn;
            var ext = pawn?.genes?.GenesListForReading
                .Select(g => g.def.GetModExtension<GeneForcedFacetypesExtension>())
                .FirstOrDefault(e => e != null);
            if (ext == null) return;

            if (ext.EyeColorMatchesSkinColor)
            {
                __instance.FaceColor = pawn.story.SkinColor;
                __instance.FaceSecondColor = pawn.story.SkinColor;
            }
        }
    }
}




