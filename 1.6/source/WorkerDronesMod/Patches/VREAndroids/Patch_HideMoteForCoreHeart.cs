using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(MoteMaker), "MakeAttachedOverlay",
        new System.Type[] { typeof(Thing), typeof(ThingDef), typeof(Vector3), typeof(float), typeof(float) })]
    public static class Patch_PreventVrea_MoteAttachedOverlay
    {
        static bool Prefix(Thing thing, ThingDef moteDef, Vector3 offset, float scale, float solidTimeOverride,
                             ref Mote __result)
        {
            // Only intercept motes with the specific defName.
            if (moteDef.defName == "VREA_Mote_AndroidReformatting")
            {
                // Check if the parent (here "thing") is a Pawn of the disallowed race.
                if (thing is Pawn pawn && pawn.def.defName == "MD_CoreHeartRace")
                {
                    // (Optional) Log a message once this condition is hit.
                    Log.Message("[WorkerDronesMod] Preventing VREA_Mote_AndroidReformatting spawn on MD_CoreHeartRace.");

                    // Set the result to null to indicate that no mote should be spawned.
                    __result = null;
                    return false; // Skip the original MakeAttachedOverlay method.
                }
            }
            // Otherwise, allow the normal spawning process.
            return true;
        }
    }
}






