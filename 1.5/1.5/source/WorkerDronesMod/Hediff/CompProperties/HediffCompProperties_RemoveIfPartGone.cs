using Verse;

namespace WorkerDronesMod
{
    public class HediffCompProperties_RemoveIfPartGone : HediffCompProperties
    {
        public HediffCompProperties_RemoveIfPartGone()
        {
            // Tell RimWorld which HediffComp to use with these properties.
            compClass = typeof(HediffComp_RemoveIfPartGone);
        }
    }
}

