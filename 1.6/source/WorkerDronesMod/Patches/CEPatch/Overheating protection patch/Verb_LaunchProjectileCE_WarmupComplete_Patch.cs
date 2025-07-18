using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace WorkerDronesMod.Patches.CE
{
    // Patch the CE launch‑verb warmup completion via a postfix.
    // This allows the original WarmupComplete to finish its setup,
    // then cancels the shot if the heat level is too high.
    [HarmonyPatch]
    public static class Verb_LaunchProjectileCE_WarmupComplete_Patch
    {
        // Only patch if CE is loaded
        static MethodBase TargetMethod()
        {
            var ceType = AccessTools.TypeByName("CombatExtended.Verb_LaunchProjectileCE");
            if (ceType == null) return null;
            return AccessTools.Method(ceType, "WarmupComplete");
        }

        static void Postfix(object __instance)
        {
            // Get the CasterPawn from the instance.
            var pawnProp = __instance.GetType().GetProperty("CasterPawn", BindingFlags.Public | BindingFlags.Instance);
            Pawn shooter = pawnProp?.GetValue(__instance) as Pawn;
            if (shooter == null) return;

            // Obtain the primary weapon.
            ThingWithComps weapon = shooter.equipment?.Primary as ThingWithComps;
            if (weapon == null) return;

            // Check your heat‐restriction component.
            var heatComp = weapon.TryGetComp<ThingComp_HeatRestriction>();
            if (heatComp != null && heatComp.IsHeatTooHigh(shooter))
            {
                // (Optional) If the original method sets up data (e.g. burstShotsLeft),
                // you might want to set those fields to safe values here via reflection.

                // End the job now that initialization is complete.
                shooter.jobs?.curDriver?.EndJobWith(JobCondition.Incompletable);

                // Only show a message if this pawn is a colonist.
                if (shooter.IsColonist)
                {
                    Messages.Message("Cannot shoot: heat level too high!", MessageTypeDefOf.RejectInput);
                }
            }
        }
    }

    // Patch the CE shoot‑verb TryCastShot as a fallback,
    // again using a postfix to let any required initialization occur first.
    [HarmonyPatch]
    public static class Verb_ShootCE_TryCastShot_Patch
    {
        static MethodBase TargetMethod()
        {
            var ceType = AccessTools.TypeByName("CombatExtended.Verb_ShootCE");
            if (ceType == null) return null;
            return AccessTools.Method(ceType, "TryCastShot");
        }

        // Assuming TryCastShot returns a bool,
        // we capture its result via the __result parameter.
        static void Postfix(object __instance, ref bool __result)
        {
            // Get the CasterPawn.
            var pawnProp = __instance.GetType().GetProperty("CasterPawn", BindingFlags.Public | BindingFlags.Instance);
            Pawn shooter = pawnProp?.GetValue(__instance) as Pawn;
            if (shooter == null) return;

            // Get the primary weapon.
            ThingWithComps weapon = shooter.equipment?.Primary as ThingWithComps;
            if (weapon == null) return;

            // Check the heat‐restriction component.
            var heatComp = weapon.TryGetComp<ThingComp_HeatRestriction>();
            if (heatComp != null && heatComp.IsHeatTooHigh(shooter))
            {
                // End the job—after the original TryCastShot has run.
                shooter.jobs?.curDriver?.EndJobWith(JobCondition.Incompletable);

                // Only show a message if this pawn is a colonist.
                if (shooter.IsColonist)
                {
                    Messages.Message("Cannot shoot: heat level too high!", MessageTypeDefOf.RejectInput);
                }

                // Force the shot to be considered “not cast.”
                __result = false;
            }
        }
    }
}


