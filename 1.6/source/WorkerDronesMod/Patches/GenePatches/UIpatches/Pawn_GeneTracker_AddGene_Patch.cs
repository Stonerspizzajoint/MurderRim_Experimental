using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    // Patch the private AddGene(Gene, bool) method so that any existing gene with the same def is removed.
    [HarmonyPatch(typeof(Pawn_GeneTracker), "AddGene", new Type[] { typeof(Gene), typeof(bool) })]
    public static class Pawn_GeneTracker_AddGene_Patch
    {
        // Prefix runs before the gene is added.
        public static void Prefix(Pawn_GeneTracker __instance, Gene gene)
        {
            // Only process if the pawn exists and is an android.
            if (__instance.pawn == null || !__instance.pawn.IsAndroid())
                return;

            // Use the read-only GenesListForReading to check for existing genes with the same definition.
            var overriddenGenes = __instance.GenesListForReading
                .Where(g => g.def == gene.def)
                .ToList();

            // Remove any gene that would be overridden by the new one.
            foreach (var oldGene in overriddenGenes)
            {
                __instance.RemoveGene(oldGene);
            }
        }
    }
}
