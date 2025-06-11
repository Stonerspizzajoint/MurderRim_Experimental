using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(CompAssignableToPawn_Bed), "get_AssigningCandidates")]
    public static class Patch_CompAssignableToPawn_Bed_AssigningCandidates_AnimalBeds
    {
        static void Postfix(CompAssignableToPawn_Bed __instance, ref IEnumerable<Pawn> __result)
        {
            // Get the bed building.
            if (__instance.parent is Building_Bed bed && bed.def.building != null)
            {
                // Determine whether the bed qualifies as an animal bed.
                // It qualifies if either the bed isn't marked as humanlike, or its defName starts with "AnimalBedFurnitureBase".
                bool isAnimalBed = (!bed.def.building.bed_humanlike) || bed.def.defName.StartsWith("AnimalBedFurnitureBase");
                if (isAnimalBed)
                {
                    // Get any free colonists that have the custom component allowing them to use animal beds.
                    // (Usually, animal candidate list would normally only have non-mutant animals.)
                    var colonistsWithComp = bed.Map.mapPawns.FreeColonists
                        .Where(p => p.TryGetComp<CompAnimalBedUser>()?.Props.canUseAnimalBeds == true);

                    // Add them (using Union) to the original candidates.
                    __result = __result.Union(colonistsWithComp);
                }
            }
        }
    }
}
