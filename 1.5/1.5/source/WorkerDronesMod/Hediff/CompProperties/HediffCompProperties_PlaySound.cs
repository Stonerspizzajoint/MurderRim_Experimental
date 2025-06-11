using Verse;

namespace WorkerDronesMod
{
    public class HediffCompProperties_PlaySound : HediffCompProperties
    {
        // The defName of the sound to play when the hediff is applied.
        public string soundDefName;

        public HediffCompProperties_PlaySound()
        {
            compClass = typeof(HediffComp_PlaySound);
        }
    }
}
