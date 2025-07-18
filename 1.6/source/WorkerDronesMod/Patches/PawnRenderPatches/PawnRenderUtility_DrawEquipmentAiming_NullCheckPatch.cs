using HarmonyLib;
using UnityEngine;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAiming")]
    public static class PawnRenderUtility_DrawEquipmentAiming_NullCheckPatch
    {
        // This prefix runs before any other patch on DrawEquipmentAiming.
        static bool Prefix(Thing eq, ref Vector3 drawLoc, ref float aimAngle)
        {
            if (eq == null)
            {
                // Skip drawing aiming if there is no equipment.
                return false;
            }
            return true;
        }
    }
}

