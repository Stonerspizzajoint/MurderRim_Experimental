using RimWorld;
using Verse;
using VREAndroids;

namespace WorkerDronesMod
{
    public class HediffComp_ReplaceWithVRECounterpart : HediffComp
    {
        public HediffCompProperties_ReplaceWithVRECounterpart Props => (HediffCompProperties_ReplaceWithVRECounterpart)this.props;

        // Every tick, check if the parent's severity has reached or exceeded the threshold.
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.Severity >= Props.severityThreshold)
            {
                TriggerReplacement();
            }
        }

        public void TriggerReplacement()
        {
            Pawn pawn = this.parent.pawn;
            if (pawn == null)
                return;
            if (this.parent.Part == null)
                return;
            // Get the VREA counterpart for the affected part.
            HediffDef counterpart = this.parent.Part.def.GetAndroidCounterPart();
            if (counterpart == null)
            {
                Log.Error($"[HediffComp_ReplaceWithVRECounterpart] No VREA counterpart found for part {this.parent.Part.def.defName} on {pawn.LabelShort}");
                return;
            }

            // If the pawn doesn't already have the counterpart on the same part, apply it.
            if (!pawn.health.hediffSet.HasHediff(counterpart, this.parent.Part))
            {
                Hediff newHediff = HediffMaker.MakeHediff(counterpart, pawn, this.parent.Part);
                // Optionally transfer severity or set a default severity.
                newHediff.Severity = this.parent.Severity;
                pawn.health.AddHediff(newHediff, this.parent.Part);
            }

            // Optionally send a notification letter.
            if (!Props.letterLabel.NullOrEmpty() && !Props.letterDesc.NullOrEmpty() && PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                // For example, if you want to list the part name:
                TaggedString label = Props.letterLabel.Formatted(pawn.Named("PAWN"), this.parent.Part.LabelCap);
                TaggedString text = Props.letterDesc.Formatted(pawn.Named("PAWN"), this.parent.Part.LabelCap);
                Find.LetterStack.ReceiveLetter(label, text, Props.letterDef ?? LetterDefOf.NegativeEvent, pawn);
            }

            // Finally, remove the original hediff.
            pawn.health.RemoveHediff(this.parent);
        }
    }
}
