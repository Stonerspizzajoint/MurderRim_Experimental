using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(PrisonBreakUtility), nameof(PrisonBreakUtility.CanParticipateInPrisonBreak))]
    public static class PrisonBreakUtility_CanParticipateInPrisonBreak_Patch
    {
        static bool Prefix(Pawn pawn, ref bool __result)
        {
            if (ExtraSolverUtils.HasSolver(pawn))
            {
                var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
                var geneExt = gene?.ext ?? gene?.def.GetModExtension<SolverGeneExtension>();

                if (!SolarUtil.IsOutsideSafe(pawn, geneExt))
                {
                    __result = false;
                    return false; // Skip original method
                }
            }
            return true; // Continue to original method
        }
    }
}

