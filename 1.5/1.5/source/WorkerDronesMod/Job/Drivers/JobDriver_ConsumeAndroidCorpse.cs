using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace WorkerDronesMod
{
    public class JobDriver_ConsumeAndroidCorpse : JobDriver
    {
        // Base ticks to wait per organ consumed.
        public const int ConsumeTicks = 100;
        public const float OilPerUnitDefault = RefuelUtils.OilPerUnitDefault;
        public const float OilPerUnitOrgan = RefuelUtils.OilPerUnitOrgan;

        // Vital organs yield a higher oil bonus.
        private static readonly HashSet<string> VitalOrgans = new HashSet<string>
        {
            "Heart", "Liver", "Kidney", "Lung", "Stomach"
        };

        // Shortcut to retrieve the corpse from the job target.
        private Corpse Corpse => (Corpse)job.GetTarget(TargetIndex.A).Thing;
        // Shortcut to the pawn's Gene_NeutroamineOil.
        private Gene_NeutroamineOil OilGene => pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();

        private int ticksLeft;
        private Sustainer sustain;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // If the corpse is already reserved by another pawn, do not assign this job.
            if (Corpse != null && !pawn.CanReserve(Corpse, 1, -1, null, false))
                return false;
            return pawn.Reserve(Corpse, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // STEP 1) Go to the corpse.
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // STEP 2) Pick up the corpse.
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);

            // STEP 3) Immediately remove the ribcage from the corpse's inner pawn and play the breaking sound.
            yield return new Toil
            {
                initAction = delegate ()
                {
                    if (Corpse != null && Corpse.InnerPawn != null)
                    {
                        var android = Corpse.InnerPawn;
                        BodyPartRecord ribcage = android.health.hediffSet.GetNotMissingParts()
                            .FirstOrDefault(p => p.def.defName.Equals("Ribcage", StringComparison.OrdinalIgnoreCase));
                        if (ribcage != null)
                        {
                            var hd = HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, android, ribcage);
                            android.health.AddHediff(hd, ribcage);
                            Log.Message($"[JobDriver_ConsumeAndroidCorpse] Removed ribcage from {android.LabelShort}");
                            SoundInfo info = SoundInfo.InMap(new TargetInfo(android.Position, pawn.Map, false));
                            SoundStarter.PlayOneShot(MD_DefOf.MD_BreakingRibcage, info);
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            // STEP 4) Carry the corpse to a valid consumption spot.
            yield return CarryCorpseToConsumptionSpot(pawn);

            // STEP 5) Wait for a consumption period scaling with the number of organs to consume.
            int totalConsumeTicks = Mathf.RoundToInt(job.count * ConsumeTicks);
            Toil consumeDelay = new Toil
            {
                initAction = delegate ()
                {
                    ticksLeft = totalConsumeTicks;
                    sustain = SoundStarter.TrySpawnSustainer(MD_DefOf.PredatorLarge_Eat, pawn);
                },
                tickAction = delegate ()
                {
                    ticksLeft = Math.Max(0, ticksLeft - 1);
                    sustain?.Maintain();
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = totalConsumeTicks
            };
            consumeDelay.WithProgressBar(TargetIndex.A, () => 1f - ((float)ticksLeft / totalConsumeTicks));
            yield return consumeDelay;

            // STEP 6) Final consumption: re-check oil, consume selected organs, update oil and award bonus.
            {
                Toil finalConsumption = new Toil();
                finalConsumption.initAction = delegate ()
                {
                    sustain?.End();

                    // Re-check: if oil is already full, skip consumption.
                    if (OilGene != null && OilGene.Value >= OilGene.InitialResourceMax)
                    {
                        Log.Message("[JobDriver_ConsumeAndroidCorpse] Oil is already full; skipping consumption.");
                        return;
                    }

                    var android = Corpse.InnerPawn;
                    int partsToConsume = job.count;
                    float totalYield = 0f;

                    // Gather available parts, excluding those already missing.
                    List<BodyPartRecord> availableParts = android.health.hediffSet.GetNotMissingParts().ToList();
                    // Exclude the ribcage.
                    availableParts.RemoveAll(p => p.def.defName.Equals("Ribcage", StringComparison.OrdinalIgnoreCase));
                    // Exclude the torso if alternatives exist.
                    if (availableParts.Count > 1)
                        availableParts.RemoveAll(p => p.def.defName.Equals("Torso", StringComparison.OrdinalIgnoreCase));
                    if (availableParts.Count == 0)
                    {
                        Log.Message("[JobDriver_ConsumeAndroidCorpse] No available parts to consume.");
                        return;
                    }

                    List<BodyPartRecord> partsChosen = new List<BodyPartRecord>();
                    var vitalParts = availableParts.Where(p => VitalOrgans.Contains(p.def.defName)).ToList();
                    var nonVitalParts = availableParts.Except(vitalParts).ToList();

                    int countChosen = 0;
                    foreach (var part in vitalParts)
                    {
                        if (countChosen < partsToConsume)
                        {
                            partsChosen.Add(part);
                            countChosen++;
                        }
                    }
                    if (countChosen < partsToConsume)
                    {
                        foreach (var part in nonVitalParts)
                        {
                            if (countChosen < partsToConsume)
                            {
                                partsChosen.Add(part);
                                countChosen++;
                            }
                        }
                    }

                    foreach (var part in partsChosen)
                    {
                        float yield = VitalOrgans.Contains(part.def.defName) ? OilPerUnitOrgan : OilPerUnitDefault;
                        totalYield += yield;
                        var hd = HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, android, part);
                        android.health.AddHediff(hd, part);
                        Log.Message($"[JobDriver_ConsumeAndroidCorpse] Consumed {part.def.defName} from {android.LabelShort}, yield: {yield}");
                    }

                    float needed = OilGene.InitialResourceMax - OilGene.Value;
                    OilGene.Value = Mathf.Min(OilGene.Value + Mathf.Min(totalYield, needed), OilGene.InitialResourceMax);
                    Log.Message($"[JobDriver_ConsumeAndroidCorpse] Total oil after consumption: {OilGene.Value}");

                    // Award the happy thought.
                    ThoughtDef happyThought = ThoughtDef.Named("MD_ConsumedCorpseNeutroamineOil_Happy");
                    pawn.needs?.mood?.thoughts?.memories.TryGainMemory(happyThought);

                    // Apply efficiency bonus via the gene.
                    Gene_NeutroamineOil gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
                    if (gene != null)
                    {
                        gene.ApplyEfficiencyBonus(1.5f, 600);
                    }
                };
                finalConsumption.defaultCompleteMode = ToilCompleteMode.Instant;
                yield return finalConsumption;
            }

            // STEP 7) Direct the pawn to move the corpse to storage (walk there and then drop it).
            {
                Toil goToStorage = new Toil();
                goToStorage.initAction = delegate ()
                {
                    IntVec3 dropPos;
                    if (StoreUtility.TryFindBestBetterStoreCellFor(pawn.carryTracker.CarriedThing, pawn, pawn.Map,
                        StoragePriority.Normal, pawn.Faction, out dropPos, false))
                    {
                        goToStorage.actor.pather.StartPath(dropPos, PathEndMode.Touch);
                        goToStorage.actor.CurJob.SetTarget(TargetIndex.B, dropPos);
                    }
                    else
                    {
                        dropPos = pawn.Position;
                        goToStorage.actor.CurJob.SetTarget(TargetIndex.B, dropPos);
                    }
                };
                goToStorage.tickAction = delegate ()
                {
                    if (!pawn.pather.Moving || pawn.Position.InHorDistOf(pawn.CurJob.GetTarget(TargetIndex.B).Cell, 1f))
                    {
                        IntVec3 dropCell = pawn.CurJob.GetTarget(TargetIndex.B).Cell;
                        pawn.carryTracker.TryDropCarriedThing(dropCell, ThingPlaceMode.Near, out Thing droppedThing);
                        EndJobWith(JobCondition.Succeeded);
                    }
                };
                goToStorage.defaultCompleteMode = ToilCompleteMode.Never;
                goToStorage.defaultDuration = 600;
                yield return goToStorage;
            }
        }

        /// <summary>
        /// Mimics the behavior of CarryIngestibleToChewSpot,
        /// carrying the corpse to a valid consumption spot.
        /// If a valid building is found, its free cell is used;
        /// otherwise, falls back to using RCellFinder.SpotToChewStandingNear.
        /// The chosen spot is set as TargetIndex.B.
        /// </summary>
        private Toil CarryCorpseToConsumptionSpot(Pawn pawn)
        {
            Toil toil = ToilMaker.MakeToil("CarryCorpseToConsumptionSpot");
            toil.initAction = delegate ()
            {
                Pawn actor = toil.actor;
                IntVec3 consumptionSpotCell = IntVec3.Invalid;
                Thing corpse = actor.CurJob.GetTarget(TargetIndex.A).Thing;

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

                // Search for a consumption building.
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
                    consumptionSpotCell = RCellFinder.SpotToChewStandingNear(actor, corpse, (IntVec3 c) => actor.CanReserveSittableOrSpot(c, false));
                }
                else
                {
                    if (!Toils_Ingest.TryFindFreeSittingSpotOnThing(consumptionSpot, actor, out consumptionSpotCell))
                    {
                        Log.Error("Could not find a free spot on the consumption building! This should not happen as we checked earlier.");
                    }
                }
                actor.CurJob.SetTarget(TargetIndex.B, consumptionSpotCell);
                actor.ReserveSittableOrSpot(consumptionSpotCell, actor.CurJob, true);
                actor.Map.pawnDestinationReservationManager.Reserve(actor, actor.CurJob, consumptionSpotCell);
                actor.pather.StartPath(consumptionSpotCell, PathEndMode.OnCell);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }

        // --------------------------------------------------------------------------
        //NEW: Cleanup override to ensure sustained sounds are canceled if the job is canceled.
        // We use the new keyword because the inherited Cleanup is not marked virtual.
        public new void Cleanup(JobCondition condition)
        {
            // If a sustained sound was started by this job, end it.
            if (sustain != null)
            {
                // End only the sound produced by this job.
                sustain.End();
                sustain = null;
            }

            // Ensure that any additional cleanup (if implemented in the base) is performed.
            // Since the base Cleanup isn't virtual, we call it via a cast.
            ((JobDriver)this).Cleanup(condition);
        }
    }
}






