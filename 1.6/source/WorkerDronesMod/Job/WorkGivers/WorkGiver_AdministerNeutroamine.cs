using RimWorld;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class WorkGiver_AdministerNeutroamine : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn patient = t as Pawn;
            if (patient == null || patient == pawn)
                return false;

            var gene = patient.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene == null)
                return false;

            // Don't administer if oil is high
            if (gene.Oil / gene.InitialResourceMax >= 0.95f)
                return false;

            // Only administer if oil is low, unless forced
            if (!forced && gene.Oil / gene.InitialResourceMax >= 0.25f)
                return false;

            // Only if the patient should be fed (e.g. downed, in bed, etc.)
            if (!FeedPatientUtility.ShouldBeFed(patient))
                return false;

            if (!pawn.CanReserve(t, 1, -1, null, false))
                return false;

            // Find neutroamine to use
            float oilNeeded = gene.InitialResourceMax - gene.Oil;
            float oilPerNeutro = 10f;
            int count;
            Thing neutro = JobGiver_RefuelWithNeutroamine.GetNeutroamineForRefuel(pawn, out count);
            if (neutro == null)
            {
                JobFailReason.Is("NoIngredient".Translate(MD_DefOf.Neutroamine.label), null);
                return false;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn patient = t as Pawn;
            var gene = patient.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            float oilNeeded = gene.InitialResourceMax - gene.Oil;
            float oilPerNeutro = 10f;
            int count;
            Thing neutro = JobGiver_RefuelWithNeutroamine.GetNeutroamineForRefuel(pawn, out count);
            if (neutro != null)
            {
                Job job = JobMaker.MakeJob(MD_DefOf.MD_Job_FeedOil, neutro, patient);
                job.count = count;
                return job;
            }
            return null;
        }
    }
}

