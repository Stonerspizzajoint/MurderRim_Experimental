using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Hediff_Injury), "BleedRate", MethodType.Getter)]
    public static class Hediff_Injury_BleedRate_Patch
    {
        // Highest priority so we override everything
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(ref float __result, Hediff_Injury __instance)
        {
            var neutroGenes = GeneDefHelper.GetGeneDefAndAlternative(
                MD_DefOf.MD_NeutroamineOil,
                MD_DefOf.VREA_MD_NeutroamineOil
            ).ToArray();

            if (GeneDefHelper.PawnHasAnyGene(__instance.pawn, neutroGenes))
            {
                __result = CustomBleedRate(__instance);
                return false;
            }
            return true;
        }

        private static float CustomBleedRate(Hediff_Injury inj)
        {
            if (inj.pawn.Dead || inj.IsTended() || inj.IsPermanent())
                return 0f;

            float rate = inj.Severity * inj.def.injuryProps.bleedRate;
            if (inj.Part != null)
                rate *= inj.Part.def.bleedRate;

            return rate;
        }
    }
}


