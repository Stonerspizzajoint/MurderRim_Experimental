// WorkerDronesMod/Patches/Patch_Recipe_RemoveArtificialBodyPart_ApplyOnPawn.cs
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using VREAndroids;                  // for Recipe_RemoveArtificialBodyPart

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Recipe_RemoveArtificialBodyPart), nameof(Recipe_RemoveArtificialBodyPart.ApplyOnPawn))]
    static class Patch_Recipe_RemoveArtificialBodyPart_ApplyOnPawn
    {
        static void Postfix(Pawn pawn,
                            BodyPartRecord part,
                            Pawn billDoer,
                            List<Thing> ingredients,
                            Bill bill)
        {
            // Only flag when:
            // 1) It's the VRE stomach‐removal recipe
            // 2) It's the stomach part
            // 3) The pawn actually has the Solver gene
            if (bill.recipe == VREA_DefOf.VREA_RemoveArtificialPart
                && part.def == MD_DefOf.Stomach
                && SolverGeneUtility.HasSolver(pawn))
            {
                SurgerySafetyUtility.MarkStomachRemoved(pawn);
            }
        }
    }
}





