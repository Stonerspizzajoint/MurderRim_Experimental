using RimWorld;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class JobGiver_BlowBubbles : JoyGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            if (pawn?.genes == null || !pawn.genes.HasActiveGene(MD_DefOf.MD_InterchangeableHands))
                return null;

            return JobMaker.MakeJob(MD_DefOf.MD_BlowBubbles);
        }
    }
}

