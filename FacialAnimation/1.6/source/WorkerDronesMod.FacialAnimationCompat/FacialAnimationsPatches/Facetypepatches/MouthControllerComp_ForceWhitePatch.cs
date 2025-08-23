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
        // Cache method info for performance
        private static readonly MethodInfo CompDefaultCurrentColor = AccessTools.Method(typeof(MouthControllerComp), "CompDefaultCurrentColor");
        private static readonly MethodInfo CompDefaultResetColor = AccessTools.Method(typeof(MouthControllerComp), "CompDefaultResetColor");

        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return CompDefaultCurrentColor;
            yield return CompDefaultResetColor;
        }

        static bool Prefix(MouthControllerComp __instance, ref Color __result)
        {
            if (!ModsConfig.BiotechActive)
                return true;

            // Direct field access (faster than reflection every call)
            var pawnField = __instance.GetType().GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var pawn = pawnField?.GetValue(__instance) as Pawn;
            if (pawn == null || pawn.genes == null)
                return true;

            // Fast loop, avoid LINQ
            GeneForcedFacetypesExtension ext = null;
            foreach (var gene in pawn.genes.GenesListForReading)
            {
                ext = gene.def.GetModExtension<GeneForcedFacetypesExtension>();
                if (ext != null && ext.forceMouthColorWhite)
                    break;
            }

            if (ext != null && ext.forceMouthColorWhite)
            {
                __result = Color.white;
                return false;
            }

            return true;
        }
    }


}
