using RimWorld;
using Verse;
using Verse.AI;
using VREAndroids;

namespace WorkerDronesMod
{
    public class WorkGiver_ForceConsumeAndroidCorpse : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Corpse);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            return gene == null || base.ShouldSkip(pawn, forced);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!forced)
                return false;

            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene == null)
                return false;

            // Accept android corpse
            if (t is Corpse corpse && corpse.InnerPawn != null && Utils.IsAndroid(corpse.InnerPawn))
            {
                if (t.IsForbidden(pawn) || !pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly))
                    return false;
                return true;
            }

            // Accept downed android pawn
            if (t is Pawn targetPawn && Utils.IsAndroid(targetPawn) && targetPawn.Downed && !targetPawn.Dead)
            {
                if (targetPawn.IsForbidden(pawn) || !pawn.CanReserveAndReach(targetPawn, PathEndMode.Touch, Danger.Deadly))
                    return false;
                return true;
            }

            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!forced)
                return null;

            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene == null)
                return null;

            // Consume corpse
            if (t is Corpse corpse && corpse.InnerPawn != null && Utils.IsAndroid(corpse.InnerPawn))
            {
                Job job = JobMaker.MakeJob(MD_DefOf.MD_Job_RefuelWithCorpse, t);
                job.count = 1;
                return job;
            }

            // Kill downed android pawn
            if (t is Pawn targetPawn && Utils.IsAndroid(targetPawn) && targetPawn.Downed && !targetPawn.Dead)
            {
                Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, targetPawn);
                job.maxNumMeleeAttacks = 1;
                job.expiryInterval = 2000;
                job.attackDoorIfTargetLost = true;
                return job;
            }

            return null;
        }
    }
}

