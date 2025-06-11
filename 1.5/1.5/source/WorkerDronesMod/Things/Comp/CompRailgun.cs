using Verse;

namespace WorkerDronesMod
{
    public class CompRailgun : ThingComp
    {
        private CompProperties_Railgun Props => (CompProperties_Railgun)this.props;

        /// <summary>
        /// Number of ticks to wait after firing.
        /// </summary>
        public int CustomCooldownTicks => Props.customCooldownTicks;

        /// <summary>
        /// One-shot sound to play when the beam sequence completes.
        /// </summary>
        public SoundDef FinishedSound => Props.finishedSound;
    }
}

