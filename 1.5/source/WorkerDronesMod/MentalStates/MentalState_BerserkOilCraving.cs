using RimWorld;
using Verse;
using Verse.AI;
using VREAndroids;
using System.Linq;

namespace WorkerDronesMod
{
    public class MentalState_BerserkOilCraving : MentalState
    {
        public override void PostStart(string reason)
        {
            base.PostStart(reason);
            ClawHandsHediffUtility.ApplyClawHandsIfNeeded(this.pawn);
        }

        public override void PostEnd()
        {
            base.PostEnd();
            ClawHandsHediffUtility.RemoveClawHands(this.pawn);
        }

        public override bool ForceHostileTo(Thing t)
        {
            if (t is Pawn otherPawn)
            {
                if (otherPawn == this.pawn)
                    return false;

                if (this.pawn.Faction != null && otherPawn.Faction != null &&
                    this.pawn.Faction.def == MD_DefOf.MD_DisassemblyDronesFaction &&
                    otherPawn.Faction.def == MD_DefOf.MD_DisassemblyDronesFaction)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        public override bool ForceHostileTo(Faction f)
        {
            if (this.pawn.Faction != null && f != null &&
                this.pawn.Faction.def == MD_DefOf.MD_DisassemblyDronesFaction &&
                f.def == MD_DefOf.MD_DisassemblyDronesFaction)
            {
                return false;
            }
            return true;
        }

        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }
    }
}


