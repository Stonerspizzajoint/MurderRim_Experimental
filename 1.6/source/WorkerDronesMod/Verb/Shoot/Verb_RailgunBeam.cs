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
        private List<Vector3> path = new List<Vector3>();
        private int ticksToNextPathStep;
        private Vector3 finalEndpoint = Vector3.zero;

        protected override int ShotsPerBurst => this.verbProps.burstShotCount;

        public override float? AimAngleOverride
        {
            get
            {
                if (this.state != VerbState.Bursting)
                    return null;
                Vector3 endpoint = this.InterpolatedPosition;
                return (endpoint - this.caster.DrawPos).AngleFlat();
            }
        }

        public Vector3 InterpolatedPosition
        {
            get
            {
                Vector3 casterPos = this.caster.Position.ToVector3Shifted();
                Vector3 targetPoint = this.currentTarget.HasThing
                    ? this.currentTarget.CenterVector3
                    : this.currentTarget.Cell.ToVector3Shifted();
                Vector3 direction = (targetPoint - casterPos).Yto0().normalized;
                return casterPos + direction * this.verbProps.range;
            }
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (!target.IsValid)
                return;

            // Draw the range ring.
            GenDraw.DrawRadiusRing(this.caster.Position, this.verbProps.range);

            // Draw the beam path.
            List<Vector3> previewPath = new List<Vector3>();
            HashSet<IntVec3> dummyPathCells = new HashSet<IntVec3>();
            CalculateBeamPath(target.CenterVector3, previewPath, dummyPathCells, locked: true, targetIsSolid: false);

            if (previewPath.Count >= 2)
            {
                Color lineColor = this.verbProps.highlightColor ?? Color.white;
                for (int i = 0; i < previewPath.Count - 1; i++)
                {
                    DrawColoredLine(previewPath[i], previewPath[i + 1], lineColor, 1.5f);
                }
            }
        }

        private void CalculateBeamPath(Vector3 targetCenter, List<Vector3> pathList, HashSet<IntVec3> pathCellsList, bool locked, bool targetIsSolid)
        {
            pathList.Clear();
            pathCellsList.Clear();

            Vector3 casterPos = this.caster.Position.ToVector3Shifted();
            Vector3 direction = (targetCenter - casterPos).Yto0().normalized;
            Vector3 startPoint = casterPos + direction * (this.verbProps.beamStartOffset != 0f ? this.verbProps.beamStartOffset : 0f);
            Vector3 candidateEnd = casterPos + direction * this.verbProps.range;

            Vector3 worldFinal = CalculateBeamEndpoint(startPoint, candidateEnd, targetIsSolid);

            IntVec3 startCell = startPoint.ToIntVec3();
            IntVec3 endCell = worldFinal.ToIntVec3();

            List<IntVec3> beamCells = GetCellsAlongBeam(startCell, endCell);
            foreach (var cell in beamCells)
            {
                pathCellsList.Add(cell);
                pathList.Add(cell.ToVector3Shifted());
            }
        }

        private Vector3 CalculateBeamEndpoint(Vector3 startPoint, Vector3 candidateEnd, bool targetIsSolid)
        {
            if (targetIsSolid)
                return candidateEnd;

            IntVec3 startCell = startPoint.ToIntVec3();
            IntVec3 targetCell = candidateEnd.ToIntVec3();
            Map map = caster.Map;

            var last = GenSight.LastPointOnLineOfSight(startCell, targetCell, c => c.InBounds(map) && c.CanBeSeenOverFast(map), true);
            return last.IsValid ? last.ToVector3Shifted() : candidateEnd;
        }

        private List<IntVec3> GetCellsAlongBeam(IntVec3 startCell, IntVec3 endCell)
        {
            var cellList = new List<IntVec3>();
            int x0 = startCell.x, z0 = startCell.z;
            int x1 = endCell.x, z1 = endCell.z;
            int dx = Math.Abs(x1 - x0), dz = Math.Abs(z1 - z0);
            int sx = x0 < x1 ? 1 : -1, sz = z0 < z1 ? 1 : -1;
            int err = dx - dz;

            while (true)
            {
                cellList.Add(new IntVec3(x0, startCell.y, z0));
                if (x0 == x1 && z0 == z1) break;
                int err2 = 2 * err;
                if (err2 > -dz) { err -= dz; x0 += sx; }
                if (err2 < dx) { err += dx; z0 += sz; }
            }
            return cellList;
        }

        private void DrawColoredLine(Vector3 start, Vector3 end, Color lineColor, float thickness)
        {
            Color originalColor = GUI.color;
            GUI.color = lineColor;
            GenDraw.DrawLineBetween(start, end, thickness);
            GUI.color = originalColor;
        }

        protected override bool TryCastShot()
        {
            if (this.currentTarget.HasThing && this.currentTarget.Thing.Map != this.caster.Map)
                return false;

            // Calculate beam path with offset
            Vector3 casterPos = this.caster.Position.ToVector3Shifted();
            Vector3 targetPos = this.currentTarget.HasThing
                ? this.currentTarget.Thing.Position.ToVector3Shifted()
                : this.currentTarget.Cell.ToVector3Shifted();

            Vector3 direction = (targetPos - casterPos).Yto0().normalized;
            Vector3 startPoint = casterPos + direction * this.verbProps.beamStartOffset;
            Vector3 endPoint = casterPos + direction * this.verbProps.range;

            // Calculate the actual endpoint considering obstacles
            Vector3 finalPoint = CalculateBeamEndpoint(startPoint, endPoint, targetIsSolid: false);
            IntVec3 startCell = startPoint.ToIntVec3();
            IntVec3 endCell = finalPoint.ToIntVec3();

            List<IntVec3> damageCells = GetCellsAlongBeam(startCell, endCell);

            // Exclude the shooter's cell to prevent self-damage
            if (damageCells.Contains(this.caster.Position))
            {
                damageCells = damageCells.Where(c => c != this.caster.Position).ToList();
            }

            foreach (var cell in damageCells)
            {
                if (!cell.InBounds(this.caster.Map))
                    continue;

                HitCell(cell, this.caster.Position);
            }

            return true;
        }

        private void HitCell(IntVec3 cell, IntVec3 sourceCell)
        {
            if (!cell.InBounds(this.caster.Map))
                return;

            foreach (var thing in cell.GetThingList(this.caster.Map))
            {
                // Prevent damaging the caster
                if (thing == this.caster)
                    continue;

                if (thing is Pawn || thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Building)
                {
                    float angleFlat = (cell - sourceCell).AngleFlat;
                    DamageInfo dinfo = new DamageInfo(this.verbProps.beamDamageDef, this.verbProps.beamTotalDamage, -1f, angleFlat)
                    {
                        // Optionally, add more properties to DamageInfo if needed
                        // Example: dinfo.SetHitPart(someBodyPart);
                    };
                    thing.TakeDamage(dinfo);
                }
            }
        }
    }
}
