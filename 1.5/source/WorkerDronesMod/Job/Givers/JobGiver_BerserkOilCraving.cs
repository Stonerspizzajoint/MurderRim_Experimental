using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using VREAndroids;

namespace WorkerDronesMod
{
    public class JobGiver_BerserkOilCraving : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!(pawn.MentalState is MentalState_BerserkOilCraving))
                return null;

            // 1. Find all valid android targets (including downed)
            List<Pawn> potentialTargets = pawn.Map.mapPawns.AllPawnsSpawned
                .Where(p =>
                    p != pawn &&
                    !p.Dead &&
                    p.Spawned &&
                    pawn.CanReach(p, PathEndMode.Touch, Danger.Deadly) &&
                    pawn.CanReserve(p) &&
                    Utils.IsAndroid(p) &&
                    (p.Faction == null || p.Faction.def != MD_DefOf.MD_DisassemblyDronesFaction) &&
                    !(pawn.Faction != null && p.Faction != null &&
                      pawn.Faction.def == MD_DefOf.MD_DisassemblyDronesFaction &&
                      p.Faction.def == MD_DefOf.MD_DisassemblyDronesFaction)
                ).ToList();

            // 2. Find the closest valid android corpse
            Corpse androidCorpse = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse)
                .OfType<Corpse>()
                .Where(c =>
                    c.InnerPawn != null &&
                    Utils.IsAndroid(c.InnerPawn) &&
                    (c.InnerPawn.Faction == null || c.InnerPawn.Faction.def != MD_DefOf.MD_DisassemblyDronesFaction) &&
                    !c.IsForbidden(pawn) &&
                    pawn.CanReserveAndReach(c, PathEndMode.Touch, Danger.Deadly)
                )
                .OrderBy(c => c.Position.DistanceTo(pawn.Position))
                .FirstOrDefault();

            // 3. Find the closest Neutroamine
            Thing neutroamine = pawn.Map.listerThings.ThingsOfDef(MD_DefOf.Neutroamine)
                .Where(t => !t.IsForbidden(pawn) && pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly))
                .OrderBy(t => t.Position.DistanceTo(pawn.Position))
                .FirstOrDefault();

            // 4. Find the closest living or downed android pawn
            Pawn closestAndroidPawn = potentialTargets
                .OrderBy(p => p.Position.DistanceTo(pawn.Position))
                .FirstOrDefault();

            float closePawnDist = closestAndroidPawn != null ? closestAndroidPawn.Position.DistanceTo(pawn.Position) : float.MaxValue;
            float corpseDist = androidCorpse != null ? androidCorpse.Position.DistanceTo(pawn.Position) : float.MaxValue;
            float neutroDist = neutroamine != null ? neutroamine.Position.DistanceTo(pawn.Position) : float.MaxValue;

            // 5. If a pawn is within 8 tiles, attack them (ignore corpse/neutroamine unless even closer)
            if (closestAndroidPawn != null && closePawnDist <= 8f &&
                (closePawnDist <= corpseDist && closePawnDist <= neutroDist))
            {
                return MeleeOrFinishOffJob(pawn, closestAndroidPawn);
            }

            // 6. Otherwise, consume the closest of corpse or neutroamine if either is closer than the pawn
            if (corpseDist < closePawnDist && corpseDist <= neutroDist && androidCorpse != null)
            {
                Job job = JobMaker.MakeJob(MD_DefOf.MD_Job_RefuelWithCorpse, androidCorpse);
                job.count = 1;
                GiveConsumedCorpseThought(pawn);
                return job;
            }
            if (neutroDist < closePawnDist && neutroamine != null)
            {
                Job job = JobMaker.MakeJob(MD_DefOf.MD_Job_RefuelWithNeutroamine, neutroamine);
                job.count = 1;
                return job;
            }

            // 7. If nothing else, attack the closest android pawn (even if downed)
            if (closestAndroidPawn != null)
            {
                return MeleeOrFinishOffJob(pawn, closestAndroidPawn);
            }

            // 8. Fallback: attack the closest non-android, non-dead, spawned pawn
            Pawn fallbackTarget = pawn.Map.mapPawns.AllPawnsSpawned
                .Where(p =>
                    p != pawn &&
                    !p.Dead &&
                    p.Spawned &&
                    pawn.CanReach(p, PathEndMode.Touch, Danger.Deadly) &&
                    pawn.CanReserve(p) &&
                    !Utils.IsAndroid(p)
                )
                .OrderBy(p => p.Position.DistanceTo(pawn.Position))
                .FirstOrDefault();

            if (fallbackTarget != null)
            {
                return MeleeOrFinishOffJob(pawn, fallbackTarget);
            }

            return null;
        }


        private Job MeleeOrFinishOffJob(Pawn pawn, Pawn target)
        {
            var job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.killIncappedTarget = true;
            return job;
        }

        private void GiveConsumedCorpseThought(Pawn pawn)
        {
            if (pawn.needs?.mood != null && MD_DefOf.MD_ConsumedCorpseNeutroamineOil_Happy != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(MD_DefOf.MD_ConsumedCorpseNeutroamineOil_Happy);
            }
        }
    }
}

