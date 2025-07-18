using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(HediffGiver_Bleeding), "OnIntervalPassed")]
    public static class HediffGiver_Bleeding_OnIntervalPassed_Patch
    {

        [HarmonyPriority(2147483647)]
        public static bool Prefix(Pawn pawn, Hediff cause)
        {

            // 1) NeutroamineOil gene: apply our custom hediff once, then never again
            if (pawn.HasActiveGene(MD_DefOf.MD_NeutroamineOil))
            {
                var hediffSet = pawn.health.hediffSet;
                if (hediffSet.BleedRateTotal >= 0f)
                {
                    HealthUtility.AdjustSeverity(pawn, VREA_DefOf.VREA_NeutroLoss, hediffSet.BleedRateTotal * 0.001f);
                }

                return false; // skip the vanilla bleeding
            }

            // 2) Everyone else: run the original method
            return true;
        }
    }
}

