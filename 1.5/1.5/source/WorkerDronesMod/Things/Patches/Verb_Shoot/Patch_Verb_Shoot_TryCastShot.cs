using HarmonyLib;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Verb_Shoot))]
    [HarmonyPatch("TryCastShot")]
    public static class Patch_Verb_Shoot_TryCastShot
    {
        // Postfix patch: runs after TryCastShot.
        public static void Postfix(Verb_Shoot __instance, bool __result)
        {
            // Only proceed if the shot was successfully fired.
            if (!__result)
                return;

            // Get the weapon (ThingWithComps) that fired the shot.
            // The caster is often the pawn, so we need to retrieve the equipment.
            if (!(__instance.caster is Pawn shooter))
                return;

            // Try to get the weapon the pawn is using.
            // This might depend on your specific weapon setup.
            ThingWithComps equipment = shooter.equipment?.Primary;
            if (equipment == null)
                return;

            // Attempt to retrieve our heat comp.
            CompHeatPerShot heatComp = equipment.TryGetComp<CompHeatPerShot>();
            if (heatComp != null)
            {
                heatComp.AddHeatOnShot();
            }
        }
    }
}

