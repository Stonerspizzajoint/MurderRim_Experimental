using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(HediffGiver_Heat), "OnIntervalPassed")]
    public static class HediffGiver_Heat_OnIntervalPassed_WorkerDrones_Patch
    {
        public static void Postfix(Pawn pawn, Hediff cause)
        {
            // Only apply to androids with your gene
            if (pawn == null || !pawn.IsAndroid())
                return;

            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene == null) return;

            var hediffDef = VREA_DefOf.VREA_Overheating;
            if (HeatUtil.IsOverheating(gene.Heat, gene.InitialResourceMax))
            {
                float overAmount = gene.Heat - gene.InitialResourceMax * 1.1f;
                float severityGain = Mathf.Max(overAmount * 0.01f, 0.01f);
                HealthUtility.AdjustSeverity(pawn, hediffDef, severityGain);
            }
            else
            {
                var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                if (hediff != null)
                {
                    float reduction = Mathf.Clamp(hediff.Severity * 0.027f, 0.0015f, 0.015f);
                    hediff.Severity -= reduction;
                }
            }
        }
    }
}
