using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(SkillRecord), "TotallyDisabled", MethodType.Getter)]
    public static class Patch_SkillRecord_TotallyDisabled
    {
        static void Postfix(SkillRecord __instance, ref bool __result)
        {
            // Only affect the SolverControl skill
            if (__instance.def == MD_DefOf.SolverControl)
            {
                Pawn pawn = __instance.Pawn;
                // If the pawn does not have a solver, always disable the skill
                if (!ExtraSolverUtils.HasSolver(pawn))
                {
                    __result = true;
                }
            }
        }
    }
}

