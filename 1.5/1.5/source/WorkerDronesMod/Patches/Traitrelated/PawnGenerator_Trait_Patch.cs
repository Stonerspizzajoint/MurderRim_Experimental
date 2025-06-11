using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
    public static class PawnGenerator_Trait_Patch
    {
        public static void Postfix(Pawn __result)
        {
            if (__result == null || __result.story?.traits == null)
                return;

            // Check if the pawn has the gene "MD_WeakenedSolver"
            bool hasWeakenedSolver = __result.genes != null &&
                __result.genes.GenesListForReading.Any(g => g.def.defName == "MD_WeakenedSolver");

            if (hasWeakenedSolver)
            {
                // For testing, set chance to 100% (1f); change to 0.4f (40%) once confirmed working.
                float chance = 0.4f;
                if (Rand.Value < chance)
                {
                    TraitDef manicTrait = TraitDef.Named("MD_ManicBloodlust");
                    if (!__result.story.traits.HasTrait(manicTrait))
                    {
                        __result.story.traits.GainTrait(new Trait(manicTrait));
                    }
                }
            }
        }
    }
}





