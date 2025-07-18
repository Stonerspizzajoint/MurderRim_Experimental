using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;
using Verse.AI;

namespace WorkerDronesMod
{
    public static class JumpAbilityAIUtil
    {

        public static bool IsCasterUsingRangedHediff(Pawn caster)
        {
            var ext = GetHediffSwitcherExtension(caster);
            if (ext == null) return false;

            foreach (var hand in caster.RaceProps.body.GetPartsWithDef(MD_DefOf.Hand))
            {
                var hediff = caster.health.hediffSet.hediffs.FirstOrDefault(h => h.Part == hand);
                if (hediff != null)
                {
                    var option = ext.selectableHediffs.FirstOrDefault(o => o.Hediff == hediff.def);
                    if (option != null && option.IsRanged)
                        return true;
                }
            }
            return false;
        }

        public static bool IsCasterUsingMeleeHediff(Pawn caster)
        {
            var ext = GetHediffSwitcherExtension(caster);
            if (ext == null) return false;

            foreach (var hand in caster.RaceProps.body.GetPartsWithDef(MD_DefOf.Hand))
            {
                var hediff = caster.health.hediffSet.hediffs.FirstOrDefault(h => h.Part == hand);
                if (hediff != null)
                {
                    var option = ext.selectableHediffs.FirstOrDefault(o => o.Hediff == hediff.def);
                    if (option != null && option.IsMelee)
                        return true;
                }
            }
            return false;
        }

        // Helper to get the extension from the pawn's ability
        public static ModExtension_AbilityHediffSwitcher GetHediffSwitcherExtension(Pawn pawn)
        {
            var ability = pawn.abilities?.AllAbilitiesForReading
                .OfType<Ability_HediffSwitcher>()
                .FirstOrDefault();
            return ability?.def?.GetModExtension<ModExtension_AbilityHediffSwitcher>();
        }

        public static bool IsPawnFleeing(Pawn pawn)
        {
            var job = pawn.CurJob;
            return job != null && job.def.defName.IndexOf("Flee", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsRangedThreat(Pawn pawn)
        {
            return pawn.equipment?.Primary != null &&
                   pawn.equipment.Primary.def.Verbs.Any(v => v.range > 1f);
        }

        public static Pawn FindBestJumpTargetInRange(Pawn caster, float maxRange)
        {
            if (caster?.Map == null) return null;

            Pawn bestFleeing = null;
            Pawn bestRanged = null;
            float bestFleeingDist = -1f;
            float bestRangedDist = -1f;

            foreach (var target in caster.Map.attackTargetsCache.GetPotentialTargetsFor(caster))
            {
                if (target is Pawn enemy &&
                    enemy.Spawned && !enemy.Dead && !enemy.Downed &&
                    enemy.Faction != null && enemy.Faction.HostileTo(caster.Faction))
                {
                    float dist = (enemy.Position - caster.Position).LengthHorizontal;
                    if (dist > maxRange)
                        continue;

                    if (IsPawnFleeing(enemy) && dist > bestFleeingDist)
                    {
                        bestFleeing = enemy;
                        bestFleeingDist = dist;
                    }
                    else if (IsRangedThreat(enemy) && dist > bestRangedDist)
                    {
                        bestRanged = enemy;
                        bestRangedDist = dist;
                    }
                }
            }

            return bestFleeing ?? bestRanged;
        }

        public static Pawn FindClosestHostilePawn(Pawn caster)
        {
            if (caster?.Map == null) return null;

            Pawn closest = null;
            float closestDist = float.MaxValue;

            foreach (var target in caster.Map.attackTargetsCache.GetPotentialTargetsFor(caster))
            {
                if (target is Pawn enemy &&
                    enemy.Spawned && !enemy.Dead && !enemy.Downed &&
                    enemy.Faction != null && enemy.Faction.HostileTo(caster.Faction))
                {
                    float dist = (enemy.Position - caster.Position).LengthHorizontal;
                    if (dist < closestDist)
                    {
                        closest = enemy;
                        closestDist = dist;
                    }
                }
            }
            return closest;
        }

        public static IntVec3? FindShootingPositionWithCover(Pawn caster, Pawn target, float weaponRange, float minDistanceFromEnemy = 8f, int searchRadius = 20)
        {
            if (caster?.Map == null || target == null) return null;

            var map = caster.Map;
            var root = caster.Position;
            var validCells = new List<IntVec3>();

            foreach (var cell in GenRadial.RadialCellsAround(root, searchRadius, true))
            {
                if (!cell.InBounds(map) || !cell.Standable(map)) continue;

                // Ensure cell is far enough from all hostiles
                bool tooCloseToAnyEnemy = false;
                foreach (var hostile in caster.Map.mapPawns.AllPawnsSpawned)
                {
                    if (hostile != null && hostile.Faction != null && hostile.Faction.HostileTo(caster.Faction) && hostile.Spawned && !hostile.Dead && !hostile.Downed)
                    {
                        if (cell.DistanceTo(hostile.Position) < minDistanceFromEnemy)
                        {
                            tooCloseToAnyEnemy = true;
                            break;
                        }
                    }
                }
                if (tooCloseToAnyEnemy) continue;

                // Must be within weapon range
                if (cell.DistanceTo(target.Position) > weaponRange) continue;

                // Must have line of sight to target
                if (!GenSight.LineOfSight(cell, target.Position, map)) continue;

                // Must be reachable
                if (!map.reachability.CanReach(caster.Position, cell, PathEndMode.OnCell, TraverseParms.For(caster))) continue;

                // Prefer cover
                bool hasCover = false;
                foreach (var dir in GenAdj.CardinalDirections)
                {
                    var coverCell = cell + dir;
                    if (coverCell.InBounds(map) && coverCell.GetCover(map) != null)
                    {
                        hasCover = true;
                        break;
                    }
                }

                if (hasCover)
                    validCells.Insert(0, cell); // Prefer cover
                else
                    validCells.Add(cell);
            }



            return validCells.FirstOrDefault();
        }

        public static Pawn FindBestRangedTarget(Pawn caster, float maxRange)
        {
            if (caster?.Map == null) return null;
            Pawn best = null;
            float bestDist = -1f;
            foreach (var target in caster.Map.attackTargetsCache.GetPotentialTargetsFor(caster))
            {
                if (target is Pawn enemy &&
                    enemy.Spawned && !enemy.Dead && !enemy.Downed &&
                    enemy.Faction != null && enemy.Faction.HostileTo(caster.Faction))
                {
                    float dist = (enemy.Position - caster.Position).LengthHorizontal;
                    if (dist > maxRange) continue;
                    if (dist > bestDist)
                    {
                        best = enemy;
                        bestDist = dist;
                    }
                }
            }
            return best;
        }

        public static bool IsCasterUsingRangedWeapon(Pawn caster)
        {
            // Check equipment
            var eq = caster.equipment?.Primary;
            if (eq != null && eq.def.IsRangedWeapon)
                return true;

            // Check hediff
            return IsCasterUsingRangedHediff(caster);
        }

        public static bool IsCasterAllowedToJump(Pawn caster)
        {
            var eq = caster.equipment?.Primary;
            if (eq == null) return true;
            if (eq.def.Verbs.Any(v => v.IsMeleeAttack))
                return true;

            // Check hediff
            return IsCasterUsingMeleeHediff(caster);
        }
    }
}
