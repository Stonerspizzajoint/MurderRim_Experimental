using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(JobDriver_AttackStatic), "MakeNewToils")]
    public static class JobDriver_AttackStatic_MakeNewToils_Patch
    {
        public static IEnumerable<Toil> Postfix(IEnumerable<Toil> original, JobDriver_AttackStatic __instance)
        {
            // If overheating protection is disabled, return the original toils unchanged.
            if (!WorkerDronesModSettings.OverheatingProtectionEnabled)
            {
                foreach (var toil in original)
                    yield return toil;
                yield break;
            }

            // Create a Toil that checks for high heat before proceeding.
            Toil heatCheckToil = new Toil();
            heatCheckToil.initAction = () =>
            {
                Pawn shooter = __instance.pawn;
                // Retrieve the pawn's primary weapon from their equipment.
                ThingWithComps weapon = shooter.equipment?.Primary;
                if (weapon != null)
                {
                    ThingComp_HeatRestriction heatComp = weapon.TryGetComp<ThingComp_HeatRestriction>();
                    if (heatComp != null && heatComp.IsHeatTooHigh(shooter))
                    {
                        // End the job early if heat is too high.
                        __instance.EndJobWith(JobCondition.Incompletable);
                        Messages.Message("Cannot shoot: heat level too high!", MessageTypeDefOf.RejectInput);
                    }
                }
            };
            heatCheckToil.defaultCompleteMode = ToilCompleteMode.Instant;

            // Prepend the heat-check toil before the original toils.
            yield return heatCheckToil;
            foreach (var toil in original)
            {
                yield return toil;
            }
        }
    }

    [HarmonyPatch(typeof(JobDriver), "DriverTick")]
    public static class JobDriver_DriverTick_Patch
    {
        // Store the last tick a pawn had its job cancelled due to heat.
        private static readonly Dictionary<int, int> lastOverheatingCancelTicks = new Dictionary<int, int>();
        // Customize the cooldown window (in ticks) here.
        private const int OverheatingCooldownTicks = 5;

        public static void Postfix(JobDriver __instance)
        {
            // Only process if overheating protection is enabled.
            if (!WorkerDronesModSettings.OverheatingProtectionEnabled)
                return;

            // Only proceed if the current job is an attack job of the type we want to protect.
            if (!(__instance is JobDriver_AttackStatic attackStatic))
                return;

            Pawn shooter = attackStatic.pawn;
            if (shooter == null)
                return;

            // *** Gene Check ***
            // If the pawn isn't one of our intended targets (eg. doesn't have Gene_NeutroamineOil), skip.
            var neutroGene = shooter.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
            if (neutroGene == null)
                return;

            // Retrieve the pawn's primary weapon and its heat component.
            ThingWithComps weapon = shooter.equipment?.Primary;
            if (weapon == null)
                return;

            ThingComp_HeatRestriction heatComp = weapon.TryGetComp<ThingComp_HeatRestriction>();
            if (heatComp == null)
                return;

            int currentTick = Find.TickManager.TicksGame;
            // Check if we have cooled down from the last cancellation.
            if (lastOverheatingCancelTicks.TryGetValue(shooter.thingIDNumber, out int lastTick))
            {
                if (currentTick - lastTick < OverheatingCooldownTicks)
                    return;
            }

            if (heatComp.IsHeatTooHigh(shooter))
            {
                attackStatic.EndJobWith(JobCondition.Incompletable);
                // Record the cancellation tick.
                lastOverheatingCancelTicks[shooter.thingIDNumber] = currentTick;
                // Optionally, show a message (if desired).
                Messages.Message("Shooting canceled: heat level too high!", MessageTypeDefOf.RejectInput);
            }
        }
    }
}



