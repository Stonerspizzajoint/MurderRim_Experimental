using Verse;
using Verse.Sound;

namespace WorkerDronesMod
{
    public class HediffComp_SustainedSound : HediffComp
    {
        private Sustainer sustainer;

        public HediffCompProperties_SustainedSound Props => (HediffCompProperties_SustainedSound)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = parent.pawn;

            // If pawn is gone, dead, destroyed, or off‑map, end the sound
            if (pawn == null || pawn.Dead || pawn.Destroyed || pawn.MapHeld == null)
            {
                EndSustainer();
                return;
            }

            // If no active sustainer (or it’s ended), try to spawn one
            if (sustainer == null || sustainer.Ended)
            {
                if (Props.sustainSound != null)
                {
                    var info = SoundInfo.InMap(pawn, Props.maintenanceType);
                    sustainer = Props.sustainSound.TrySpawnSustainer(info);
                }
            }
            else
            {
                // Keep it alive and moving with the pawn
                sustainer.Maintain();
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            EndSustainer();
        }

        private void EndSustainer()
        {
            if (sustainer != null && !sustainer.Ended)
            {
                sustainer.End();
            }
            sustainer = null;
        }
    }
}






