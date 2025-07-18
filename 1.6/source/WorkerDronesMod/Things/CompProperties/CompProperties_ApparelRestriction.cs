using Verse;

namespace WorkerDronesMod
{
    // This comp properties holds a simple bool.
    public class CompProperties_HatsOnly : CompProperties
    {
        // If set to true, the pawn will be allowed to wear only hats.
        public bool onlyAllowHats = true;

        public CompProperties_HatsOnly()
        {
            compClass = typeof(Comp_HatsOnly);
        }
    }
}
