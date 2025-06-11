using Verse;

namespace WorkerDronesMod
{
    public class HediffCompProperties_ReplaceWithVRECounterpart : HediffCompProperties
    {
        // When the parent's severity reaches this value, replacement occurs.
        public float severityThreshold = 1.0f;
        // Optional letter fields for notification.
        [MustTranslate]
        public string letterLabel;
        [MustTranslate]
        public string letterDesc;
        public LetterDef letterDef;

        public HediffCompProperties_ReplaceWithVRECounterpart()
        {
            compClass = typeof(HediffComp_ReplaceWithVRECounterpart);
        }
    }
}
