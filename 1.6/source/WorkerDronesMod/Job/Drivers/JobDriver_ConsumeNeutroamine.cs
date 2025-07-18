using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class JobDriver_ConsumeNeutroamine : JobDriver
    {
        public Thing ToConsume => job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(ToConsume, job, 1, job.count, null, errorOnFailed);
        }


        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

            // Pick up the neutroamine
            yield return Toils_Ingest.PickupIngestible(TargetIndex.A, pawn);

            // Go to a spot to "consume" (chew) it
            yield return CarryIngestibleToChewSpot(pawn, TargetIndex.A);

            // Chew/consume neutroamine
            Toil chew = ToilMaker.MakeToil("ChewNeutroamine");
            chew.initAction = delegate
            {
                chew.actor.jobs.curDriver.ticksLeftThisToil = 300; // Shorter than eating
            };
            chew.tickAction = delegate
            {
                chew.WithEffect(DefDatabase<EffecterDef>.GetNamed("EatVegetarian"), TargetIndex.A);
            };
            chew.WithProgressBar(TargetIndex.A, () =>
            {
                return 1f - (float)chew.actor.jobs.curDriver.ticksLeftThisToil / 300f;
            }, false, -0.5f, false);
            chew.defaultCompleteMode = ToilCompleteMode.Delay;
            chew.handlingFacing = true;
            chew.FailOnDestroyedOrNull(TargetIndex.A);
            chew.WithEffect(DefDatabase<EffecterDef>.GetNamed("EatVegetarian", true), TargetIndex.A);
            chew.PlaySustainerOrSound(DefDatabase<SoundDef>.GetNamed("Meal_Eat", true), 1f);
            yield return chew;

            // Finalize: refuel oil and destroy neutroamine
            Toil finalize = ToilMaker.MakeToil("RefuelOil");
            finalize.initAction = delegate
            {
                Pawn actor = finalize.actor;
                Thing toConsume = ToConsume;
                var gene = actor.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
                if (gene != null && toConsume != null)
                {
                    // Refuel oil: 1 neutroamine = 10 oil (adjust as needed)
                    float oilPerNeutro = 10f;
                    int count = Mathf.Min(toConsume.stackCount, job.count);
                    gene.Oil = Mathf.Min(gene.Oil + oilPerNeutro * count, gene.InitialResourceMax);

                    // Destroy the neutroamine used
                    toConsume.SplitOff(count).Destroy(DestroyMode.Vanish);

                    // >>> GIVE THE HAPPY THOUGHT HERE <<<
                    if (actor.needs?.mood != null && MD_DefOf.MD_ConsumedNeutroamineOil_Happy != null)
                    {
                        actor.needs.mood.thoughts.memories.TryGainMemory(MD_DefOf.MD_ConsumedNeutroamineOil_Happy);
                    }

                    if (actor.MentalState is MentalState_BerserkOilCraving)
                    {
                        actor.MentalState.RecoverFromState();
                    }
                }
            };
            finalize.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finalize;
        }

        // Optionally, you can copy the CarryIngestibleToChewSpot from your example for proper sit/eat surface logic.
        private Toil CarryIngestibleToChewSpot(Pawn pawn, TargetIndex ingestibleInd)
        {
            Toil toil = ToilMaker.MakeToil("CarryIngestibleToChewSpot");
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                IntVec3 chewSpot = RCellFinder.SpotToChewStandingNear(actor, actor.CurJob.GetTarget(ingestibleInd).Thing, c => actor.CanReserveSittableOrSpot(c, false));
                actor.ReserveSittableOrSpot(chewSpot, actor.CurJob, true);
                actor.Map.pawnDestinationReservationManager.Reserve(actor, actor.CurJob, chewSpot);
                actor.pather.StartPath(chewSpot, PathEndMode.OnCell);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }
    }
}

