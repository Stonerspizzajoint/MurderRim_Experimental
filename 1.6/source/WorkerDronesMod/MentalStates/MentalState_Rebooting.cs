using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class MentalState_Rebooting : MentalState
    {
        // Number of ticks to wait after healing before ending the mental state (e.g., 120 = 2 seconds)
        private const int PostHealDelayTicks = 500;
        private int postHealTicksLeft = -1;

        public override void MentalStateTick(int delta)
        {
            base.MentalStateTick(delta);

            if (pawn?.health?.hediffSet == null)
                return;

            bool hasNonPermanentInjury = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Any(h => !h.IsPermanent());

            if (!hasNonPermanentInjury)
            {
                // Start or continue the post-heal countdown
                if (postHealTicksLeft < 0)
                    postHealTicksLeft = PostHealDelayTicks;
                else
                    postHealTicksLeft--;

                if (postHealTicksLeft <= 0)
                    RecoverFromState();
            }
            else
            {
                // Reset the countdown if new injuries appear
                postHealTicksLeft = -1;
            }
        }

        public override void PreStart()
        {
            base.PreStart();
            if (!pawn.health.hediffSet.HasHediff(MD_DefOf.MD_BootupComa))
                pawn.health.AddHediff(MD_DefOf.MD_BootupComa);
        }

        public override void PostEnd()
        {
            base.PostEnd();
            var comaHediff = pawn.health.hediffSet.GetFirstHediffOfDef(MD_DefOf.MD_BootupComa);
            if (comaHediff != null)
                pawn.health.RemoveHediff(comaHediff);
        }
    }

    public static class HediffSetExtensions
    {
        // Returns true if there is any injury that can still heal naturally
        public static bool HasNaturallyHealingInjury(this HediffSet set)
        {
            return set.hediffs.OfType<Hediff_Injury>().Any(h => !h.IsPermanent() && h.CanHealNaturally());
        }
    }
}

