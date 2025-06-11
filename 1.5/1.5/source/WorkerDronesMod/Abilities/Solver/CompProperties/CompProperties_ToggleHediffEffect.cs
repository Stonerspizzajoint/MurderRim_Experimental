using System.Collections.Generic;
using RimWorld;

namespace WorkerDronesMod
{
    public class CompProperties_ToggleHediffEffect : CompProperties_AbilityEffect
    {
        // List of Hediff defNames to toggle on the caster.
        public List<string> hediffDefsToToggle = new List<string>();

        // Default severity to apply when adding a hediff.
        public float defaultSeverity = 1.0f;

        // Optional: allowed body part defNames on which to add the hediff(s). 
        // If empty, the effect will default to the torso.
        public List<string> allowedBodyPartDefs = new List<string>();

        public CompProperties_ToggleHediffEffect()
        {
            compClass = typeof(Comp_ToggleHediffEffect);
        }
    }
}





