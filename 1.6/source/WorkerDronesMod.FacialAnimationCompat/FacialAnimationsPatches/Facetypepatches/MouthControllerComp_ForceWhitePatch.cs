using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FacialAnimation;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace WorkerDronesMod.FacialAnimationCompat
{
    [HarmonyPatch]
    public static class MouthControllerComp_ForceWhitePatch
    {
        // tell Harmony which two methods to patch
        static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(MouthControllerComp), "CompDefaultCurrentColor");
            yield return AccessTools.Method(typeof(MouthControllerComp), "CompDefaultResetColor");
        }

        // if our gene is present & flag == true, return white and skip original
        static bool Prefix(MouthControllerComp __instance, ref Color __result)
        {
            if (!ModsConfig.BiotechActive)
                return true;  // let vanilla run

            // grab the private pawn field
            var pawn = AccessTools.FieldRefAccess<MouthControllerComp, Pawn>(__instance, "pawn");
            if (pawn?.genes?.GenesListForReading == null)
                return true;

            // find our gene
            var gene = pawn.genes.GenesListForReading
                           .FirstOrDefault(g => g.def.HasModExtension<GeneForcedFacetypesExtension>());
            var ext = gene?.def.GetModExtension<GeneForcedFacetypesExtension>();
            if (ext?.forceMouthColorWhite == true)
            {
                __result = Color.white;
                return false;  // skip original: always white
            }

            return true;  // fall back to vanilla
        }
    }


}
