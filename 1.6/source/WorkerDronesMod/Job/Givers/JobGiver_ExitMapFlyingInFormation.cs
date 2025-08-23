using Verse;
using RimWorld;
using Verse.AI;

namespace WorkerDronesMod
{
    public class JobGiver_ExitMapFlyingInFormation : ThinkNode_JobGiver
    {
        public Rot4 FormationDirection = Rot4.East;

        protected override Job TryGiveJob(Pawn pawn)
        {
            // Basic checks
            if (pawn.Dead || pawn.Downed || !pawn.Spawned)
                return null;

            // Check for the specific duty
            if (pawn.mindState?.duty?.def != MD_DefOf.MD_ExitMapPanicFly)
                return null;

            // If pawn can fly and is not under a roof, assign flying job
            if (pawn.flight != null && pawn.flight.CanFlyNow && !pawn.Position.Roofed(pawn.Map))
            {
                Job job = JobMaker.MakeJob(MD_DefOf.ExitMapFlyingInFormation);
                job.targetA = pawn.Position;
                job.count = FormationDirection.AsInt;
                return job;
            }

            // Otherwise, assign default exit job
            IntVec3 exitCell;
            if (RCellFinder.TryFindBestExitSpot(pawn, out exitCell, TraverseMode.ByPawn))
            {
                Job job = JobMaker.MakeJob(JobDefOf.Goto, exitCell);
                job.exitMapOnArrival = true;
                return job;
            }

            return null;
        }
    }
}

