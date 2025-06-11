using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse.AI;
using Verse;
using UnityEngine;
using VREAndroids;

namespace WorkerDronesMod
{
    public class JobGiver_RefuelMadness : JobGiver_Berserk
    {
        public static Job TryGiveRefuelMadnessJob(Pawn pawn)
        {
            if (!(pawn.MentalState is MentalState_RefuelMadness))
                return null;

            if (Rand.Value < 0.5f)
            {
                var waitJob = JobMaker.MakeJob(JobDefOf.Wait_Combat);
                waitJob.expiryInterval = 90;
                waitJob.canUseRangedWeapon = false;
                return waitJob;
            }

            if (pawn.TryGetAttackVerb(null, false, false) == null)
                return null;

            var thing = RefuelMadnessUtility.FindNearestNeutroamineOrCorpse(pawn);
            if (thing != null)
            {
                Job job;
                var gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
                float missing = gene != null ? (gene.TargetValue - gene.Value) / 2f : 0f;

                if (thing is Corpse corp && corp.InnerPawn != null && Utils.IsAndroid(corp.InnerPawn))
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
                    // Limit per stack like vanilla: take only what's available
                    count = Mathf.Min(count, thing.stackCount);
                    if (count <= 0) count = 1;
                    job.count = count; // allow gathering across stacks
                }
                return job;
            }

            var android = RefuelMadnessUtility.FindNearestAndroid(pawn);
            if (android != null)
            {
                var atkJob = JobMaker.MakeJob(JobDefOf.AttackMelee, android);
                atkJob.killIncappedTarget = true;
                atkJob.expiryInterval = Rand.Range(420, 900);
                atkJob.canBashDoors = true;
                return atkJob;
            }

            var target = RefuelMadnessUtility.FindNearestAnyPawn(pawn);
            if (target != null)
            {
                var atkJob = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
                atkJob.killIncappedTarget = true;
                atkJob.expiryInterval = Rand.Range(420, 900);
                atkJob.canBashDoors = true;
                return atkJob;
            }

            return null;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            return TryGiveRefuelMadnessJob(pawn);
        }

        protected override bool IsGoodTarget(Thing thing)
        {
            return thing is Pawn p && p.Spawned && !p.Downed && !p.IsPsychologicallyInvisible();
        }
    }
}
