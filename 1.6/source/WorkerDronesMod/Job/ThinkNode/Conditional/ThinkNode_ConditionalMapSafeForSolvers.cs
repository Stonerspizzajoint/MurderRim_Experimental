using RimWorld;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    /// <summary>
    /// Conditional node: returns true if the current map is "safe" for solvers (per SolarUtil.IsOutsideSafe).
    /// </summary>
    public class ThinkNode_ConditionalMapSafeForSolvers : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            // Use the extension if available, otherwise null
            var ext = pawn.def.GetModExtension<SolverGeneExtension>();
            return SolarUtil.IsOutsideSafe(pawn, ext);
        }
    }
}
