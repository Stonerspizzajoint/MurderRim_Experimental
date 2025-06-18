using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using VREAndroids;

namespace WorkerDronesMod
{
    public static class ConsumptionPlanStorage
    {
        public static Dictionary<Job, List<string>> PlanDict = new Dictionary<Job, List<string>>();
    }

    public class JobGiver_RefuelNeutroamineOil : ThinkNode_JobGiver
    {
        private static Dictionary<Pawn, int> pawnLastRefuelTick;
        private const int RefuelAttemptCooldownTicks = 300;

        static JobGiver_RefuelNeutroamineOil()
        {
            pawnLastRefuelTick = new Dictionary<Pawn, int>();
        }

        public static void ResetStaticData()
        {
            pawnLastRefuelTick = new Dictionary<Pawn, int>();
        }

        public override float GetPriority(Pawn pawn)
        {
            if (!ModsConfig.BiotechActive) return 0f;
            if (pawn.Downed || pawn.InMentalState) return 0f;

            int tick = Find.TickManager.TicksGame;
            if (pawnLastRefuelTick.TryGetValue(pawn, out int last) && tick - last < RefuelAttemptCooldownTicks)
                return 0f;

            var gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
            if (gene == null) return 0f;
            if (gene.Value >= gene.TargetValue) return 0f;
            if (!gene.neutroamineAllowed) return 0f;

            float shortage = gene.InitialResourceMax - gene.Value;
            return 9.1f + shortage;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!ModsConfig.BiotechActive) return null;
            if (pawn.Downed || pawn.InMentalState) return null;

            int tick = Find.TickManager.TicksGame;
            if (pawnLastRefuelTick.TryGetValue(pawn, out int last) && tick - last < RefuelAttemptCooldownTicks)
                return null;
            pawnLastRefuelTick[pawn] = tick;

            var gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
            if (gene == null || gene.Value >= gene.TargetValue || !gene.neutroamineAllowed)
                return null;

            float missing = gene.InitialResourceMax - gene.Value;

            // --- Primary: Neutroamine ingestion ---
            if (missing > 0f)
            {
                var neutroDef = MD_DefOf.Neutroamine;
                var jobDefNeutro = MD_DefOf.MD_Job_RefuelWithNeutroamine;
                Thing target = GenClosest.ClosestThingReachable(
                    pawn.Position, pawn.Map, ThingRequest.ForDef(neutroDef), PathEndMode.ClosestTouch,
                    TraverseParms.For(pawn), 9999f, t =>
                        SolverGeneUtility.IsValidConsumptionTarget(t)
                );
                if (target != null)
                {
                    int count = Mathf.CeilToInt(missing / JobDriver_RefuelWithNeutroamineOil.OilPerNeutroamineUnit);
                    count = Mathf.Min(count, target.stackCount);
                    if (count > 0)
                    {
                        Job job = JobMaker.MakeJob(jobDefNeutro, target);
                        job.count = count;
                        return job;
                    }
                }
            }

            // --- Fallback: Android corpse consumption ---
            if (missing > 0f)
            {
                var jobDefCorpse = MD_DefOf.MD_Job_RefuelWithCorpse;
                Predicate<Thing> validator = t =>
                {
                    if (t is Corpse c && c.InnerPawn != null && c.InnerPawn.IsAndroid())
                    {
                        return SolverGeneUtility.IsValidConsumptionTarget(t);
                    }
                    return false;
                };
                Thing corpse = GenClosest.ClosestThingReachable(
                    pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Corpse), PathEndMode.Touch,
                    TraverseParms.For(pawn), 9999f, validator
                );
                if (corpse is Corpse c2)
                {
                    var plan = CalculateConsumptionPlan(c2.InnerPawn, missing);
                    if (plan.Count > 0)
                    {
                        Job job = JobMaker.MakeJob(jobDefCorpse, corpse);
                        job.count = plan.Count;
                        ConsumptionPlanStorage.PlanDict[job] = plan;
                        return job;
                    }
                }
            }

            return null;
        }

        public List<string> CalculateConsumptionPlan(Pawn android, float missingOil)
        {
            var parts = android.health.hediffSet.GetNotMissingParts().ToList();
            parts.RemoveAll(p => p.def.defName.Equals("Ribcage", StringComparison.OrdinalIgnoreCase));
            if (parts.Count > 1)
                parts.RemoveAll(p => p.def.defName.Equals("Torso", StringComparison.OrdinalIgnoreCase));

            List<string> plan = new List<string>();
            float total = 0f;
            var vital = parts.Where(p => new[] { "Heart", "Liver", "Kidney", "Lung", "Stomach" }.Contains(p.def.defName));
            foreach (var p in vital)
            {
                plan.Add(p.def.defName);
                total += RefuelUtils.OilPerUnitOrgan;
                if (total >= missingOil) break;
            }
            if (total < missingOil)
            {
                foreach (var p in parts.Except(vital))
                {
                    plan.Add(p.def.defName);
                    total += RefuelUtils.OilPerUnitDefault;
                    if (total >= missingOil) break;
                }
            }
            return plan;
        }
    }
}


