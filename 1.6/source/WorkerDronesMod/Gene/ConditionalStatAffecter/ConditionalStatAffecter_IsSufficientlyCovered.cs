using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class ConditionalStatAffecter_IsSufficientlyCovered : ConditionalStatAffecter
    {
        public override bool Applies(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            if (pawn == null)
                return false;

            // Only true if sufficiently covered
            return SolarUtil.IsSufficientlyCovered(pawn);
        }

        public override string Label => "Sufficiently Covered";
    }
}

