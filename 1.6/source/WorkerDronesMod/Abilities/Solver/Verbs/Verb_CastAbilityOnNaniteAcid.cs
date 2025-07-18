using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class Verb_CastAbilityOnNaniteAcid : Verb_CastAbilityTouch
    {
        private static readonly HediffDef RequiredHediff = MD_DefOf.MD_NaniteAcidBuildup;

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            // Ensure the base checks pass.
            if (!base.ValidateTarget(target, showMessages))
                return false;

            // Ensure the target is a pawn.
            if (!target.HasThing || !(target.Thing is Pawn targetPawn))
            {
                if (showMessages)
                    Messages.Message("MD.NotValidTarget".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // Ensure the target is affected by nanite acid buildup.
            if (!targetPawn.health.hediffSet.HasHediff(RequiredHediff))
            {
                if (showMessages)
                    Messages.Message("MD.PawnNOTAffectedByNaniteBuildup".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }
    }
}
