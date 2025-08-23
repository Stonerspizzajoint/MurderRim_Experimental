using Verse;
using RimWorld;
using System.Collections.Generic;

namespace WorkerDronesMod
{
    public class WingsFlightControl : DefModExtension
    {
        public bool CanFly = true;
        public bool CanFlyInVaccuum;
        public HediffDef LandedHediff;
        public HediffDef FlyingHediff;
        public List<JobDef> allowedFlyingJobs;
        public EffecterDef FlyingEffecter; // NEW: Effecter to show while flying
        public EffecterDef LiftOffEffecter; // NEW: Effecter to show while taking off
    }
}
