using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class MentalState_RefuelMadness : MentalState
    {
        private const int CheckInterval = 120;

        // at top of your MentalState_RefuelMadness class
        private Faction playerFaction = Faction.OfPlayer;

        public override bool ForceHostileTo(Faction f)
        {
            // If *this* pawn’s faction already hates the player,
            // then only treat those factions as hostile which also hate the player.
            if (pawn.Faction.HostileTo(playerFaction))
                return f.HostileTo(playerFaction);

            // Otherwise (if pawn’s faction is still friendly), only berserk against the player.
            return f == playerFaction;
        }

        public override bool ForceHostileTo(Thing t)
        {
            var f = t.Faction;
            // Things without a faction are not automatically enemies
            if (f == null)
                return false;

            return ForceHostileTo(f);
        }


        public override void PreStart()
        {
            base.PreStart();
            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            TryGiveRefuelJob();
        }

        public override void MentalStateTick()
        {
            base.MentalStateTick();

            // 0) If you’re in direct sun, recover immediately
            if (!SolverGeneUtility.IsThingSafeFromSun(pawn))
            {
                RecoverFromState();
                return;
            }

            // 1) Recover when Neutroamine is half-full
            var gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
            if (gene != null && gene.Value >= gene.MaxForDisplay * 0.5f)
            {
                RecoverFromState();
                return;
            }

            // 2) Every interval, refresh your own refuel or attack job
            if (pawn.IsHashIntervalTick(CheckInterval) &&
               (pawn.CurJobDef != MD_DefOf.MD_Job_RefuelWithNeutroamine &&
                pawn.CurJobDef != MD_DefOf.MD_Job_RefuelWithCorpse))
            {
                TryGiveRefuelJob();
            }
        }

        private void TryGiveRefuelJob()
        {
            var job = JobGiver_RefuelMadness.TryGiveRefuelMadnessJob(pawn);
            if (job != null)
            {
                job.locomotionUrgency = LocomotionUrgency.Sprint;
                pawn.jobs.TryTakeOrderedJob(job);
            }
        }
    }
}


