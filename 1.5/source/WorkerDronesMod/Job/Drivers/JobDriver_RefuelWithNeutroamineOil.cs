using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace WorkerDronesMod
{
    // This job driver handles the refuelling process using neutroamine.
    // It directs the pawn to go to the neutroamine resource (TargetIndex.A),
    // then carries it to a designated consumption spot (TargetIndex.B) using a mechanism similar
    // to that used by the corpse consumption job, where the pawn waits and then consumes the required
    // number of neutroamine units. After consumption, any leftover is hauled back to the original location.
    public class JobDriver_RefuelWithNeutroamineOil : JobDriver
    {
        // Conversion factor: amount of oil recovered per neutroamine unit.
        public const float OilPerNeutroamineUnit = 10f;
        // Consumption time per neutroamine unit—in ticks.
        public const float ConsumptionTimePerUnit = 100f;

        // Field to hold the sustained sound during consumption.
        private Sustainer consumeSustainer;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.GetTarget(TargetIndex.A), this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Ensure the sustained sound stops when the job ends (whether normally or interrupted).
            this.AddFinishAction((JobCondition condition) =>
            {
                if (this.consumeSustainer != null)
                {
                    this.consumeSustainer.End();
                    this.consumeSustainer = null;
                }
            });

            // Fail if the neutroamine resource (TargetIndex.A) is destroyed or forbidden.
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);

            // --- Toil 1: Go to the neutroamine resource.
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // --- Toil 2: Pick up the neutroamine resource.
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false, true);

            // Record the original resource cell so we can later drop any leftover.
            IntVec3 originalResourceCell = this.job.GetTarget(TargetIndex.A).Cell;

            // --- Toil 3: Carry the resource to a proper consumption spot.
            yield return CarryResourceToConsumptionSpot(this.pawn);

            // --- Toil 4: Wait at the consumption spot with a progress bar.
            int waitTicks = Mathf.RoundToInt(this.job.count * ConsumptionTimePerUnit);
            Toil waitToil = Toils_General.Wait(waitTicks)
                .WithProgressBarToilDelay(TargetIndex.B, true);
            waitToil.tickAction += () =>
            {
                if (this.consumeSustainer == null)
                {
                    // Attempt to play the sustained sound (e.g., "HemogenPack_Consume").
                    this.consumeSustainer = SoundStarter.TrySpawnSustainer(SoundDef.Named("HemogenPack_Consume"), this.pawn);
                }
                this.consumeSustainer?.Maintain();
            };
            yield return waitToil;

            // --- Toil 5: Consume the required number of neutroamine units.
            Toil consumeToil = new Toil
            {
                initAction = delegate
                {
                    // Stop the sustained sound after waiting.
                    if (this.consumeSustainer != null)
                    {
                        this.consumeSustainer.End();
                        this.consumeSustainer = null;
                    }

                    // Retrieve the pawn’s Gene_NeutroamineOil.
                    Gene_NeutroamineOil gene = this.pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
                    if (gene == null)
                    {
                        Log.Error("[JobDriver_RefuelWithNeutroamineOil] Pawn has no Gene_NeutroamineOil.");
                        return;
                    }

                    // Get the carried neutroamine resource.
                    Thing carriedItem = this.pawn.carryTracker.CarriedThing;
                    if (carriedItem == null)
                    {
                        Log.Error("[JobDriver_RefuelWithNeutroamineOil] Pawn is not carrying a neutroamine resource.");
                        return;
                    }

                    int unitsToConsume = this.job.count;
                    if (carriedItem.stackCount > unitsToConsume)
                    {
                        // Split off only the required number of units.
                        Thing consumed = carriedItem.SplitOff(unitsToConsume);
                        float oilRecovered = consumed.stackCount * OilPerNeutroamineUnit;
                        gene.Value = Mathf.Min(gene.Value + oilRecovered, gene.InitialResourceMax);
                        consumed.Destroy(DestroyMode.Vanish);
                        Log.Message($"[JobDriver_RefuelWithNeutroamineOil] Consumed {unitsToConsume} units. Oil now: {gene.Value}");
                        // Leftover remains with the pawn.
                    }
                    else
                    {
                        // Consume the entire carried stack.
                        float oilRecovered = carriedItem.stackCount * OilPerNeutroamineUnit;
                        gene.Value = Mathf.Min(gene.Value + oilRecovered, gene.InitialResourceMax);
                        carriedItem.Destroy(DestroyMode.Vanish);
                        Log.Message($"[JobDriver_RefuelWithNeutroamineOil] Consumed entire stack. Oil now: {gene.Value}");
                    }

                    // Award the happy thought indicating successful consumption.
                    ThoughtDef happyThought = ThoughtDef.Named("MD_ConsumedNeutroamineOil_Happy");
                    this.pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(happyThought);
                }
            };
            yield return consumeToil;

            // --- Toil 6: Haul leftover resource (if any) back to the original location.
            Toil haulLeftover = new Toil
            {
                initAction = delegate
                {
                    if (this.pawn.carryTracker.CarriedThing != null)
                    {
                        if (!this.pawn.carryTracker.TryDropCarriedThing(
                                originalResourceCell,
                                ThingPlaceMode.Near,
                                out Thing dropped,
                                (Thing thing, int count) => { }))
                        {
                            Log.Warning("[JobDriver_RefuelWithNeutroamineOil] Failed to drop leftover neutroamine at original location.");
                        }
                        else
                        {
                            Log.Message($"[JobDriver_RefuelWithNeutroamineOil] Leftover neutroamine dropped at {originalResourceCell}.");
                        }
                    }
                }
            };
            yield return haulLeftover;
        }

        /// <summary>
        /// Carries the neutroamine resource (currently being carried as TargetIndex.A) to a valid consumption spot.
        /// This method mimics the behavior of the corpse job's consumption spot routine. It attempts to find a
        /// valid building that can be used as a consumption spot; if none is found, it falls back to using
        /// RCellFinder.SpotToChewStandingNear.
        /// The chosen consumption cell is set as TargetIndex.B.
        /// </summary>
        private Toil CarryResourceToConsumptionSpot(Pawn pawn)
        {
            Toil toil = ToilMaker.MakeToil("CarryResourceToConsumptionSpot");
            toil.initAction = delegate ()
            {
                Pawn actor = toil.actor;
                IntVec3 consumptionSpotCell = IntVec3.Invalid;
                // Get the neutroamine resource from the current job target.
                Thing resource = actor.CurJob.GetTarget(TargetIndex.A).Thing;

                // Validator for a valid consumption spot.
                Predicate<Thing> consumptionSpotValidator = delegate (Thing t)
                {
                    if (t.def.building == null || !t.def.building.isSittable)
                        return false;
                    IntVec3 freeCell;
                    if (!Toils_Ingest.TryFindFreeSittingSpotOnThing(t, actor, out freeCell))
                        return false;
                    if (t.IsForbidden(actor))
                        return false;
                    if (!actor.CanReserve(t, 1, -1, null, false))
                        return false;
                    if (!t.IsSociallyProper(actor))
                        return false;
                    if (t.IsBurning())
                        return false;
                    if (t.HostileTo(actor))
                        return false;
                    bool hasValidSurface = false;
                    for (int i = 0; i < 4; i++)
                    {
                        Building edifice = (freeCell + GenAdj.CardinalDirections[i]).GetEdifice(t.Map);
                        if (edifice != null && edifice.def.surfaceType == SurfaceType.Eat)
                        {
                            hasValidSurface = true;
                            break;
                        }
                    }
                    return hasValidSurface;
                };

                // Search for the closest valid building to use as a consumption spot.
                Thing consumptionSpot = GenClosest.ClosestThingReachable(
                    actor.Position,
                    actor.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial),
                    PathEndMode.OnCell,
                    TraverseParms.For(actor, Danger.Deadly, TraverseMode.ByPawn),
                    32f,
                    (Thing t) => consumptionSpotValidator(t) && t.Position.GetDangerFor(actor, t.Map) == Danger.None,
                    null,
                    0,
                    -1,
                    false,
                    RegionType.Set_Passable,
                    false);

                if (consumptionSpot == null)
                {
                    // Fallback: use RCellFinder.SpotToChewStandingNear.
                    consumptionSpotCell = RCellFinder.SpotToChewStandingNear(actor, resource, (IntVec3 c) => actor.CanReserveSittableOrSpot(c, false));
                }
                else
                {
                    if (!Toils_Ingest.TryFindFreeSittingSpotOnThing(consumptionSpot, actor, out consumptionSpotCell))
                    {
                        Log.Error("Could not find a free spot on the consumption building! This should not happen as we checked earlier.");
                    }
                }
                // Set the chosen cell as the consumption target (TargetIndex.B).
                actor.CurJob.SetTarget(TargetIndex.B, consumptionSpotCell);
                actor.ReserveSittableOrSpot(consumptionSpotCell, actor.CurJob, true);
                actor.Map.pawnDestinationReservationManager.Reserve(actor, actor.CurJob, consumptionSpotCell);
                actor.pather.StartPath(consumptionSpotCell, PathEndMode.OnCell);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }
    }
}



