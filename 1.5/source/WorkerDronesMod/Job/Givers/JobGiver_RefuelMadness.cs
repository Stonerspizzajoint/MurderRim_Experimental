using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using VREAndroids;

namespace WorkerDronesMod
{
    public class JobGiver_RefuelMadness : JobGiver_Berserk
    {
        public static Job TryGiveRefuelMadnessJob(Pawn pawn)
        {
            if (!(pawn.MentalState is MentalState_RefuelMadness))
                return null;

            // --- 1) Refuel/corpse in shade? ---
            var thing = RefuelMadnessUtility.FindNearestNeutroamineOrCorpse(pawn);
            if (thing != null
                && SolverGeneUtility.IsThingSafeFromSun(thing))
            {
                var gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
                float missing = (gene != null)
                    ? (gene.TargetValue - gene.Value) / 2f
                    : 0f;

                Job job;
                if (thing is Corpse corp
                    && Utils.IsAndroid(corp.InnerPawn))
                {
                    job = JobMaker.MakeJob(MD_DefOf.MD_Job_RefuelWithCorpse, thing);
                    var plan = RefuelMadnessUtility.CalculateConsumptionPlan(corp.InnerPawn, missing);
                    job.count = Mathf.Max(1, plan.Count);
                    ConsumptionPlanStorage.PlanDict[job] = plan;
                }
                else
                {
                    job = JobMaker.MakeJob(MD_DefOf.MD_Job_RefuelWithNeutroamine, thing);
                    int count = Mathf.CeilToInt(missing / RefuelUtils.OilPerNeutroamineUnit);
                    job.count = Mathf.Max(1, Mathf.Min(count, thing.stackCount));
                }

                job.locomotionUrgency = LocomotionUrgency.Sprint;
                return job;
            }

            // --- 2) Melee android only if it's safe from sun ---
            var android = RefuelMadnessUtility.FindNearestAndroid(pawn);
            if (android != null
                && SolverGeneUtility.IsThingSafeFromSun(android))
            {
                var atkJob = JobMaker.MakeJob(JobDefOf.AttackMelee, android);
                atkJob.killIncappedTarget = true;
                atkJob.expiryInterval = int.MaxValue;
                atkJob.canBashDoors = true;
                atkJob.locomotionUrgency = LocomotionUrgency.Sprint;
                return atkJob;
            }

            // --- 3) Fallback: attack any pawn, but only if shaded ---
            var target = RefuelMadnessUtility.FindNearestAnyPawn(pawn);
            if (target != null
                && SolverGeneUtility.IsThingSafeFromSun(target))
            {
                var atkJob = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
                atkJob.killIncappedTarget = true;
                atkJob.expiryInterval = int.MaxValue;
                atkJob.canBashDoors = true;
                atkJob.locomotionUrgency = LocomotionUrgency.Sprint;
                return atkJob;
            }

            // Nothing safe—don’t send them into sunlight.
            return null;
        }

        protected override Job TryGiveJob(Pawn pawn) => TryGiveRefuelMadnessJob(pawn);

        protected override bool IsGoodTarget(Thing thing)
        {
            return thing is Pawn p
                   && p.Spawned
                   && !p.Downed
                   && !p.IsPsychologicallyInvisible()
                   && SolverGeneUtility.IsThingSafeFromSun(p);
        }
    }
}


