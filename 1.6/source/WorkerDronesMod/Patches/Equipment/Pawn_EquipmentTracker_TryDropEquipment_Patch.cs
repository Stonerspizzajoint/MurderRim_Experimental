using HarmonyLib;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "TryDropEquipment")]
    public static class Pawn_EquipmentTracker_TryDropEquipment_Patch
    {
        static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps eq, ref bool __result)
        {
            if (eq == null)
                return true; // Let the game handle null eq normally.

            Pawn pawn = __instance?.pawn;
            if (pawn == null || pawn.health?.hediffSet == null)
                return true;

            // Look through the pawn's hediffs.
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                // Check by defName (make sure "GunHandHediff" matches your hediff's defName).
                if (hediff.def.defName == "GunHandHediff")
                {
                    HediffComp_GunHand comp = hediff.TryGetComp<HediffComp_GunHand>();
                    // Only proceed if the comp and its gun-hand weapon are valid.
                    if (comp != null && comp.gunHandWeapon != null && comp.gunHandWeapon == eq)
                    {
                        // Cancel the drop if the weapon being dropped is the one managed by our hediff.
                        __result = false;
                        return false;
                    }
                }
            }
            // Otherwise, allow the drop as normal.
            return true;
        }
    }
}

