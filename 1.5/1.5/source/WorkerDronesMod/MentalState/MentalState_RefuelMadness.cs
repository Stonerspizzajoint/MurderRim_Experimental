using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse.AI;
using Verse;

namespace WorkerDronesMod
{
    public class MentalState_RefuelMadness : MentalState
    {
        private const int CheckInterval = 120;

        public override void PreStart()
        {
            base.PreStart();
            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            var job = JobGiver_RefuelMadness.TryGiveRefuelMadnessJob(pawn);
            if (job != null)
                pawn.jobs.TryTakeOrderedJob(job);
        }

        public override void MentalStateTick()
        {
            base.MentalStateTick();

            var gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
            float halfTarget = gene != null ? gene.MaxForDisplay * 0.5f : 0f;
            if (gene != null && gene.Value >= halfTarget)
            {
                RecoverFromState();
                return;
            }

            if (pawn.IsHashIntervalTick(CheckInterval) &&
               (pawn.jobs.curJob == null ||
                (pawn.jobs.curJob.def != MD_DefOf.MD_Job_RefuelWithNeutroamine &&
                 pawn.jobs.curJob.def != MD_DefOf.MD_Job_RefuelWithCorpse &&
                 pawn.jobs.curJob.def != JobDefOf.AttackMelee)))
            {
                var job = JobGiver_RefuelMadness.TryGiveRefuelMadnessJob(pawn);
                if (job != null)
                    pawn.jobs.TryTakeOrderedJob(job);
            }
        }
    }
}
