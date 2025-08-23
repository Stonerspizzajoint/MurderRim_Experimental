using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Building_NeutroCasket), nameof(Building_NeutroCasket.TickInterval))]
    public static class Patch_Building_NeutroCasket_OilFromFuel
    {
        // Postfix runs after the original Tick() has healed NeutroLoss and consumed 1 fuel per pawn
        [HarmonyPostfix]
        public static void Tick_Postfix(Building_NeutroCasket __instance)
        {
            // Mirror the vanilla guard so we only try to refill oil at the same cadence
            if (!__instance.IsHashIntervalTick(60)
                || __instance.compPower?.PowerOn != true
                // compRefuelable.Fuel has already been decreased by vanilla for each pawn healed,
                // so require at least 1 left
                || __instance.compRefuelable?.Fuel < 1f)
            {
                return;
            }

            foreach (Pawn pawn in __instance.CurOccupants)
            {
                // Only androids with the solver gene gain oil
                var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
                if (gene != null && gene.Oil < gene.InitialResourceMax)
                {
                    // Now consume 1 more fuel for oil, *then* top up the gene
                    __instance.compRefuelable.ConsumeFuel(1f);

                    // You can use a constant or a config value for oil per Neutroamine
                    float oilPerNeutroamine = 10f; // Or RefuelUtils.OilPerNeutroamineUnit if you have it

                    gene.Oil += oilPerNeutroamine;
                    gene.Oil = Mathf.Min(gene.Oil, gene.InitialResourceMax);
                }
            }
        }
    }
}



