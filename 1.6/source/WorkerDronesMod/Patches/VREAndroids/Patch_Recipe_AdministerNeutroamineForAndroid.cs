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
            if (thing is Pawn pawn)
            {
                var geneBasic = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
                if (geneBasic != null && geneBasic.Oil < geneBasic.InitialResourceMax)
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
            if (bill?.billStack?.billGiver is Pawn pawn)
            {
                var geneBasic = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
                if (geneBasic != null && geneBasic.Oil < geneBasic.InitialResourceMax)
                {
                    float missingOil = geneBasic.InitialResourceMax - geneBasic.Oil;
                    float oilPerNeutro = RefuelUtils.OilPerNeutroamineUnit; // Or 10f if not defined
                    __result = Mathf.Ceil(missingOil / oilPerNeutro);
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
            var geneBasic = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (geneBasic != null)
            {
                int totalNeutro = ingredients.Where(t => t.def == VREA_DefOf.Neutroamine).Sum(t => t.stackCount);
                float oilPerNeutro = RefuelUtils.OilPerNeutroamineUnit; // Or 10f if not defined
                geneBasic.Oil += totalNeutro * oilPerNeutro;
                geneBasic.Oil = Mathf.Min(geneBasic.Oil, geneBasic.InitialResourceMax);

                foreach (var thing in ingredients)
                    thing.Destroy(DestroyMode.Vanish);

                return false; // Skip original ApplyOnPawn
            }
            return true; // Fall back to original
        }
    }
}


