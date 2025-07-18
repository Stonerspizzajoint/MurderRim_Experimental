using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class WorkGiver_ForceConsumeNeutroamine : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.HaulableAlways);

        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

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

            if (t.def != MD_DefOf.Neutroamine)
                return false;

            // Optionally, check if consuming would restore oil
            float oilPerNeutro = 10f;
            if (gene.Oil >= gene.InitialResourceMax)
                return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!forced)
                return null;

            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene == null)
                return null;

            if (t.def != MD_DefOf.Neutroamine)
                return null;

            float oilNeeded = gene.InitialResourceMax - gene.Oil;
            float oilPerNeutro = 10f;
            int count = Mathf.Min(t.stackCount, Mathf.CeilToInt(oilNeeded / oilPerNeutro));

            Job job = JobMaker.MakeJob(MD_DefOf.MD_Job_RefuelWithNeutroamine, t);
            job.count = count;
            return job;
        }
    }
}
