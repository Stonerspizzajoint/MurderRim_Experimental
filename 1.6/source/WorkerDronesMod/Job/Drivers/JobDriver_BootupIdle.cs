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
            // Stand in place indefinitely while the mental state is active
            yield return Toils_General.Wait(-1, TargetIndex.None);
        }
    }
}

