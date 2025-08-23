using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Interval))]
    public static class Patch_SkillRecord_Interval
    {
        static bool Prefix(SkillRecord __instance)
        {
            // Prevent natural decay for SolverControl skill only
            if (__instance.def == MD_DefOf.SolverControl)
            {
                // Skip the original method, so no decay occurs
                return false;
            }
            // Allow normal decay for all other skills
            return true;
        }
    }
}

