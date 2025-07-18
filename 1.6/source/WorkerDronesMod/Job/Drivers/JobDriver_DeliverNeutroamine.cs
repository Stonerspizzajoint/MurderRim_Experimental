using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class JobDriver_DeliverNeutroamine : JobDriver_HaulToCell
    {
        public Pawn Deliveree
        {
            get
            {
                return this.job.targetC.Pawn;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Reserve the prisoner (deliveree) and then the item/cell as usual
            return this.pawn.Reserve(this.Deliveree, this.job, 1, -1, null, errorOnFailed, false)
                && base.TryMakePreToilReservations(errorOnFailed);
        }
    }
}

