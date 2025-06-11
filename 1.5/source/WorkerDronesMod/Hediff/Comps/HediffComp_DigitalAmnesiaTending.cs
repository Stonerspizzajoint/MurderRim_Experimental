using RimWorld;
using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    // Our custom comp reduces the virus severity further based on the tend quality.
    public class HediffComp_DigitalAmnesiaTending : HediffComp_TendDuration
    {
        public HediffCompProperties_DigitalAmnesiaTending Props => (HediffCompProperties_DigitalAmnesiaTending)this.props;

        public override void CompTended(float quality, float maxQuality, int batchPosition = 0)
        {
            // First, call the base tending behavior.
            base.CompTended(quality, maxQuality, batchPosition);

            // Calculate extra reduction using the tend quality.
            float extraReduction = quality * Props.qualityMultiplier;

            if (this.parent.Severity > 0f)
            {
                float oldSeverity = this.parent.Severity;
                this.parent.Severity = Mathf.Max(0f, this.parent.Severity - extraReduction);
                Log.Message($"[DigitalAmnesiaTending] Reduced severity from {oldSeverity:F2} to {this.parent.Severity:F2} using tend quality {quality:F2}");

                // If severity falls below threshold, cure (remove) the virus.
                if (this.parent.Severity < 0.2f)
                {
                    this.parent.pawn.health.RemoveHediff(this.parent);
                    Messages.Message($"{this.parent.pawn.LabelShort} has been cured of the memory virus.", MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message($"{this.parent.pawn.LabelShort}'s memory virus severity is reduced.", MessageTypeDefOf.NeutralEvent);
                }
            }
        }
    }
}

