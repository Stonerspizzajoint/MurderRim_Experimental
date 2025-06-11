using System;
using System.Collections;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    // Patch the RemoveGene method of Pawn_GeneTracker.
    // This ensures that when a gene is removed, we clear the cached gene list and remove the gene from both underlying lists.
    [HarmonyPatch(typeof(Pawn_GeneTracker), "RemoveGene", new Type[] { typeof(Gene) })]
    public static class Pawn_GeneTracker_RemoveGene_Patch
    {
        public static void Postfix(Pawn_GeneTracker __instance, Gene gene)
        {
            // Clear the cached gene list to force a rebuild.
            var cachedGenesField = AccessTools.Field(typeof(Pawn_GeneTracker), "cachedGenes");
            if (cachedGenesField != null)
            {
                cachedGenesField.SetValue(__instance, null);
            }

            // Remove the gene from the xenogenes list if it exists.
            var xenogenesField = AccessTools.Field(typeof(Pawn_GeneTracker), "xenogenes");
            if (xenogenesField != null)
            {
                var xenogenes = xenogenesField.GetValue(__instance) as IList;
                if (xenogenes != null && xenogenes.Contains(gene))
                {
                    xenogenes.Remove(gene);
                }
            }

            // Remove the gene from the endogenes list if it exists.
            var endogenesField = AccessTools.Field(typeof(Pawn_GeneTracker), "endogenes");
            if (endogenesField != null)
            {
                var endogenes = endogenesField.GetValue(__instance) as IList;
                if (endogenes != null && endogenes.Contains(gene))
                {
                    endogenes.Remove(gene);
                }
            }
        }
    }
}
