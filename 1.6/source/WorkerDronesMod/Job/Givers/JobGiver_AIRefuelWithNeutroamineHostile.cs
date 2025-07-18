using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class JobGiver_AIRefuelWithNeutroamineHostile : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            if (!ModsConfig.BiotechActive)
                return 0f;

            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene == null)
                return 0f;

            // Only high priority if oil is below threshold
            if (gene.Oil < gene.InitialResourceMax * gene.OilRefuelThreshold)
                return 9.2f; // Slightly higher than neutroamine jobgiver

            return 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!ModsConfig.BiotechActive)
                return null;

            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene == null)
                return null;

            if (gene.Oil >= gene.InitialResourceMax * gene.OilRefuelThreshold)
                return null;

            // 1. Prioritize android corpse
            Thing corpse = GetHostileAndroidCorpseForRefuel(pawn);
            if (corpse != null)
            {
                Job job = JobMaker.MakeJob(MD_DefOf.MD_Job_RefuelWithCorpse, corpse);
                job.count = 1;
                return job;
            }

            // 2. If no corpse, look for downed hostile android pawn to finish off
            Pawn downedAndroid = GetHostileDownedAndroidPawnForRefuel(pawn);
            if (downedAndroid != null)
            {
                Job finishJob = JobMaker.MakeJob(JobDefOf.AttackMelee, downedAndroid);
                finishJob.killIncappedTarget = true;
                finishJob.canBashDoors = false;
                return finishJob;
            }

            // 3. Fallback: Try Neutroamine
            int count;
            Thing neutro = JobGiver_RefuelWithNeutroamine.GetNeutroamineForRefuel(pawn, out count);
            if (neutro != null)
            {
                Job job = JobMaker.MakeJob(MD_DefOf.MD_Job_RefuelWithNeutroamine, neutro);
                job.count = count;
                return job;
            }

            return null;
        }

        // Finds a valid hostile android corpse
        private static Thing GetHostileAndroidCorpseForRefuel(Pawn pawn)
        {
            var geneExt = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>()?.def.GetModExtension<SolverGeneExtension>();
            foreach (var thing in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
            {
                if (thing is Corpse corpse
                    && corpse.InnerPawn != null
                    && VREAndroids.Utils.IsAndroid(corpse.InnerPawn)
                    && corpse.InnerPawn.Faction != null
                    && corpse.InnerPawn.Faction.HostileTo(pawn.Faction)
                    && !thing.IsForbidden(pawn)
                    && pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Deadly)
                    && SolarUtil.IsThingSafe(thing, geneExt)) // <-- Sunlight safety check
                {
                    return thing;
                }
            }
            return null;
        }

        // Finds a valid downed hostile android pawn
        private static Pawn GetHostileDownedAndroidPawnForRefuel(Pawn pawn)
        {
            var geneExt = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>()?.def.GetModExtension<SolverGeneExtension>();
            foreach (var potential in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (potential != null
                    && VREAndroids.Utils.IsAndroid(potential)
                    && potential.Downed
                    && !potential.Dead
                    && potential.Faction != null
                    && potential.Faction.HostileTo(pawn.Faction)
                    && !potential.IsForbidden(pawn)
                    && pawn.CanReserveAndReach(potential, PathEndMode.OnCell, Danger.Deadly)
                    && SolarUtil.IsThingSafe(potential, geneExt)) // <-- Sunlight safety check
                {
                    return potential;
                }
            }
            return null;
        }

    }
}

