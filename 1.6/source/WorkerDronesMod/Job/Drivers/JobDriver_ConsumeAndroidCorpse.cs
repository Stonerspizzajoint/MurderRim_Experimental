using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using VREAndroids;

namespace WorkerDronesMod
{
    public class JobDriver_ConsumeAndroidCorpse : JobDriver
    {
        public Thing CorpseToConsume => job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(CorpseToConsume, job, 1, -1, null, errorOnFailed, false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);

            // Go to corpse
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // "Consume" corpse
            Toil consume = ToilMaker.MakeToil("ConsumeAndroidCorpse");
            consume.initAction = () =>
            {
                consume.actor.jobs.curDriver.ticksLeftThisToil = 400;
            };
            // Only handle progress bar and effect once
            consume.WithEffect(EffecterDefOf.EatMeat, TargetIndex.A);
            consume.WithProgressBar(TargetIndex.A, () =>
            {
                return 1f - (float)consume.actor.jobs.curDriver.ticksLeftThisToil / 400f;
            }, false, -0.5f, false);
            consume.PlaySustainerOrSound(SoundDefOf.RawMeat_Eat, 1f);
            consume.defaultCompleteMode = ToilCompleteMode.Delay;
            consume.handlingFacing = true;
            consume.FailOnDestroyedOrNull(TargetIndex.A);
            yield return consume;

            // Finalize: grant oil, destroy a body part
            Toil finalize = ToilMaker.MakeToil("GainOilFromAndroidCorpse");
            finalize.initAction = () =>
            {
                Pawn actor = finalize.actor;
                Thing corpseThing = CorpseToConsume;
                var gene = actor.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
                if (gene == null || !(corpseThing is Corpse corpse) || corpse.InnerPawn == null)
                    return;

                Pawn innerPawn = corpse.InnerPawn;

                // Only proceed if the corpse is an android
                if (!Utils.IsAndroid(innerPawn))
                    return;

                // Find a valid, non-missing, non-torso, hittable body part
                BodyPartRecord part = null;
                foreach (var p in innerPawn.RaceProps.body.AllParts)
                {
                    if (p == null || p == innerPawn.RaceProps.body.corePart) continue;
                    if (innerPawn.health.hediffSet.PartIsMissing(p)) continue;
                    if (p.depth != BodyPartDepth.Outside) continue;
                    if (p.coverageAbs <= 0.01f) continue;
                    if (p.IsInGroup(BodyPartGroupDefOf.FullHead)) continue;
                    part = p;
                    break;
                }

                if (part != null)
                {
                    // Remove the part
                    innerPawn.health.AddHediff(HediffDefOf.MissingBodyPart, part);

                    // Oil gain base value (tune as needed)
                    float baseOilGain = 40f;

                    // Get NeutroLoss severity (0 if not present)
                    float neutroLossSeverity = 0f;
                    var neutroLoss = actor.health?.hediffSet?.GetFirstHediffOfDef(MD_DefOf.VREA_NeutroLoss);
                    if (neutroLoss != null)
                        neutroLossSeverity = neutroLoss.Severity;

                    // Oil gain is reduced by severity (e.g., linear reduction, min 10%)
                    float reductionFactor = 1f - neutroLossSeverity;
                    reductionFactor = Mathf.Clamp(reductionFactor, 0.1f, 1f);

                    float oilGain = baseOilGain * reductionFactor;
                    gene.Oil = Mathf.Min(gene.Oil + oilGain, gene.InitialResourceMax);

                    // >>> GIVE THE HAPPY THOUGHT HERE <<<
                    if (actor.needs?.mood != null && MD_DefOf.MD_ConsumedCorpseNeutroamineOil_Happy != null)
                    {
                        actor.needs.mood.thoughts.memories.TryGainMemory(MD_DefOf.MD_ConsumedCorpseNeutroamineOil_Happy);
                    }

                    // If no more valid parts, destroy the corpse
                    bool hasRemainingParts = false;
                    foreach (var p in innerPawn.RaceProps.body.AllParts)
                    {
                        if (p == null || p == innerPawn.RaceProps.body.corePart) continue;
                        if (innerPawn.health.hediffSet.PartIsMissing(p)) continue;
                        if (p.depth != BodyPartDepth.Outside) continue;
                        if (p.coverageAbs <= 0.01f) continue;
                        hasRemainingParts = true;
                        break;
                    }
                    if (!hasRemainingParts)
                    {
                        corpseThing.Destroy(DestroyMode.Vanish);
                    }
                }
                else
                {
                    // No valid parts left, destroy the corpse
                    corpseThing.Destroy(DestroyMode.Vanish);
                }
                if (actor.MentalState is MentalState_BerserkOilCraving)
                {
                    actor.MentalState.RecoverFromState();
                }
            };
            finalize.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finalize;
        }
    }
}

