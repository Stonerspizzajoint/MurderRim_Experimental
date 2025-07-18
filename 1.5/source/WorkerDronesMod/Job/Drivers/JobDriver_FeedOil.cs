using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class JobDriver_FeedOil : JobDriver
    {
        public Thing ToConsume => this.job.GetTarget(TargetIndex.A).Thing;
        public Pawn Deliveree => this.job.targetB.Pawn;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.ToConsume, this.job, 1, this.job.count, null, errorOnFailed, false)
                && this.pawn.Reserve(this.Deliveree, this.job, 1, -1, null, errorOnFailed, false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);

            // Go to Neutroamine
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

            // Pick up Neutroamine
            yield return Toils_Ingest.PickupIngestible(TargetIndex.A, this.Deliveree);

            // Go to patient
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);

            // "Feed" Neutroamine (chew/deliver)
            Toil feed = ToilMaker.MakeToil("FeedOil");
            feed.initAction = delegate
            {
                feed.actor.jobs.curDriver.ticksLeftThisToil = 180; // Shorter than eating
            };
            feed.tickAction = delegate
            {
                feed.actor.GainComfortFromCellIfPossible(false);
            };
            feed.WithProgressBar(TargetIndex.B, () =>
            {
                return 1f - (float)feed.actor.jobs.curDriver.ticksLeftThisToil / 180f;
            }, false, -0.5f, false);
            feed.defaultCompleteMode = ToilCompleteMode.Delay;
            feed.handlingFacing = true;
            feed.FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch);
            feed.WithEffect(DefDatabase<EffecterDef>.GetNamed("EatVegetarian", true), TargetIndex.B);
            feed.PlaySustainerOrSound(DefDatabase<SoundDef>.GetNamed("Meal_Eat", true), 1f);
            yield return feed;

            // Finalize: refuel oil and destroy neutroamine
            Toil finalize = ToilMaker.MakeToil("FedOil");
            finalize.initAction = delegate
            {
                Pawn deliveree = this.Deliveree;
                Thing toConsume = this.ToConsume;
                var gene = deliveree.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
                if (gene != null && toConsume != null)
                {
                    float oilPerNeutro = 10f;
                    int count = this.job.count;
                    gene.Oil = Mathf.Min(gene.Oil + oilPerNeutro * count, gene.InitialResourceMax);

                    // Optional: mood/thoughts, messages, etc.
                    Messages.Message("MD.DroneFedOil".Translate(deliveree.LabelShort), deliveree, MessageTypeDefOf.PositiveEvent);

                    toConsume.SplitOff(count).Destroy(DestroyMode.Vanish);
                }
            };
            finalize.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finalize;
        }
    }
}

