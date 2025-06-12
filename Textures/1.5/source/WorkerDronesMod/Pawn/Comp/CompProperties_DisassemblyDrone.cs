using Verse;

namespace WorkerDronesMod
{
    public class CompProperties_DisassemblyDrone : CompProperties
    {
        public CompProperties_DisassemblyDrone()
        {
            compClass = typeof(Comp_DisassemblyDrone);
        }
    }

    public class Comp_DisassemblyDrone : ThingComp
    {
        // This flag is used for another purpose in your mod (meleeJustUsed).
        public bool meleeJustUsed;

        // New flag to track whether the pawn is in Resurrection Stasis.
        public bool isInResurrectionStasis;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref meleeJustUsed, "meleeJustUsed", false);
            Scribe_Values.Look(ref isInResurrectionStasis, "isInResurrectionStasis", false);
        }
    }
}
