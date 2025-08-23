using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class JobDriver_BootupIdle : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            int waitTicks = job.count > 0 ? job.count : 999999; // fallback if not set

            var waitToil = Toils_General.Wait(waitTicks, TargetIndex.None);
            waitToil.AddEndCondition(() =>
            {
                // End the job if the mental state is no longer active
                if (pawn.MentalStateDef != MD_DefOf.MD_RecoverAndBootUp)
                    return JobCondition.Succeeded;
                return JobCondition.Ongoing;
            });
            yield return waitToil;
        }
    }
}

