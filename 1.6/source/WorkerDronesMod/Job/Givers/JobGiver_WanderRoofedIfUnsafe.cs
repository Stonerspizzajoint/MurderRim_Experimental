using RimWorld;
using Verse;
using Verse.AI;
using System;

namespace WorkerDronesMod
{
    public class JobGiver_WanderRoofedIfUnsafe : JobGiver_Wander
    {
        public JobGiver_WanderRoofedIfUnsafe()
        {
            this.wanderRadius = 10f;
            this.ticksBetweenWandersRange = new IntRange(100, 200);
            this.locomotionUrgency = LocomotionUrgency.Walk;
            this.maxDanger = Danger.Some;
        }

        protected override IntVec3 GetWanderRoot(Pawn pawn)
        {
            return pawn.Position;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!ExtraSolverUtils.HasSolver(pawn))
                return base.TryGiveJob(pawn);

            var ext = pawn.def.GetModExtension<SolverGeneExtension>();
            bool mustStayRoofed = !SolarUtil.IsOutsideSafe(pawn, ext);

            if (!mustStayRoofed)
                return base.TryGiveJob(pawn);

            bool inRoof = pawn.Position.Roofed(pawn.Map);

            // If already in a roofed cell, wander normally (walk)
            if (inRoof)
            {
                this.locomotionUrgency = LocomotionUrgency.Walk;
                this.ticksBetweenWandersRange = new IntRange(100, 200);
                this.wanderRadius = 10f;

                // Only wander to other roofed cells
                Func<Pawn, IntVec3, IntVec3, bool> validator = (p, c, root) =>
                    c.Roofed(p.Map) && p.CanReach(c, PathEndMode.OnCell, Danger.Some);

                IntVec3 dest = RCellFinder.RandomWanderDestFor(
                    pawn,
                    pawn.Position,
                    this.wanderRadius,
                    validator,
                    PawnUtility.ResolveMaxDanger(pawn, this.maxDanger),
                    this.canBashDoors
                );

                if (dest.IsValid && dest != pawn.Position)
                {
                    Job job = JobMaker.MakeJob(JobDefOf.GotoWander, dest);
                    job.locomotionUrgency = LocomotionUrgency.Walk;
                    return job;
                }
                // If no other roofed cell, just idle
                return null;
            }
            else
            {
                // Not in a roofed cell: panicked wander (sprint/jog, more frequent, larger radius)
                this.locomotionUrgency = LocomotionUrgency.Sprint;
                this.ticksBetweenWandersRange = new IntRange(20, 60);
                this.wanderRadius = 20f;

                // Try to find the nearest roofed cell first
                IntVec3 roofedCell = IntVec3.Invalid;
                float minDist = float.MaxValue;
                foreach (var c in GenRadial.RadialCellsAround(pawn.Position, 20f, true))
                {
                    if (c.InBounds(pawn.Map) && c.Roofed(pawn.Map) && pawn.CanReach(c, PathEndMode.OnCell, Danger.Some))
                    {
                        float dist = pawn.Position.DistanceTo(c);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            roofedCell = c;
                        }
                    }
                }
                if (roofedCell.IsValid)
                {
                    // Sprint to the nearest roofed cell
                    Job job = JobMaker.MakeJob(JobDefOf.GotoWander, roofedCell);
                    job.locomotionUrgency = LocomotionUrgency.Sprint;
                    return job;
                }
                else
                {
                    // No roofed cell found: panicked, erratic wander (anywhere, but fast)
                    Func<Pawn, IntVec3, IntVec3, bool> validator = (p, c, root) =>
                        p.CanReach(c, PathEndMode.OnCell, Danger.Some);

                    IntVec3 dest = RCellFinder.RandomWanderDestFor(
                        pawn,
                        pawn.Position,
                        this.wanderRadius,
                        validator,
                        PawnUtility.ResolveMaxDanger(pawn, this.maxDanger),
                        this.canBashDoors
                    );

                    if (dest.IsValid && dest != pawn.Position)
                    {
                        Job job = JobMaker.MakeJob(JobDefOf.GotoWander, dest);
                        job.locomotionUrgency = LocomotionUrgency.Sprint;
                        return job;
                    }
                    // If no valid cell, just idle
                    return null;
                }
            }
        }
    }
}

