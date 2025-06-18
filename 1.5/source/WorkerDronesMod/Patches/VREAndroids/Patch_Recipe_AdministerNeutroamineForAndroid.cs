using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Recipe_AdministerNeutroamineForAndroid))]
    public static class Patch_Recipe_AdministerNeutroamineForAndroid
    {
        // Patch AvailableOnNow to allow for solver gene with low oil
        [HarmonyPrefix]
        [HarmonyPatch("AvailableOnNow")]
        public static bool AvailableOnNow_Prefix(Thing thing, BodyPartRecord part, ref bool __result)
        {
            if (thing is Pawn pawn && SolverGeneUtility.HasSolver(pawn))
            {
                var gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
                if (gene != null && gene.Value < gene.MaxForDisplay)
                {
                    __result = true;
                    return false; // skip original
                }
            }
            // Otherwise, use original
            return true;
        }

        // Patch GetIngredientCount to use oil values if Solver is present and oil is below max
        [HarmonyPrefix]
        [HarmonyPatch("GetIngredientCount")]
        public static bool GetIngredientCount_Prefix(IngredientCount ing, Bill bill, ref float __result)
        {
            if (bill?.billStack?.billGiver is Pawn pawn && SolverGeneUtility.HasSolver(pawn))
            {
                var gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
                if (gene != null && gene.Value < gene.MaxForDisplay)
                {
                    float missingOil = gene.MaxForDisplay - gene.Value;
                    __result = Mathf.Ceil(missingOil / RefuelUtils.OilPerNeutroamineUnit);
                    return false; // Skip original method
                }
            }
            return true; // Run original method if no solver gene or not needed
        }

        // Patch ApplyOnPawn to refuel oil if Solver is present
        [HarmonyPrefix]
        [HarmonyPatch("ApplyOnPawn")]
        public static bool ApplyOnPawn_Prefix(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (SolverGeneUtility.HasSolver(pawn))
            {
                var gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
                if (gene != null)
                {
                    int totalNeutro = ingredients.Where(t => t.def == VREA_DefOf.Neutroamine).Sum(t => t.stackCount);
                    gene.Value += totalNeutro * RefuelUtils.OilPerNeutroamineUnit;
                    gene.Value = Mathf.Min(gene.Value, gene.MaxForDisplay);

                    foreach (var thing in ingredients)
                        thing.Destroy(DestroyMode.Vanish);

                    return false; // Skip original ApplyOnPawn
                }
            }
            return true; // Fall back to original
        }
    }
}


