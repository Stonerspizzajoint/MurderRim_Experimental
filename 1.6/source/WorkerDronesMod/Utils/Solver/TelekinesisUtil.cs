using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using System.Linq;
using System.Collections.Generic;
using System;

namespace WorkerDronesMod
{
    public static class TelekinesisUtil
    {
        // Pick up a thing for telekinetic holding
        public static Thing PickUpThing(Pawn caster, Thing target)
        {
            if (target == null || !target.Spawned) return null;
            target.DeSpawn();
            return target;
        }

        // Calculate floating position beside the caster
        public static Vector3 FloatingDrawPos(Pawn caster, int index = 0)
        {
            float floatHeight = 0.4f + 0.3f * Mathf.Sin(Find.TickManager.TicksGame / 60f + index * 30f);
            Vector3 offset = new Vector3(1.2f + index * 0.6f, 0f, 0f); // Support multiple items
            Vector3 drawPos = caster.DrawPos + offset;
            drawPos.y += floatHeight;
            return drawPos;
        }

        // Draw the held thing at the calculated position
        public static void DrawHeldThings(
    IReadOnlyList<Thing> heldThings,
    Pawn caster,
    List<Vector3> startPositions,
    int driftTicks,
    int driftDurationTicks,
    int orbitTicks,
    int orbitTransitionTicks,
    Material spinningIconMat = null,
    float spinningIconAngle = 0f,
    float spinIconSize = 1f,
    Color? spinIconColor = null,
    bool dynamicSizeOffset = false)
        {
            if (heldThings == null || caster == null) return;
            Color iconColor = spinIconColor ?? Color.white;

            // For multiple items, draw the icon at MoteLow centered on the pawn (only once)
            if (heldThings.Count > 1 && spinningIconMat != null)
            {
                // Use the first item's size if dynamicSizeOffset is enabled
                float iconSize = spinIconSize;
                if (dynamicSizeOffset && heldThings.Count > 0 && heldThings[0]?.Graphic != null)
                {
                    var drawSize = heldThings[0].Graphic.drawSize;
                    iconSize *= Mathf.Max(drawSize.x, drawSize.y);
                }
                Mesh iconMesh = MeshPool.plane10;
                Vector3 iconPos = caster.DrawPos;
                iconPos.y = AltitudeLayer.MoteLow.AltitudeFor();
                Matrix4x4 iconMatrix = Matrix4x4.TRS(iconPos, Quaternion.AngleAxis(spinningIconAngle, Vector3.up), new Vector3(iconSize, 1f, iconSize));
                spinningIconMat.color = iconColor;
                Graphics.DrawMesh(iconMesh, iconMatrix, spinningIconMat, 0);
            }

            for (int i = 0; i < heldThings.Count; i++)
            {
                Thing heldThing = heldThings[i];
                if (heldThing == null) continue;

                // Calculate icon size for single item
                float iconSize = spinIconSize;
                if (dynamicSizeOffset && heldThing.Graphic != null)
                {
                    var drawSize = heldThing.Graphic.drawSize;
                    iconSize *= Mathf.Max(drawSize.x, drawSize.y);
                }
                Mesh iconMesh = MeshPool.plane10;

                Vector3 drawPos;
                if (startPositions != null && startPositions.Count == heldThings.Count && driftTicks < driftDurationTicks)
                {
                    drawPos = DriftDrawPos(startPositions[i], caster, driftTicks, driftDurationTicks, i);
                    drawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
                }
                else if (heldThings.Count == 1)
                {
                    drawPos = BobDrawPos(caster);
                    drawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.3f * Mathf.Sin(Find.TickManager.TicksGame / 60f);
                }
                else
                {
                    drawPos = OrbitDrawPos(caster, orbitTicks, orbitTransitionTicks, i);
                    drawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
                }

                // For a single item, draw the icon just below the item
                if (heldThings.Count == 1 && spinningIconMat != null)
                {
                    Vector3 iconPos = drawPos;
                    iconPos.y -= 0.05f;
                    Matrix4x4 iconMatrix = Matrix4x4.TRS(iconPos, Quaternion.AngleAxis(spinningIconAngle, Vector3.up), new Vector3(iconSize, 1f, iconSize));
                    spinningIconMat.color = iconColor;
                    Graphics.DrawMesh(iconMesh, iconMatrix, spinningIconMat, 0);
                }

                // Only render non-pawn, non-corpse things
                if (!(heldThing is Pawn) && !(heldThing is Corpse))
                {
                    var mesh = heldThing.Graphic.MeshAt(Rot4.North);
                    var mat = heldThing.Graphic.MatAt(Rot4.North, heldThing);
                    Graphics.DrawMesh(mesh, drawPos, Quaternion.identity, mat, 0);
                }
            }
        }


