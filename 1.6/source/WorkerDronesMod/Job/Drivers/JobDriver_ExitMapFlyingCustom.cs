using Verse;
using RimWorld;
using Verse.AI;
using System.Collections.Generic;

namespace WorkerDronesMod
{
    public class JobDriver_ExitMapFlyingFormation : JobDriver_ExitMapFlying
    {
        public Rot4 ForcedDirection = Rot4.East;
        private int waitTicks = -1;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 1. Trigger takeoff if not already flying
            yield return new Toil
            {
                initAction = () =>
                {
                    if (pawn.flight != null && !pawn.flight.Flying)
                    {
                        pawn.flight.StartFlying();
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            // 2. Wait until pawn is flying (after takeoff animation)
            var waitForFlying = new Toil
            {
                tickAction = () =>
                {
                    // Wait until pawn is in the Flying state
                    if (pawn.flight != null && pawn.flight.Flying)
                    {
                        ReadyForNextToil();
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };
            waitForFlying.FailOn(() => pawn.flight == null || !pawn.flight.CanEverFly);
            yield return waitForFlying;


            // 3. Optional: wait a few ticks for polish
            if (waitTicks < 0)
            {
                waitTicks = Rand.Range(0, 10);
            }
            yield return Toils_General.Wait(waitTicks, TargetIndex.None);

            // 4. Despawn and spawn skyfaller
            yield return Toils_General.Do(delegate
            {
                Map map = pawn.Map;
                IntVec3 position = pawn.Position;
                pawn.DeSpawn(DestroyMode.Vanish);
                Skyfaller skyfaller = SkyfallerMaker.MakeSkyfaller(ThingDefOf.FlyerLeaving, pawn);
                GenSpawn.Spawn(skyfaller, position, map, WipeMode.Vanish);

                skyfaller.OverrideFlightFlippedHorizontal = new bool?(ForcedDirection == Rot4.West);
                pawn.Rotation = ForcedDirection;
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref waitTicks, "waitTicks", -1, false);
            Scribe_Values.Look(ref ForcedDirection, "ForcedDirection", Rot4.East, false);
        }
    }
}

