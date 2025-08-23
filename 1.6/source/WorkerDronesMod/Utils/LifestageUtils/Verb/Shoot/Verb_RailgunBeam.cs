using System;
using System.Collections.Generic;
using System.Linq;                       // ◀◀ ADDED
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WorkerDronesMod
{
    public class Verb_RailgunBeam : Verb
    {
        private IntVec3 beamStartCell;
        private IntVec3 beamEndCell;
        private List<IntVec3> beamLine = new List<IntVec3>();
        private MoteDualAttached beamMote;
        private Effecter beamEndEffecter;
        private Sustainer beamSustainer;

        protected override int ShotsPerBurst => verbProps.burstShotCount > 0 ? verbProps.burstShotCount : 10; // Default to 10 ticks

        public override void DrawHighlight(LocalTargetInfo target)
        {
            base.DrawHighlight(target);
            var cells = GetBeamLine(caster.Position, target.Cell, out _);
            if (cells.Any())
                GenDraw.DrawFieldEdges(cells, verbProps.highlightColor ?? Color.cyan);
        }

        protected override bool TryCastShot()
        {
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
                return false;

            // Only create visuals on the first shot of the burst
            if (burstShotsLeft == ShotsPerBurst)
            {
                beamStartCell = caster.Position;
                beamEndCell = GetBeamEndCell(beamStartCell, currentTarget.Cell, out beamLine);

                Vector3 startVec = beamStartCell.ToVector3Shifted();
                Vector3 endVec = beamEndCell.ToVector3Shifted();
                Vector3 offsetA = Vector3.zero;
                Vector3 offsetB = endVec - startVec;

                if (verbProps.beamMoteDef != null && beamMote == null)
                {
                    beamMote = MoteMaker.MakeInteractionOverlay(
                        verbProps.beamMoteDef,
                        caster,
                        new TargetInfo(beamEndCell, caster.Map, false),
                        offsetA,
                        offsetB
                    );
                }

                if (verbProps.beamEndEffecterDef != null && beamEndEffecter == null)
                {
                    beamEndEffecter = verbProps.beamEndEffecterDef.Spawn(beamEndCell, caster.Map, offsetB, 1f);
                }
                if (verbProps.soundCastBeam != null && beamSustainer == null)
                {
                    beamSustainer = verbProps.soundCastBeam.TrySpawnSustainer(SoundInfo.InMap(caster, MaintenanceType.PerTick));
                }
            }

            DamageAlongBeam();

            return true;
        }

        public override void BurstingTick()
        {
            // Recalculate the beam line in case a wall was destroyed
            beamEndCell = GetBeamEndCell(beamStartCell, currentTarget.Cell, out beamLine);

            Vector3 startVec = beamStartCell.ToVector3Shifted();
            Vector3 endVec = beamEndCell.ToVector3Shifted();
            Vector3 offsetA = Vector3.zero;
            Vector3 offsetB = endVec - startVec;

            if (beamMote != null)
            {
                beamMote.UpdateTargets(
                    new TargetInfo(beamStartCell, caster.Map, false),
                    new TargetInfo(beamEndCell, caster.Map, false),
                    offsetA,
                    offsetB
                );
                beamMote.Maintain();
            }
            if (beamEndEffecter != null)
            {
                beamEndEffecter.offset = offsetB;
                beamEndEffecter.EffectTick(new TargetInfo(beamEndCell, caster.Map, false), TargetInfo.Invalid);
                beamEndEffecter.ticksLeft--;
            }
            if (beamSustainer != null)
            {
                if (!beamSustainer.Ended)
                    beamSustainer.Maintain();
                else
                    beamSustainer = null;
            }

            DamageAlongBeam();
        }

        private void DamageAlongBeam()
        {
            foreach (var cell in beamLine)
            {
                if (!cell.InBounds(caster.Map) || cell == caster.Position)
                    continue;

                var things = cell.GetThingList(caster.Map).ToList();
                foreach (var thing in things)
                {
                    if (thing == caster) continue;
                    // Damage pawns, items, and buildings
                    if (thing is Pawn || thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Building)
                    {
                        float angleFlat = (cell - beamStartCell).AngleFlat;
                        DamageInfo dinfo = new DamageInfo(
                            verbProps.beamDamageDef ?? DamageDefOf.Bullet,
                            verbProps.beamTotalDamage,
                            -1f,
                            angleFlat
                        );
                        thing.TakeDamage(dinfo);

                        // If it's a solid wall, stop the beam here for this tick
                        if (thing.def.passability == Traversability.Impassable && thing.HitPoints > 0)
                            return;
                    }
                }
            }
        }

        // Returns the list of cells the beam passes through, and the endpoint (wall or max range)
        private IntVec3 GetBeamEndCell(IntVec3 start, IntVec3 target, out List<IntVec3> line)
        {
            line = new List<IntVec3>();
            IntVec3 end = start;
            Vector3 dir = (target - start).ToVector3().normalized;
            float maxDist = verbProps.range;
            for (float dist = 0f; dist <= maxDist; dist += 0.5f)
            {
                IntVec3 cell = (start.ToVector3Shifted() + dir * dist).ToIntVec3();
                if (!cell.InBounds(caster.Map))
                    break;
                if (!line.Contains(cell))
                    line.Add(cell);

                // If we hit a solid wall, stop here
                var things = cell.GetThingList(caster.Map);
                if (things.Any(t => t.def.passability == Traversability.Impassable && t != caster))
                {
                    end = cell;
                    break;
                }
                end = cell;
            }
            return end;
        }

        // For highlight preview
        private List<IntVec3> GetBeamLine(IntVec3 start, IntVec3 target, out IntVec3 end)
        {
            List<IntVec3> line;
            end = GetBeamEndCell(start, target, out line);
            return line;
        }

        public override void Reset()
        {
            base.Reset();
            if (beamEndEffecter != null)
                beamEndEffecter.Cleanup();
            beamEndEffecter = null;
            beamMote = null;
            if (beamSustainer != null)
            {
                beamSustainer.End();
                beamSustainer = null;
            }
        }
    }
}

