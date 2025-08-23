using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;

namespace WorkerDronesMod.Patches
{

    [HarmonyPatch(typeof(Pawn_FlightTracker), "CanEverFly", MethodType.Getter)]
    public static class Patch_CanEverFly_Hediff
    {
        private static readonly FieldInfo pawnField = typeof(Pawn_FlightTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);

        static void Postfix(object __instance, ref bool __result)
        {
            Pawn pawn = pawnField?.GetValue(__instance) as Pawn;
            if (pawn?.genes != null && pawn.CurJob != null)
            {
                foreach (var gene in pawn.genes.GenesListForReading)
                {
                    var ext = gene.def.GetModExtension<WingsFlightControl>();
                    if (ext != null && ext.CanFly && ext.allowedFlyingJobs != null)
                    {
                        // Check vacuum biome restriction
                        bool inVacuum = pawn.MapHeld?.Biome?.inVacuum == true;
                        if (!ext.CanFlyInVaccuum && inVacuum)
                            continue;

                        // Flight is only allowed if at least one of the hediffs is present
                        bool hasLandedHediff = ext.LandedHediff != null && pawn.health.hediffSet.HasHediff(ext.LandedHediff);
                        bool hasFlyingHediff = ext.FlyingHediff != null && pawn.health.hediffSet.HasHediff(ext.FlyingHediff);

                        if (!(hasLandedHediff || hasFlyingHediff))
                            __result = false;
                            return; // No hediffs, cannot fly

                        // Only allow flight if job is allowed
                        bool jobAllowed = ext.allowedFlyingJobs.Contains(pawn.CurJob.def);
                        if (jobAllowed)
                        {
                            __result = true;
                            return;
                        }
                    }
                }
            }
            // Do not set __result for other pawns; vanilla logic remains
        }
    }
}

