using RimWorld;
using Verse;
using Verse.AI;
using WorkerDronesMod;

namespace WorkerDronesMod
{
    public class ThinkNode_ConditionalSolver : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            return ExtraSolverUtils.HasSolver(pawn);
        }
    }
}

