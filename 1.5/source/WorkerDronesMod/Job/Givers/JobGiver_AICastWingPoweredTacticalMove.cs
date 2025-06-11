using System.Linq;
using RimWorld;
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

        private int lastJumpTick = -CooldownTicks;

        protected override LocalTargetInfo GetTarget(Pawn caster, Ability abilityInstance)
        {
            // Null/map/job-target guard
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

            // Gather all valid hostile pawns once
            float surroundSq = SurroundRadius * SurroundRadius;
            float weaponRangeSq = weaponRange * weaponRange;
            var enemies = map
                .attackTargetsCache
                .GetPotentialTargetsFor(caster)
                .OfType<Pawn>()
                .Where(p =>
                    p.Spawned && !p.Dead && !p.Downed &&
                    p.Faction != null && p.Faction.HostileTo(caster.Faction)
                )
                .ToList();
            if (enemies.Count == 0)
                return LocalTargetInfo.Invalid;

            // Are we surrounded?
            bool isSurrounded = enemies.Count(e =>
                (e.Position - caster.Position).LengthHorizontalSquared <= surroundSq
            ) >= SurroundEnemyCountThreshold;

            // Enforce cooldown unless surrounded
            if (!isSurrounded && now - lastJumpTick < CooldownTicks)
                return LocalTargetInfo.Invalid;

            // How far we can jump
            float jumpRange = abilityInstance.verb.verbProps.range;

            // Try random candidate cells
            for (int i = 0; i < CandidateAttempts; i++)
            {
                Vector2 rnd = UnityEngine.Random.insideUnitCircle * jumpRange;
                var dest = caster.Position + new IntVec3((int)rnd.x, 0, (int)rnd.y);

                // Must be on-map and standable
                if (!dest.InBounds(map)
                 || !map.terrainGrid.TerrainAt(dest).affordances.Contains(TerrainAffordanceDefOf.Light))
                    continue;

                // At least one enemy falls within weapon range
                if (enemies.Any(e =>
                    (e.Position - dest).LengthHorizontalSquared <= weaponRangeSq
                ))
                {
                    if (!isSurrounded)
                        lastJumpTick = now;
                    return new LocalTargetInfo(dest);
                }
            }

            return LocalTargetInfo.Invalid;
        }
    }
}


