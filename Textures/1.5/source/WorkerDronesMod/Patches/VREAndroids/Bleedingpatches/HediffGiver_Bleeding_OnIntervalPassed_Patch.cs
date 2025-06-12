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
        // keep track of which pawns we've already handled
        private static readonly HashSet<int> _processedPawnIds = new HashSet<int>();

        [HarmonyPriority(2147483647)]
        public static bool Prefix(Pawn pawn, Hediff cause)
        {
            int id = pawn.thingIDNumber;

            // if we’ve already seen this pawn, skip immediately
            if (_processedPawnIds.Contains(id))
                return false;

            // 1) WeakenedSolver gene: skip bleeding forever after this first check
            if (pawn.HasActiveGene(MD_DefOf.MD_WeakenedSolver))
            {
                if (DebugSettings.godMode)
                    Log.Message($"[HediffGiver_Bleeding] {pawn.LabelShort} has MD_WeakenedSolver; skipping bleeding hediff.");

                _processedPawnIds.Add(id);
                return false; // never run the original
            }

            // 2) NeutroamineOil gene: apply our custom hediff once, then never again
            if (pawn.HasActiveGene(MD_DefOf.MD_NeutroamineOil))
            {
                var hediffSet = pawn.health.hediffSet;
                if (hediffSet.BleedRateTotal >= 0f)
                {
                    HealthUtility.AdjustSeverity(pawn, VREA_DefOf.VREA_NeutroLoss, hediffSet.BleedRateTotal * 0.001f);
                }

                return false; // skip the vanilla bleeding
            }

            // 3) Everyone else: run the original method
            return true;
        }
    }
}

