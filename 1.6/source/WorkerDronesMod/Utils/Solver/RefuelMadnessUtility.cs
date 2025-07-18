using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using VREAndroids;

namespace WorkerDronesMod
{
    public static class RefuelMadnessUtility
    {
        public static Thing FindNearestNeutroamineOrCorpse(Pawn pawn)
        {
            var neutro = pawn.Map.listerThings.ThingsOfDef(MD_DefOf.Neutroamine)
                .Where(t => !t.IsForbidden(pawn) && !t.Position.Fogged(pawn.Map) && pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly))
                .OrderBy(t => pawn.Position.DistanceToSquared(t.Position))
                .FirstOrDefault();
            if (neutro != null)
                return neutro;

            var corpse = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse)
                .Where(t => t is Corpse c && c.InnerPawn != null && Utils.IsAndroid(c.InnerPawn) && pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly))
                .OrderBy(t => pawn.Position.DistanceToSquared(t.Position))
                .FirstOrDefault();
            return corpse;
        }

        public static Pawn FindNearestAndroid(Pawn pawn)
        {
            return pawn.Map.mapPawns.AllPawnsSpawned
                .Where(p => p != pawn && Utils.IsAndroid(p) && pawn.CanReach(p, PathEndMode.Touch, Danger.Deadly))
                .OrderBy(p => pawn.Position.DistanceToSquared(p.Position))
                .FirstOrDefault();
        }

        public static Pawn FindNearestAnyPawn(Pawn pawn)
        {
            return pawn.Map.mapPawns.AllPawnsSpawned
                .Where(p => p != pawn && !p.Dead && !p.Downed && pawn.CanReach(p, PathEndMode.Touch, Danger.Deadly))
                .OrderBy(p => pawn.Position.DistanceToSquared(p.Position))
                .FirstOrDefault();
        }

        public static List<string> CalculateConsumptionPlan(Pawn android, float missingOil)
        {
            var parts = android.health.hediffSet.GetNotMissingParts().ToList();

            List<string> plan = new List<string>();
            float total = 0f;
            var vitalNames = new[] { "Heart", "Liver", "Kidney", "Lung", "Stomach" };
            var vital = parts.Where(p => vitalNames.Contains(p.def.defName));
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
