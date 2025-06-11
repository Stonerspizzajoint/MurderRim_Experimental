using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Building_NeutroCasket))]
    public static class Patch_Building_NeutroCasket_OilFromFuel
    {
        // Prefix patch to replace NeutroLoss healing with oil refill
        [HarmonyPrefix]
        [HarmonyPatch("Tick")]
        public static bool Tick_Prefix(Building_NeutroCasket __instance)
        {
            // Run every 60 ticks if there's power and at least 1 fuel
            if (__instance.IsHashIntervalTick(60) && __instance.compPower.PowerOn && __instance.compRefuelable.Fuel >= 1f)
            {
                foreach (Pawn pawn in __instance.CurOccupants)
                {
                    // Only for androids with the solver gene
                    if (SolverGeneUtility.HasSolver(pawn))
                    {
                        var gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
                        if (gene != null && gene.Value < gene.MaxForDisplay)
                        {
                            // Consume 1 fuel, add oil per unit
                            __instance.compRefuelable.ConsumeFuel(1f);
                            gene.Value += RefuelUtils.OilPerNeutroamineUnit;
                            gene.Value = Mathf.Min(gene.Value, gene.MaxForDisplay);
                        }
                    }
                }
                return false; // skip original Tick
            }
            return true; // allow original Tick if conditions aren't met
        }
    }
}

