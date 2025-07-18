using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class JobGiver_RefuelWithNeutroamine : ThinkNode_JobGiver
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
                return 9.1f;

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

            // Try Neutroamine first
            int count;
            Thing neutro = GetNeutroamineForRefuel(pawn, out count);
            if (neutro != null)
            {
                Job job = JobMaker.MakeJob(MD_DefOf.MD_Job_RefuelWithNeutroamine, neutro);
                job.count = count;
                return job;
            }

            // Try android corpse as fallback
            Thing corpse = GetAndroidCorpseForRefuel(pawn);
            if (corpse != null)
            {
                Job job = JobMaker.MakeJob(MD_DefOf.MD_Job_RefuelWithCorpse, corpse);
                job.count = 1;
                return job;
            }

            return null;
        }

        public static Thing GetNeutroamineForRefuel(Pawn pawn, out int count)
        {
            count = 0;
            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene == null)
                return null;

            var geneExt = gene.def.GetModExtension<SolverGeneExtension>();

            float oilNeeded = gene.InitialResourceMax - gene.Oil;
            float oilPerNeutro = 10f; // Must match JobDriver_ConsumeNeutroamine
            int needed = Mathf.CeilToInt(oilNeeded / oilPerNeutro);

            IEnumerable<Thing> candidates = pawn.Map.listerThings.ThingsOfDef(MD_DefOf.Neutroamine)
                .Where(t => !t.IsForbidden(pawn)
                    && pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly)
                    && SolarUtil.IsThingSafe(t, geneExt)); // Sunlight safety check

            Thing best = GenClosest.ClosestThing_Global_Reachable(
                pawn.Position, pawn.Map, candidates, PathEndMode.Touch,
                TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f);

            if (best != null)
            {
                count = Mathf.Min(needed, best.stackCount);
            }

            return best;
        }

        // Helper to find a valid android corpse
        private static Thing GetAndroidCorpseForRefuel(Pawn pawn)
        {
            // Only corpses of androids, not forbidden, reservable, reachable
            foreach (var thing in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
            {
                if (thing is Corpse corpse
                    && corpse.InnerPawn != null
                    && VREAndroids.Utils.IsAndroid(corpse.InnerPawn)
                    && !thing.IsForbidden(pawn)
                    && pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Deadly))
                {
                    return thing;
                }
            }
            return null;
        }
    }
}
