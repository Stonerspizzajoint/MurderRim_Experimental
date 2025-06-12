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
        // ◀◀ ADDED: remember which cells we forced into avoid-mode
        private List<IntVec3> lastAvoidCells = new List<IntVec3>();

        protected override int ShotsPerBurst
        {
            get { return this.verbProps.burstShotCount; }
        }

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
                return this.currentTarget.HasThing
                    ? targetPoint
                    : casterPos + direction * this.verbProps.range;
            }
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (!target.IsValid)
                return;

            // Draw the range ring.
            IntVec3 casterCell = this.caster.Position;
            GenDraw.DrawRadiusRing(casterCell, this.verbProps.range);

            // For a visual preview we calculate a temporary beam path.
            List<Vector3> previewPath = new List<Vector3>();
            // We don’t need the full cell set for preview; use a temporary hash.
            HashSet<IntVec3> dummyPathCells = new HashSet<IntVec3>();

            bool targetIsSolid = target.HasThing &&
                                 target.Thing.def.category == ThingCategory.Building &&
                                 target.Thing.def.Fillage != FillCategory.None;

            // Calculate the beam path using the current target center.
            // (locked = true so that the preview is consistent with the in‐flight beam)
            CalculateBeamPath(
                target.CenterVector3,
                previewPath,
                dummyPathCells,
                locked: true,
                targetIsSolid: targetIsSolid
            );

            // Draw the beam by connecting each computed point.
            // Example in DrawHighlight
            if (previewPath.Count >= 2)
            {
                Color lineColor = this.verbProps.highlightColor ?? Color.white;
                for (int i = 0; i < previewPath.Count - 1; i++)
                {
                    DrawColoredLine(previewPath[i], previewPath[i + 1], lineColor, 1.5f);
                }
            }
            else
            {
                // Fallback line from caster to target.
                Color lineColor = this.verbProps.highlightColor ?? Color.white;
                Vector3 casterPos = casterCell.ToVector3Shifted();
                Vector3 targetPos = target.HasThing ? target.CenterVector3 : target.Cell.ToVector3Shifted();
                DrawColoredLine(casterPos, targetPos, lineColor, 1.5f);
            }
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

        // In CalculateBeamEndpoint, if the target is solid simply ignore collisions.
        private Vector3 CalculateBeamEndpoint(Vector3 startPoint, Vector3 candidateEnd, bool targetIsSolid)
        {
            // If targeting a solid building, ignore any obstacles and return the candidate position.
            if (targetIsSolid)
            {
                return candidateEnd;
            }

            IntVec3 startCell = startPoint.ToIntVec3();
            IntVec3 targetCell = candidateEnd.ToIntVec3();
            Map map = caster.Map;
            var validator = GetBeamValidator();

            // For non‐solid targets, check along the edges first.
            if (GenSight.LineOfSightToEdges(startCell, targetCell, map, true, validator))
                return targetCell.ToVector3Shifted();

            var last = GenSight.LastPointOnLineOfSight(startCell, targetCell, validator, true);
            return last.IsValid ? last.ToVector3Shifted() : candidateEnd;
        }



        // “IsCover” already returns true for a Thing that this pawn is using as cover:
        private bool IsCover(Thing t)
        {
            if (t == currentTarget.Thing) return false;
            return caster.Map.coverGrid[t.Position] == t;
        }

        private Func<IntVec3, bool> GetBeamValidator()
        {
            Map map = caster.Map;
            return c =>
            {
                // 1) If it’s your own starting cell, always allow
                if (c == caster.Position) return true;

                // 2) If there’s a cover‐thing here that your pawn is actively using, ignore it:
                var things = c.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (IsCover(things[i]))
                        return true;
                }

                // 3) Otherwise fall back to normal “can see over” test
                return c.CanBeSeenOverFast(map);
            };
        }

        private void CalculateBeamPath(
            Vector3 targetCenter,
            List<Vector3> pathList,
            HashSet<IntVec3> pathCellsList,
            bool locked,
            bool targetIsSolid)
        {
            pathList.Clear();
            pathCellsList.Clear();

            // 1) Determine effective shooting origin using ShootLeanUtility (peeking)
            IntVec3 shooterLoc = this.caster.Position;
            Map map = caster.Map;
            var leanSources = new List<IntVec3>();
            ShootLeanUtility.LeanShootingSourcesFromTo(shooterLoc,
                targetCenter.ToIntVec3(), map, leanSources);
            // Pick the first valid peek position (fallback to center)
            // direction from pawn to target
            Vector3 toTarget = (targetCenter.ToIntVec3() - shooterLoc).ToVector3().normalized;
            // pick the lean-source whose vector is best aligned with toTarget
            IntVec3 originCell = shooterLoc;
            float bestAngle = float.MaxValue;
            foreach (var cell in leanSources)
            {
                if (!cell.InBounds(map)) continue;

                Vector3 peekDirection = (cell - shooterLoc).ToVector3().normalized;
                Vector3 toTargetVec = (targetCenter - shooterLoc.ToVector3Shifted()).normalized;

                // Use angle rather than dot for better control
                float angle = Vector3.Angle(peekDirection, toTargetVec);
                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    originCell = cell;
                }
            }
            Vector3 casterPos = originCell.ToVector3Shifted();


            // 2) Calculate direction & beam start offset
            Vector3 direction = (targetCenter - casterPos).Yto0().normalized;
            Vector3 startPoint = casterPos + direction * this.verbProps.beamStartOffset;

            // 3) Determine candidate end (solid targets vs full range)
            Vector3 candidateEnd = targetIsSolid
                ? targetCenter
                : casterPos + direction * this.verbProps.range;

            // 4) Run your collision logic
            Vector3 worldFinal = CalculateBeamEndpoint(startPoint, candidateEnd, targetIsSolid);

            // 5) Build the cell list from start→target→end
            IntVec3 startCell = startPoint.ToIntVec3();
            IntVec3 endCell = worldFinal.ToIntVec3();
            IntVec3 targCell = targetCenter.ToIntVec3();

            List<IntVec3> beamCells;
            if (locked && !targetIsSolid)
            {
                var leg1 = GetCellsAlongBeam(startCell, targCell);
                var leg2 = GetCellsAlongBeam(targCell, endCell);
                beamCells = leg1.Concat(leg2.Skip(1)).ToList();
            }
            else
            {
                beamCells = GetCellsAlongBeam(startCell, endCell);
            }

            // 6) Populate path & pathCells
            foreach (var cell in beamCells)
            {
                pathCellsList.Add(cell);
                pathList.Add(cell.ToVector3Shifted());
            }
        }



        // In TryCastShot, use an overridden collision endpoint if targeting a solid.
        protected override bool TryCastShot()
        {
            if (this.currentTarget.HasThing && this.currentTarget.Thing.Map != this.caster.Map)
                return false;

            // 1) Verify line of sight from the caster to the target.
            ShootLine shootLine;
            bool hasLOS = base.TryFindShootLineFromTo(
                this.caster.Position,
                this.currentTarget,
                out shootLine,
                false
            );
            if (this.verbProps.stopBurstWithoutLos && !hasLOS) return false;

            // 2) Handle equipment components.
            if (base.EquipmentSource != null)
            {
                base.EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
                base.EquipmentSource.GetComp<CompApparelReloadable>()?.UsedOnce();
            }

            this.lastShotTick = Find.TickManager.TicksGame;
            this.ticksToNextPathStep = this.verbProps.ticksBetweenBurstShots;

            // 3) Calculate beam path with collision detection.
            // (The 'targetIsSolid' variable is computed the same way as in WarmupComplete.)
            bool targetIsSolid = (this.currentTarget.HasThing &&
                                  this.currentTarget.Thing.def.category == ThingCategory.Building &&
                                  this.currentTarget.Thing.def.Fillage != FillCategory.None);

            CalculateBeamPath(
                this.currentTarget.CenterVector3,
                this.path,
                this.pathCells,
                locked: true,
                targetIsSolid: false // Let collision detection handle this for non‐solid targets.
            );

            // 4) Find actual collision endpoint.
            IntVec3 casterCell = this.caster.Position;
            IntVec3 intendedEndCell = this.path.Last().ToIntVec3();
            IntVec3 validatedEndCell;

            if (targetIsSolid)
            {
                // For solid targets, override any collision checks and directly use the target's center.
                validatedEndCell = this.currentTarget.CenterVector3.ToIntVec3();
            }
            else
            {
                validatedEndCell = GenSight.LastPointOnLineOfSight(
                    casterCell,
                    intendedEndCell,
                    (IntVec3 c) => c.InBounds(this.caster.Map) && c.CanBeSeenOverFast(this.caster.Map),
                    true
                );
            }

            // 5) Build damage path to the collision point.
            List<IntVec3> damageCells = GetCellsAlongBeam(casterCell, validatedEndCell)
                .Skip(1) // Skip caster's own cell.
                .ToList();

            // 6) Apply damage along the damage path.
            foreach (var cell in damageCells)
            {
                if (!cell.InBounds(this.caster.Map))
                    break;
                HitCell(cell, shootLine.Source, CalculateAccuracyFactor());
            }

            // 7) Handle neighbor cell hits.
            if (this.verbProps.beamHitsNeighborCells)
            {
                var mainHit = validatedEndCell.IsValid ? validatedEndCell : this.currentTarget.Cell;
                this.hitCells.Add(mainHit);

                foreach (var adj in GetBeamHitNeighbourCells(shootLine.Source, mainHit))
                {
                    if (this.hitCells.Contains(adj)) continue;

                    float factor = this.pathCells.Contains(adj) ? 1f : 0.5f;
                    HitCell(adj, shootLine.Source, factor * CalculateAccuracyFactor());
                    this.hitCells.Add(adj);
                }
            }

            // 8) Update final endpoint.
            this.finalEndpoint = validatedEndCell.IsValid
                ? validatedEndCell.ToVector3Shifted()
                : intendedEndCell.ToVector3Shifted();

            return true;
        }


        private float CalculateAccuracyFactor()
        {
            if (this.caster is Pawn pawn)
            {
                float factor = pawn.skills.GetSkill(SkillDefOf.Shooting).Level / 20f;
                return Mathf.Clamp(factor, 0.3f, 1f);
            }
            return Rand.Range(0.5f, 1.0f);
        }

        public override void BurstingTick()
        {
            base.BurstingTick();

            // Decrement burst counter.
            this.ticksToNextPathStep--;

            Vector3 casterPos = this.caster.Position.ToVector3Shifted();

            // For the purposes of visual display, if the path is computed we use
            // the first and last vertices of our beam path.
            Vector3 startVisual = this.path != null && this.path.Count > 0
                ? this.path.First()
                : casterPos;
            Vector3 endVisual = this.path != null && this.path.Count > 0
                ? this.path.Last()
                : (casterPos + (this.currentTarget.CenterVector3 - casterPos).Yto0().normalized * this.verbProps.range);

            // Option A: Update the mote endpoints as a fallback.
            if (this.mote != null)
            {
                this.mote.UpdateTargets(
                    new TargetInfo(this.caster.Position, this.caster.Map, false),
                    new TargetInfo(endVisual.ToIntVec3(), this.caster.Map, false),
                    startVisual - casterPos,
                    Vector3.zero
                );
                this.mote.Maintain();
            }

            // Option B: Draw the beam line so that the visual exactly follows the path.
            // (This runs in addition to the mote update so you see a drawn polyline.)
            if (this.path != null && this.path.Count >= 2)
            {
                Color lineColor = this.verbProps.highlightColor ?? Color.white;
                for (int i = 0; i < this.path.Count - 1; i++)
                {
                    DrawColoredLine(this.path[i], this.path[i + 1], lineColor, 1.5f);
                }
            }

            // Ground flecks.
            if (this.verbProps.beamGroundFleckDef != null && Rand.Chance(this.verbProps.beamFleckChancePerTick))
            {
                FleckMaker.Static(endVisual, this.caster.Map, this.verbProps.beamGroundFleckDef, 1f);
            }

            // End effecter.
            IntVec3 endpointCell = endVisual.ToIntVec3();
            if (this.endEffecter == null && this.verbProps.beamEndEffecterDef != null)
            {
                this.endEffecter = this.verbProps.beamEndEffecterDef.Spawn(endpointCell, this.caster.Map, Vector3.zero, 1f);
            }

            if (this.endEffecter != null)
            {
                this.endEffecter.offset = Vector3.zero;
                this.endEffecter.EffectTick(new TargetInfo(endpointCell, this.caster.Map, false), TargetInfo.Invalid);
                this.endEffecter.ticksLeft--;
            }

            // Beam line flecks.
            if (this.verbProps.beamLineFleckDef != null)
            {
                int flecksThisTick = Rand.RangeInclusive(2, 5);
                for (int i = 0; i < flecksThisTick; i++)
                {
                    float t = Rand.Value;
                    Vector3 fleckPos = Vector3.Lerp(startVisual, endVisual, t);
                    Vector3 perpendicular = Vector3.Cross((endVisual - startVisual).normalized, Vector3.up).normalized;
                    fleckPos += perpendicular * Rand.Range(-0.3f, 0.3f);
                    FleckMaker.Static(fleckPos, this.caster.Map, this.verbProps.beamLineFleckDef, 1f);
                }
            }

            // Camera shake.
            ShakeCamera(0.2f);

            // Sustain sound.
            this.sustainer?.Maintain();
        }

        private void ShakeCamera(float baseMagnitude)
        {
            var cam = Find.CameraDriver;
            if (cam?.shaker == null) return;

            float zoomAtten = 1f - cam.ZoomRootSize.Remap(11f, 60f, 0f, 1f);
            IntVec3 camPos = cam.MapPosition;
            float distance = (this.currentTarget.Cell - camPos).LengthHorizontal;
            float distanceAtten = 1f - Mathf.Clamp01(distance / 15f);
            distanceAtten *= zoomAtten;
            float shakeMag = baseMagnitude * Mathf.Lerp(1f, distanceAtten, 0.5f);
            cam.shaker.DoShake(shakeMag);
        }

        private void DrawColoredLine(Vector3 start, Vector3 end, Color lineColor, float thickness)
        {
            Color originalColor = GUI.color;
            GUI.color = lineColor;
            GenDraw.DrawLineBetween(start, end, thickness);
            GUI.color = originalColor;
        }

        public override void WarmupComplete()
        {
            // 1) set up burst
            this.burstShotsLeft = this.ShotsPerBurst;
            this.state = VerbState.Bursting;

            // 2) calc path
            CalculateBeamPath(
                this.currentTarget.CenterVector3,
                this.path, this.pathCells,
                locked: true,
                targetIsSolid: (this.currentTarget.HasThing
                                && this.currentTarget.Thing.def.category == ThingCategory.Building
                                && this.currentTarget.Thing.def.Fillage != FillCategory.None)
            );

            // ◀◀ ADDED: store & mark avoid-cells
            lastAvoidCells = this.pathCells.ToList();
            foreach (var pawn in this.caster.Map.mapPawns.AllPawnsSpawned)
            {
                if (pawn == this.CasterPawn) continue;
                var grid = pawn.GetAvoidGrid(onlyIfLordAllows: false);
                if (grid == null) continue;
                foreach (var cell in lastAvoidCells)
                    grid[cell] = byte.MaxValue;  // max out avoidance weight
            }

            this.hitCells.Clear();
            if (this.verbProps.beamMoteDef != null)
            {
                this.mote = MoteMaker.MakeInteractionOverlay(
                    this.verbProps.beamMoteDef, this.caster,
                    new TargetInfo(this.path[0].ToIntVec3(), this.caster.Map, false)
                );
            }
            base.TryCastNextBurstShot();
            this.ticksToNextPathStep = this.verbProps.ticksBetweenBurstShots;
            this.endEffecter?.Cleanup();
            if (this.verbProps.soundCastBeam != null)
            {
                this.sustainer = this.verbProps.soundCastBeam
                    .TrySpawnSustainer(SoundInfo.InMap(this.caster, MaintenanceType.PerTick));
            }
        }

        private bool TryGetHitCell(IntVec3 source, IntVec3 targetCell, out IntVec3 hitCell)
        {
            IntVec3 lastPoint = GenSight.LastPointOnLineOfSight(source, targetCell,
                (IntVec3 c) => c.InBounds(this.caster.Map) && c.CanBeSeenOverFast(this.caster.Map), true);
            if (this.verbProps.beamCantHitWithinMinRange && lastPoint.DistanceTo(source) < this.verbProps.minRange)
            {
                hitCell = default(IntVec3);
                return false;
            }
            hitCell = (lastPoint.IsValid ? lastPoint : targetCell);
            return lastPoint.IsValid;
        }

        private IntVec3 GetHitCell(IntVec3 source, IntVec3 targetCell)
        {
            IntVec3 result;
            this.TryGetHitCell(source, targetCell, out result);
            return result;
        }

        protected IEnumerable<IntVec3> GetBeamHitNeighbourCells(IntVec3 source, IntVec3 pos)
        {
            if (!this.verbProps.beamHitsNeighborCells)
                yield break;
            for (int i = 0; i < 4; i++)
            {
                IntVec3 adjacent = pos + GenAdj.CardinalDirections[i];
                if (adjacent.InBounds(this.Caster.Map) &&
                    (!this.verbProps.beamHitsNeighborCellsRequiresLOS || GenSight.LineOfSight(source, adjacent, this.caster.Map)))
                    yield return adjacent;
            }
        }

        public override bool TryStartCastOn(
            LocalTargetInfo castTarg,
            LocalTargetInfo destTarg,
            bool surpriseAttack = false,
            bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false,
            bool nonInterruptingSelfCast = false)
        {
            // Get the equipped weapon and its Railgun component.
            var eq = this.EquipmentSource;
            var railgunComp = eq?.GetComp<CompRailgun>();

            // If we have a custom cooldown and we're still cooling down, block.
            if (railgunComp != null &&
                railgunComp.CustomCooldownTicks > 0 &&
                Find.TickManager.TicksGame < nextAvailableTick)
            {
                return false;
            }

            // Queue up our post-cast callback.
            this.castCompleteCallback = () =>
            {
                if (railgunComp == null) return;

                // play the finish-sound if set
                railgunComp.FinishedSound?.PlayOneShot(
                    new TargetInfo(this.caster.Position, this.caster.Map, false)
                );

                // clear avoid-cells
                foreach (var pawn in this.caster.Map.mapPawns.AllPawnsSpawned)
                {
                    var grid = pawn.GetAvoidGrid(onlyIfLordAllows: false);
                    if (grid == null) continue;
                    foreach (var cell in lastAvoidCells)
                        grid[cell] = 0;
                }
                lastAvoidCells.Clear();

                // set the nextAvailableTick: custom if >0, else default
                int cd = (railgunComp.CustomCooldownTicks > 0)
                    ? railgunComp.CustomCooldownTicks
                    : this.verbProps.AdjustedCooldownTicks(this, this.CasterPawn);
                nextAvailableTick = Find.TickManager.TicksGame + cd;
            };

            return base.TryStartCastOn(
                this.verbProps.beamTargetsGround ? castTarg.Cell : castTarg,
                destTarg,
                surpriseAttack, canHitNonTargetPawns,
                preventFriendlyFire, nonInterruptingSelfCast
            );
        }


        private void HitCell(IntVec3 cell, IntVec3 sourceCell, float damageFactor = 1f)
        {
            if (!cell.InBounds(this.caster.Map))
                return;
            this.ApplyDamage(VerbUtility.ThingsToHit(cell, this.caster.Map, (Thing t) => this.CanHit(t))
                .RandomElementWithFallback(null), sourceCell, damageFactor);
            if (this.verbProps.beamSetsGroundOnFire && Rand.Chance(this.verbProps.beamChanceToStartFire))
                FireUtility.TryStartFireIn(cell, this.caster.Map, 1f, this.caster, null);
        }

        // Only apply damage on targets that are not cover.
        private bool CanHit(Thing thing)
        {
            if (!thing.Spawned)
                return false;
            if (IsCover(thing))
                return false;
            return true;
        }

        private void ApplyDamage(Thing thing, IntVec3 sourceCell, float damageFactor = 1f)
        {
            IntVec3 endpointCell = this.InterpolatedPosition.Yto0().ToIntVec3();
            Map map = this.caster.Map;
            if (thing != null && this.verbProps.beamDamageDef != null)
            {
                float angleFlat = (this.currentTarget.Cell - this.caster.Position).AngleFlat;
                BattleLogEntry_RangedImpact log = new BattleLogEntry_RangedImpact(this.caster, thing, this.currentTarget.Thing,
                    base.EquipmentSource.def, null, null);
                DamageInfo dinfo;
                if (this.verbProps.beamTotalDamage > 0f)
                {
                    float num = this.verbProps.beamTotalDamage / (float)this.pathCells.Count;
                    num *= damageFactor;
                    dinfo = new DamageInfo(this.verbProps.beamDamageDef, num, this.verbProps.beamDamageDef.defaultArmorPenetration,
                        angleFlat, this.caster, null, base.EquipmentSource.def,
                        DamageInfo.SourceCategory.ThingOrUnknown, this.currentTarget.Thing, true, true, QualityCategory.Normal, true);
                }
                else
                {
                    float amount = (float)this.verbProps.beamDamageDef.defaultDamage * damageFactor;
                    dinfo = new DamageInfo(this.verbProps.beamDamageDef, amount, this.verbProps.beamDamageDef.defaultArmorPenetration,
                        angleFlat, this.caster, null, base.EquipmentSource.def,
                        DamageInfo.SourceCategory.ThingOrUnknown, this.currentTarget.Thing, true, true, QualityCategory.Normal, true);
                }
                thing.TakeDamage(dinfo).AssociateWithLog(log);
                if (thing.CanEverAttachFire())
                {
                    float chance = this.verbProps.flammabilityAttachFireChanceCurve != null ?
                        this.verbProps.flammabilityAttachFireChanceCurve.Evaluate(thing.GetStatValue(StatDefOf.Flammability, true, -1)) :
                        this.verbProps.beamChanceToAttachFire;
                    if (Rand.Chance(chance))
                    {
                        thing.TryAttachFire(this.verbProps.beamFireSizeRange.RandomInRange, this.caster);
                        return;
                    }
                }
                else if (Rand.Chance(this.verbProps.beamChanceToStartFire))
                {
                    FireUtility.TryStartFireIn(endpointCell, map, this.verbProps.beamFireSizeRange.RandomInRange,
                        this.caster, this.verbProps.flammabilityAttachFireChanceCurve);
                }
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<Vector3>(ref this.path, "path", LookMode.Value, Array.Empty<object>());
            Scribe_Values.Look<int>(ref this.ticksToNextPathStep, "ticksToNextPathStep", 0, false);
            Scribe_Values.Look<Vector3>(ref this.initialTargetPosition, "initialTargetPosition", default(Vector3), false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.path == null)
                this.path = new List<Vector3>();
        }

        // Private fields used for beam calculation and visual effects.
        private List<Vector3> path = new List<Vector3>();
        private List<Vector3> tmpPath = new List<Vector3>();
        private int ticksToNextPathStep;
        private int nextAvailableTick = 0;
        private Vector3 initialTargetPosition;
        private MoteDualAttached mote;
        private Effecter endEffecter;
        private Sustainer sustainer;
        private HashSet<IntVec3> pathCells = new HashSet<IntVec3>();
        private HashSet<IntVec3> tmpPathCells = new HashSet<IntVec3>();
        private HashSet<IntVec3> tmpHighlightCells = new HashSet<IntVec3>();
        private HashSet<IntVec3> tmpSecondaryHighlightCells = new HashSet<IntVec3>();
        private HashSet<IntVec3> hitCells = new HashSet<IntVec3>();
        private Vector3 finalEndpoint = Vector3.zero;
        private const int NumSubdivisionsPerUnitLength = 1;
    }
}

