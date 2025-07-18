using System.Linq;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    public class JobGiver_AICastWingPoweredTacticalMove : JobGiver_AICastAbility
    {
        private const int CooldownTicks = 600;
        private const int CandidateAttempts = 20;
        private const float SurroundRadius = 2f;
        private const int SurroundEnemyCountThreshold = 4;

        // Per-caster cooldowns to allow multiple pawns to use this jobgiver independently
        private static readonly Dictionary<Pawn, int> lastJumpTick = new Dictionary<Pawn, int>();

        protected override LocalTargetInfo GetTarget(Pawn caster, Ability abilityInstance)
        {
            if (caster?.Map == null || caster.CurJob == null || !caster.CurJob.targetA.IsValid)
                return LocalTargetInfo.Invalid;

            var map = caster.Map;
            int now = Find.TickManager.TicksGame;

            // Weapon check
            var primary = caster.equipment?.Primary;
            if (primary == null)
                return LocalTargetInfo.Invalid;
            float weaponRange = primary.def.Verbs[0].range;
            if (weaponRange <= 0f)
                return LocalTargetInfo.Invalid;

            float surroundSq = SurroundRadius * SurroundRadius;
            float weaponRangeSq = weaponRange * weaponRange;

            // Find all valid hostile pawns in a single pass, and check for "surrounded" status
            int nearbyEnemies = 0;
            List<Pawn> enemies = new List<Pawn>();
            foreach (var target in map.attackTargetsCache.GetPotentialTargetsFor(caster))
            {
                if (target is Pawn enemy &&
                    enemy.Spawned && !enemy.Dead && !enemy.Downed &&
                    enemy.Faction != null && enemy.Faction.HostileTo(caster.Faction))
                {
                    enemies.Add(enemy);
                    if ((enemy.Position - caster.Position).LengthHorizontalSquared <= surroundSq)
                        nearbyEnemies++;
                }
            }
            if (enemies.Count == 0)
                return LocalTargetInfo.Invalid;

            bool isSurrounded = nearbyEnemies >= SurroundEnemyCountThreshold;

            // Enforce cooldown unless surrounded
            int lastTick = lastJumpTick.TryGetValue(caster, out int t) ? t : -CooldownTicks;
            if (!isSurrounded && now - lastTick < CooldownTicks)
                return LocalTargetInfo.Invalid;

            float jumpRange = abilityInstance.verb.verbProps.range;

            // Try random candidate cells
            for (int i = 0; i < CandidateAttempts; i++)
            {
                Vector2 rnd = UnityEngine.Random.insideUnitCircle * jumpRange;
                var dest = caster.Position + new IntVec3(Mathf.RoundToInt(rnd.x), 0, Mathf.RoundToInt(rnd.y));

                if (!dest.InBounds(map))
                    continue;

                // Must be standable and have the right affordance
                var terrain = map.terrainGrid.TerrainAt(dest);
                if (!terrain.affordances.Contains(TerrainAffordanceDefOf.Light) || !dest.Standable(map))
                    continue;

                // At least one enemy falls within weapon range
                bool enemyInRange = false;
                foreach (var enemy in enemies)
                {
                    if ((enemy.Position - dest).LengthHorizontalSquared <= weaponRangeSq)
                    {
                        enemyInRange = true;
                        break;
                    }
                }
                if (enemyInRange)
                {
                    if (!isSurrounded)
                        lastJumpTick[caster] = now;
                    return new LocalTargetInfo(dest);
                }
            }

            return LocalTargetInfo.Invalid;
        }
    }
}


