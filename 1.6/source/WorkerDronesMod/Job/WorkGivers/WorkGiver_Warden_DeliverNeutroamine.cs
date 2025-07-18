using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class WorkGiver_Warden_DeliverNeutroamine : WorkGiver_Warden
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!base.ShouldTakeCareOfPrisoner(pawn, t, forced))
                return null;

            Pawn prisoner = t as Pawn;
            if (prisoner == null || prisoner == pawn)
                return null;

            if (!prisoner.guest.CanBeBroughtFood || !prisoner.Position.IsInPrisonCell(prisoner.Map))
                return null;

            var gene = prisoner.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene == null)
                return null;

            if (gene.Oil / gene.InitialResourceMax >= gene.OilRefuelThreshold)
                return null;

            if (!forced && gene.Oil > gene.InitialResourceMax * 0.95f)
                return null;

            if (WardenFeedUtility.ShouldBeFed(prisoner))
                return null;

            if (NeutroamineAlreadyDelivered(prisoner, gene))
                return null;

            float oilNeeded = gene.InitialResourceMax - gene.Oil;
            float oilPerNeutro = 10f; // Must match JobDriver_ConsumeNeutroamine
            int count = Mathf.CeilToInt(oilNeeded / oilPerNeutro);

            Thing neutro = GetNeutroamineForPrisoner(pawn, count);
            if (neutro == null)
                return null;

            IntVec3 c = RCellFinder.SpotToStandDuringJob(prisoner, null, null);
            if (!pawn.CanReserve(neutro, 1, -1, null, false) || !pawn.CanReserve(c, 1, -1, null, false) || !pawn.CanReserve(prisoner, 1, -1, null, false))
                return null;

            Job job = JobMaker.MakeJob(MD_DefOf.MD_Job_DeliverNeutroamine, neutro, c, prisoner);
            job.count = count;
            return job;
        }

        private bool NeutroamineAlreadyDelivered(Pawn prisoner, Gene_BasicSolver gene)
        {
            // Check if prisoner is carrying or has neutroamine in inventory
            if (prisoner.carryTracker?.CarriedThing != null && prisoner.carryTracker.CarriedThing.def == MD_DefOf.Neutroamine)
                return true;
            if (prisoner.inventory?.innerContainer.Any(x => x.def == MD_DefOf.Neutroamine) == true)
                return true;

            // Check if enough neutroamine is in the prisoner's room
            float oilNeeded = gene.InitialResourceMax - gene.Oil;
            float oilPerNeutro = 10f;
            int needed = Mathf.CeilToInt(oilNeeded / oilPerNeutro);

            Room room = prisoner.GetRoom(RegionType.Set_All);
            int found = 0;
            foreach (Thing thing in room.ContainedAndAdjacentThings.Where(x => x.def == MD_DefOf.Neutroamine))
            {
                found += thing.stackCount;
                if (found >= needed)
                    return true;
            }
            return false;
        }

        private Thing GetNeutroamineForPrisoner(Pawn pawn, int count)
        {
            return GenClosest.ClosestThing_Global_Reachable(
                pawn.Position,
                pawn.Map,
                pawn.Map.listerThings.ThingsOfDef(MD_DefOf.Neutroamine),
                PathEndMode.Touch,
                TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false),
                9999f,
                t => !t.IsForbidden(pawn) && pawn.CanReserve(t, 1, count, null, false)
            );
        }
    }
}

