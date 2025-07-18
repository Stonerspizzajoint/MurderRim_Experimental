using System.Linq;  // Needed for ToList()
using HarmonyLib;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "Notify_EquipmentAdded")]
    public static class Patch_Pawn_EquipmentAdded_RemoveInterchangeable
    {
        static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            try
            {
                if (__instance == null || __instance.pawn == null || eq == null)
                    return;

                Pawn pawn = __instance.pawn;
                if (pawn.health == null || pawn.health.hediffSet == null || pawn.health.hediffSet.hediffs == null)
                    return;

                // Iterate over a snapshot copy of the hediffs to avoid concurrent modifications.
                foreach (Hediff hediff in pawn.health.hediffSet.hediffs.ToList())
                {
                    if (hediff != null && hediff.def != null && hediff.def.defName == "MD_interchangeable_SMGhand")
                    {
                        HediffComp_GunHand comp = hediff.TryGetComp<HediffComp_GunHand>();
                        if (comp != null && comp.gunHandWeapon != null && comp.gunHandWeapon == eq)
                        {
                            // Our special weapon is being added; do nothing further.
                            return;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Exception in Patch_Pawn_EquipmentAdded_RemoveInterchangeable: {ex}");
            }
        }
    }
}