        // Throw the held thing using a projectile
        public static void ThrowThing(Pawn caster, Thing heldThing, IntVec3 destCell)
        {
            Log.Message($"[Telekinesis] ThrowThing called for {heldThing} to {destCell}");
            if (heldThing == null || caster == null || caster.Map == null) return;

            if (heldThing.Spawned)
            {
                // Drop the item at the caster's position
                Thing droppedThing;
                GenPlace.TryPlaceThing(heldThing, caster.Position, caster.Map, ThingPlaceMode.Direct, out droppedThing);
                heldThing = droppedThing ?? heldThing;
            }

            // Spawn the projectile
            var projectileDef = MD_DefOf.MD_TelekineticProjectile;
            var projectile = (Projectile_TelekineticThrow)GenSpawn.Spawn(projectileDef, caster.Position, caster.Map);

            // Assign the thrown thing
            projectile.thrownThing = heldThing;

            // Launch the projectile
            projectile.Launch(
                caster,
                caster.Position.ToVector3Shifted(),
                new LocalTargetInfo(destCell),
                new LocalTargetInfo(destCell),
                ProjectileHitFlags.IntendedTarget
            );
        }

        // Check if a thing is eligible for telekinesis
        public static bool IsTelekinesisEligible(Thing thing)
        {
            // Exclude corpses
            if (thing is Corpse)
                return false;
            return thing != null && thing.Spawned && thing.def.EverHaulable;
        }
        public static Vector3 DriftDrawPos(Vector3 startPos, Pawn caster, int driftTicks, int driftDurationTicks, int index = 0)
        {
            Vector3 endPos = FloatingDrawPos(caster, index);
            float t = Mathf.Clamp01(driftTicks / (float)driftDurationTicks);
            Vector3 driftPos = Vector3.Lerp(startPos, endPos, t);
            return driftPos;
        }
        public static Vector3 OrbitDrawPos(Pawn caster, int orbitTicks, int orbitTransitionTicks, int index = 0)
        {
            // Slow bobbing: lower frequency for up/down
            float bobSpeed = 120f; // Higher value = slower bob
            float floatHeight = 0.4f + 0.3f * Mathf.Sin(Find.TickManager.TicksGame / bobSpeed + index * 30f);

            // Orbit radius, smoothly interpolated for transition
            float orbitRadius = 1.2f;
            if (orbitTicks < orbitTransitionTicks)
            {
                orbitRadius *= (orbitTicks / (float)orbitTransitionTicks);
            }

            // Full orbit angle in radians
            float orbitAngle = (Find.TickManager.TicksGame % 360) * Mathf.Deg2Rad + index * 0.5f;
            Vector3 offset = new Vector3(Mathf.Cos(orbitAngle) * orbitRadius, 0f, Mathf.Sin(orbitAngle) * orbitRadius);

            Vector3 drawPos = caster.DrawPos + offset;
            drawPos.y += floatHeight;
            return drawPos;
        }
        public static void ApplyTelekineticImpact(Thing thrownThing, IntVec3 cell, Map map)
        {
            if (thrownThing == null || map == null) return;

            // If the item has CompExplosive, apply enough bomb damage to reduce HP to 9
            var compExplosive = thrownThing.TryGetComp<CompExplosive>();
            if (compExplosive != null)
            {
                int currentHP = thrownThing.HitPoints;
                int maxHP = thrownThing.MaxHitPoints;
                int targetHP = 9;
                int damageToApply = Math.Max(1, currentHP - targetHP);

                var damageInfo = new DamageInfo(DamageDefOf.Bomb, damageToApply, 0, -1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                thrownThing.TakeDamage(damageInfo);
                return; // Don't apply normal impact logic
            }

            // Try to get melee damage from the item's verbs
            float meleeDamage = -1f;
            DamageDef meleeDamageDef = DamageDefOf.Blunt;

            if (thrownThing.def?.Verbs != null)
            {
                // Prefer melee verbs with a valid damage amount
                foreach (var verb in thrownThing.def.Verbs)
                {
                    if (verb.IsMeleeAttack && verb.meleeDamageDef != null && verb.meleeDamageBaseAmount > 0)
                    {
                        meleeDamage = verb.meleeDamageBaseAmount;
                        meleeDamageDef = verb.meleeDamageDef;
                        break;
                    }
                }
            }

            foreach (var thing in cell.GetThingList(map).ToList())
            {
                if (thing is Pawn targetPawn)
                {
                    float mass = thrownThing.GetStatValue(StatDefOf.Mass, true);
                    float damage;
                    DamageDef damageDef;

                    if (meleeDamage > 0)
                    {
                        damage = meleeDamage;
                        damageDef = meleeDamageDef;
                    }
                    else
                    {
                        damage = Mathf.Max(5f, mass * 2f);
                        damageDef = DamageDefOf.Blunt;
                    }

                    var damageInfo = new DamageInfo(damageDef, damage, 0, -1, thrownThing, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                    targetPawn.TakeDamage(damageInfo);
                }
            }
        }
        public static Vector3 BobDrawPos(Pawn caster)
        {
            // Bobbing up and down beside the caster
            float floatHeight = 0.4f + 0.3f * Mathf.Sin(Find.TickManager.TicksGame / 60f);
            Vector3 offset = new Vector3(1.2f, 0f, 0f); // Beside the caster
            Vector3 drawPos = caster.DrawPos + offset;
            drawPos.y += floatHeight;
            return drawPos;
        }
    }
}

