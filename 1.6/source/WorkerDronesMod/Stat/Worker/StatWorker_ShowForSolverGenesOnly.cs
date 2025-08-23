using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class StatWorker_ShowForSolverGenesOnly : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            if (pawn?.genes == null)
                return false;

            // Hardcoded: Only show for pawns with either solver gene
            if (pawn.genes.HasActiveGene(MD_DefOf.MD_BasicSolver) ||
                pawn.genes.HasActiveGene(MD_DefOf.MD_AbsoluteSolver))
            {
                return true;
            }
            return false;
        }
    }
}



