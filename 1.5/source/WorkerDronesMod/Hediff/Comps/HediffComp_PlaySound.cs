using Verse;
using Verse.Sound;

namespace WorkerDronesMod
{
    public class HediffComp_PlaySound : HediffComp
    {
        // Declare the flag at the class level so it can be referenced in CompPostTick.
        private bool soundPlayed = false;
        public HediffCompProperties_PlaySound Props => (HediffCompProperties_PlaySound)props;

        // We use CompPostTick as a workaround since there is no CompPostAdd method available.
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (!soundPlayed)
            {
                // Only play the sound if this instance is the first instance of this hediff on the pawn.
                if (parent.pawn != null &&
                    parent.pawn.health.hediffSet.GetFirstHediffOfDef(parent.def) == parent)
                {
                    soundPlayed = true;
                    if (!string.IsNullOrEmpty(Props.soundDefName))
                    {
                        SoundDef sound = SoundDef.Named(Props.soundDefName);
                        if (sound != null)
                        {
                            sound.PlayOneShot(new TargetInfo(parent.pawn.Position, parent.pawn.Map, false));
                        }
                    }
                }
            }
        }
    }
}


