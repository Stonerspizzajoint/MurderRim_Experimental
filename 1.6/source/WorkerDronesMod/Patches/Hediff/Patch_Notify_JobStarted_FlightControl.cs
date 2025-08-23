using HarmonyLib;
using Verse;
using RimWorld;
using System.Reflection;
using Verse.AI;

namespace WorkerDronesMod.Patches
{

    [HarmonyPatch(typeof(Pawn_FlightTracker), nameof(Pawn_FlightTracker.Notify_JobStarted))]
    public static class Patch_Notify_JobStarted_FlightControl
    {
        static bool Prefix(object __instance, Job job)
        {
            var pawnField = typeof(Pawn_FlightTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);
            Pawn pawn = pawnField?.GetValue(__instance) as Pawn;
            if (pawn == null || job == null) return true; // run vanilla

            var genes = pawn.genes?.GenesListForReading;
            if (genes == null) return true; // run vanilla

            // Only restrict pawns with our gene extension
            bool hasFlightControlGene = false;
            bool jobAllowed = false;
            foreach (var gene in genes)
            {
                var ext = gene.def.GetModExtension<WingsFlightControl>();
                if (ext != null)
                {
                    hasFlightControlGene = true;
                    if (ext.allowedFlyingJobs != null && ext.allowedFlyingJobs.Contains(job.def))
                    {
                        jobAllowed = true;
                        break;
                    }
                }
            }

            // If pawn has our gene extension and job is NOT allowed, block flight
            if (hasFlightControlGene && !jobAllowed)
            {
                job.flying = false;
                return false; // skip vanilla method, so flight is not started
            }

            // Otherwise, run vanilla logic
            return true;
        }
    }
}

