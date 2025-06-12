using Verse;

namespace WorkerDronesMod
{
    // This hediff represents a memory virus that gradually corrupts the pawn's cognitive functions.
    // It is tendable, and our custom comp improves the tending effectiveness based on the treating doctor's intelligence.
    public class Hediff_MemoryVirus : HediffWithComps
    {
        public override void Tick()
        {
            base.Tick();
            // Severity increases automatically per day as defined in the XML's <severityPerDay>.
            // You can add custom visual or sound effects here if desired.
        }
    }
}


