using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class JobGiver_BootupState : ThinkNode_JobGiver
    {
        // You can make this configurable or fetch from mental state if needed
        private const int DefaultBootupWaitTicks = 500;

        protected override Job TryGiveJob(Pawn pawn)
        {
            // Only assign the job if the pawn is in the bootup mental state
            // and does not already have the job
            if (pawn.MentalStateDef == MD_DefOf.MD_RecoverAndBootUp &&
                (pawn.jobs?.curJob == null || pawn.jobs.curJob.def != MD_DefOf.MD_Job_BootupIdle))
            {
                var job = new Job(MD_DefOf.MD_Job_BootupIdle)
                {
                    count = DefaultBootupWaitTicks // Set the wait duration
                };
                return job;
            }
            return null;
        }
    }
}


