using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class ConditionalStatAffecter_IsHeadCovered : ConditionalStatAffecter
    {
        public override bool Applies(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            if (pawn == null)
                return false;

            // Only true if head covered and NOT sufficiently covered
            return SolarUtil.IsHeadCovered(pawn) && !SolarUtil.IsSufficientlyCovered(pawn);
        }

        public override string Label => "Head Covered";
    }
}


