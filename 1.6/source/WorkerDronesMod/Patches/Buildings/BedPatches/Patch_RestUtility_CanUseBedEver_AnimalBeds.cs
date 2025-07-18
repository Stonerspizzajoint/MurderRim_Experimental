using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(RestUtility), "CanUseBedEver")]
    public static class Patch_RestUtility_CanUseBedEver_AnimalBeds
    {
        static bool Prefix(Pawn p, ThingDef bedDef, ref bool __result)
        {
            if (bedDef?.building != null)
            {
                // Consider it an animal bed if:
                // 1. The building isn’t marked as humanlike, or
                // 2. The defName starts with "AnimalBedFurnitureBase".
                bool isAnimalBed = (!bedDef.building.bed_humanlike) ||
                                   bedDef.defName.StartsWith("AnimalBedFurnitureBase");

                if (isAnimalBed)
                {
                    var comp = p?.TryGetComp<CompAnimalBedUser>();
                    if (comp != null && comp.Props.canUseAnimalBeds)
                    {
                        __result = true;
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
