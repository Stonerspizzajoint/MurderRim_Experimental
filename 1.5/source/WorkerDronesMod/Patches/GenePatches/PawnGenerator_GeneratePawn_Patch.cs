using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(PawnGenerator))]
    [HarmonyPatch(nameof(PawnGenerator.GeneratePawn))]
    [HarmonyPatch(new Type[] { typeof(PawnGenerationRequest) })]
    public static class PawnGenerator_GeneratePawn_Patch
    {
        public static void Postfix(Pawn __result)
        {
            // Get the gene definition first
            GeneDef droneGene = DefDatabase<GeneDef>.GetNamed("MD_DroneBody", false);

            // Check if the gene exists and pawn has it
            if (droneGene != null &&
                __result.genes?.HasActiveGene(droneGene) == true &&
                Rand.Value < 0.25f)
            {
                // Create helmet
                ThingDef helmetDef = ThingDef.Named("MD_Headgear_Hardhat");
                ThingDef steel = ThingDef.Named("Steel");

                if (helmetDef != null && steel != null)
                {
                    Apparel helmet = ThingMaker.MakeThing(helmetDef, steel) as Apparel;

                    if (helmet != null)
                    {
                        // Try to equip
                        if (__result.apparel != null)
                        {
                            try
                            {
                                __result.apparel.Wear(helmet, false);
                                return;
                            }
                            catch
                            {
                                // Wear failed, continue to other methods
                            }
                        }

                        // Try inventory
                        if (__result.inventory?.innerContainer.TryAdd(helmet) == true)
                        {
                            return;
                        }

                        // Drop nearby
                        GenPlace.TryPlaceThing(helmet, __result.PositionHeld, __result.MapHeld, ThingPlaceMode.Near);
                    }
                }
            }
        }
    }
}



