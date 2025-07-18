using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    /// <summary>
    /// Minimal custom mental state inheriting from MentalState_WanderConfused.
    /// Every tick it checks the pawn's Gene_NeutroamineOil's IsSafeFromSun property
    /// and ends the state if that property is false. When the mental state ends it sets
    /// the internal confusion flag to false.
    /// </summary>
    public class MentalState_ConfusedWander : MentalState_WanderConfused
    {
        // Change our internal flag to public so that other classes can check it.
        public bool IsConfused { get; private set; } = true;

        public override void PostStart(string reason)
        {
            base.PostStart(reason);
            IsConfused = true;
            Log.Message($"[MentalState_ConfusedWander] {pawn.LabelShort} has entered the confused wandering state.");
        }

        public override void PostEnd()
        {
            base.PostEnd();
            IsConfused = false;
            Log.Message($"[MentalState_ConfusedWander] {pawn.LabelShort} has exited the confused wandering state.");
        }
    }
}