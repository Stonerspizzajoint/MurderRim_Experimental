using Verse;

namespace WorkerDronesMod
{
    public class Gene_HediffSwitcherActivator : Gene
    {
        // Cache the last known state as PawnState.
        private GeneHediffSwitcherUtility.PawnState lastState = GeneHediffSwitcherUtility.PawnState.Normal;

        public override void PostAdd()
        {
            base.PostAdd();
            Log.Message($"[Gene_HediffSwitcherActivator] PostAdd called for pawn {pawn.LabelShort}.");

            // Determine and cache the initial state, then update the hediff.
            GeneHediffSwitcherUtility.PawnState currentState = GeneHediffSwitcherUtility.GetPawnState(pawn);
            lastState = currentState;
            GeneHediffSwitcherUtility.UpdateGeneHediff(pawn, this);
        }

        public override void Tick()
        {
            base.Tick();

            // Retrieve the current state every tick.
            GeneHediffSwitcherUtility.PawnState currentState = GeneHediffSwitcherUtility.GetPawnState(pawn);
            // Only update if the state has changed.
            if (currentState != lastState)
            {
                lastState = currentState;
                GeneHediffSwitcherUtility.UpdateGeneHediff(pawn, this);
            }
        }

        // New override to handle removal.
        public override void PostRemove()
        {
            base.PostRemove();
            Log.Message($"[Gene_HediffSwitcherActivator] PostRemove called for pawn {pawn.LabelShort}.");
            // When the gene is removed, remove all associated hediffs.
            GeneHediffSwitcherUtility.RemoveGeneHediff(pawn, this);
        }
    }
}










