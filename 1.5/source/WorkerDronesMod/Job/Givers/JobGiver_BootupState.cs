using RimWorld;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class JobGiver_BootupState : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            // Only give the job if the pawn is in the rebooting mental state
            if (pawn.MentalState is MentalState_Rebooting)
            {
                return JobMaker.MakeJob(MD_DefOf.MD_Job_BootupIdle);
            }
            return null;
        }
    }
}

