using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace WorkerDronesMod
{
    public class JobDriver_BlowBubbles : JobDriver
    {
        private Effecter bubbleEffecter;
        private ThingWithComps bubbleWand;
        private ThingWithComps originalEquipment;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // No reservations needed for this job
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => pawn?.genes == null || !pawn.genes.HasActiveGene(MD_DefOf.MD_InterchangeableHands));

            // Toil: Wait and play bubble effect, equip wand
            Toil waitAndBlowBubbles = Toils_General.Wait(1200);
            waitAndBlowBubbles.initAction = () =>
            {
                pawn.pather.StopDead();
                bubbleEffecter = MD_DefOf.MD_BlowingBubbles.Spawn();
                bubbleEffecter.Trigger(pawn, pawn);

                // Store and remove current equipment (if any)
                if (pawn.equipment != null && pawn.equipment.Primary != null)
                {
                    originalEquipment = pawn.equipment.Primary;
                    pawn.equipment.Remove(originalEquipment);
                }

                // Spawn and equip the bubble wand
                var wandThing = ThingMaker.MakeThing(MD_DefOf.BubbleWand_Hand) as ThingWithComps;
                bubbleWand = wandThing;
                if (bubbleWand != null && pawn.equipment != null)
                {
                    // Remove any existing BubbleWand_Hand first
                    foreach (var eq in pawn.equipment.AllEquipmentListForReading)
                    {
                        if (eq.def == MD_DefOf.BubbleWand_Hand)
                        {
                            pawn.equipment.Remove(eq);
                            eq.Destroy();
                            break;
                        }
                    }
                    pawn.equipment.AddEquipment(bubbleWand);
                }
            };
            waitAndBlowBubbles.tickAction = () =>
            {
                bubbleEffecter?.EffectTick(pawn, new TargetInfo(pawn.Position, pawn.Map));
            };
            waitAndBlowBubbles.AddFinishAction(() =>
            {
                bubbleEffecter?.Cleanup();
                bubbleEffecter = null;

                // Remove and destroy the bubble wand
                if (bubbleWand != null && pawn.equipment != null)
                {
                    pawn.equipment.Remove(bubbleWand);
                    bubbleWand.Destroy();
                    bubbleWand = null;
                }

                // Restore original equipment
                if (originalEquipment != null && pawn.equipment != null)
                {
                    pawn.equipment.AddEquipment(originalEquipment);
                    originalEquipment = null;
                }
            });
            waitAndBlowBubbles.PlaySustainerOrSound(() => MD_DefOf.MD_BlowingBubblesSound);

            yield return waitAndBlowBubbles;

            // Toil: Gain joy
            yield return Toils_General.Do(() =>
            {
                pawn.needs.joy?.GainJoy(1.0f, MD_DefOf.MD_BubbleBlowing);
            });
        }
    }
}

