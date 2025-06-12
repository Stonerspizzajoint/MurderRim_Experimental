using System.Linq;
using Verse;

namespace WorkerDronesMod
{
    public class HediffComp_RemoveIfPartGone : HediffComp
    {
        public HediffCompProperties_RemoveIfPartGone Props => (HediffCompProperties_RemoveIfPartGone)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            // If there's no pawn or no part, do nothing.
            if (parent?.pawn == null || parent.Part == null)
                return;

            // Check if the body part is still present.
            // GetNotMissingParts returns the list of parts that are still intact.
            if (!parent.pawn.health.hediffSet.GetNotMissingParts().Contains(parent.Part))
            {
                // Remove the hediff if its body part has been removed or destroyed.
                parent.pawn.health.RemoveHediff(parent);
            }
        }
    }
}

