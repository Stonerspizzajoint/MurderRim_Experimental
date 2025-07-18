using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Building_Bed), "GetBedRestFloatMenuOption")]
    public static class Patch_Building_Bed_GetBedRestFloatMenuOption_AnimalBeds
    {
        static void Postfix(Building_Bed __instance, Pawn myPawn, ref FloatMenuOption __result)
        {
            // Check if there's no option returned,
            // and if the bed is considered an animal bed. 
            // We classify a bed as an animal bed if either:
            // 1. The bed def's building is not marked as humanlike,
            // OR 2. The bed defName starts with "AnimalBedFurnitureBase".
            if (__result == null && myPawn != null &&
                ((!__instance.def.building.bed_humanlike) || __instance.def.defName.StartsWith("AnimalBedFurnitureBase")))
            {
                var comp = myPawn.TryGetComp<CompAnimalBedUser>();
                if (comp != null && comp.Props.canUseAnimalBeds)
                {
                    // Creates an option that allows the pawn to use the bed.
                    __result = new FloatMenuOption("Use animal bed", delegate ()
                    {
                        Job job = JobMaker.MakeJob(JobDefOf.LayDown, __instance);
                        job.restUntilHealed = true;
                        myPawn.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
                    });
                }
            }
        }
    }
}
