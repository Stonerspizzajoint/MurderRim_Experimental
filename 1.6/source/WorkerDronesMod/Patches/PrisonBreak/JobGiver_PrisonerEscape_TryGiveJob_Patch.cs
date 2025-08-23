using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(JobGiver_PrisonerEscape), "TryGiveJob")]
    public static class JobGiver_PrisonerEscape_TryGiveJob_Patch
    {
        static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (ExtraSolverUtils.HasSolver(pawn))
            {
                var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
                var geneExt = gene?.ext ?? gene?.def.GetModExtension<SolverGeneExtension>();

                if (!SolarUtil.IsOutsideSafe(pawn, geneExt))
                {
                    __result = null; // Prevent escape job
                    return false;    // Skip original method
                }
            }
            return true; // Continue to original method
        }
    }
}

