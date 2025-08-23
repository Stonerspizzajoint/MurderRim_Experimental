using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;

namespace WorkerDronesMod
{
    public class Comp_AbilityTelekinesisEffect : CompAbilityEffect
    {
        public new CompProperties_AbilityTelekinesisEffect Props => (CompProperties_AbilityTelekinesisEffect)props;

        private List<Thing> heldThings = new List<Thing>();
        public IReadOnlyList<Thing> HeldThingsForDraw => heldThings;
        public bool IsHoldingThing => heldThings.Count > 0;

        private List<Vector3> heldThingStartPositions = new List<Vector3>();
        private int heldThingDriftTicks;
        private int orbitTicks;
        private Material spinningIconMat;
        private const int DriftDurationTicks = 60; // 1 second at 60 ticks/sec
        private const int OrbitTransitionTicks = 30; // e.g., 0.5 seconds

        public Comp_AbilityTelekinesisEffect()
        {
            if (!string.IsNullOrEmpty(Props?.SpinningIconPath))
            {
                var tex = ContentFinder<Texture2D>.Get(Props.SpinningIconPath, true);
                Shader shader = Shader.Find(Props.SpinningIconShader) ?? ShaderDatabase.TransparentPostLight;
                if (tex != null && shader != null)
                    spinningIconMat = MaterialPool.MatFrom(Props.SpinningIconPath, shader);
            }
        }


        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Log.Message($"[Telekinesis] Apply called. heldThings={heldThings.Count}, target={target}, dest={dest}");
            Map map = parent.pawn.Map;

            // Pickup phase: add eligible item to heldThings
            if (!IsHoldingThing)
            {
                if (TelekinesisUtil.IsTelekinesisEligible(target.Thing))
                {
                    var pickedUp = TelekinesisUtil.PickUpThing(parent.pawn, target.Thing);
                    if (pickedUp != null)
                    {
                        heldThings.Add(pickedUp);
                        heldThingStartPositions.Add(target.Thing.DrawPos);
                        heldThingDriftTicks = 0;
                        orbitTicks = 0;
                    }
                }
            }
            // Throw phase: throw the first held item at the destination
            else
            {
                if (heldThings.Count > 0)
                {
                    Thing toThrow = heldThings[0];
                    // If destination is a pawn, use its position
                    if (dest.Thing is Pawn pawn)
                    {
                        TelekinesisUtil.ThrowThing(parent.pawn, toThrow, pawn.Position);
                        RemoveHeldThingAt(0);
                        return;
                    }
                    // If destination is a valid cell
                    IntVec3 newPos = dest.IsValid ? dest.Cell : target.Cell;
                    if (newPos.InBounds(map) && newPos.Walkable(map))
                    {
                        TelekinesisUtil.ThrowThing(parent.pawn, toThrow, newPos);
                        RemoveHeldThingAt(0);
                    }
                    else
                    {
                        Log.Message("[Telekinesis] Destination invalid.");
                    }
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (IsHoldingThing)
            {
                // Only drift if we have start positions for all held things
                if (heldThingStartPositions.Count == heldThings.Count && heldThingDriftTicks < DriftDurationTicks)
                {
                    heldThingDriftTicks++;
                    if (heldThingDriftTicks >= DriftDurationTicks)
                    {
                        heldThingStartPositions.Clear(); // Done drifting
                        orbitTicks = 0; // Start orbit transition
                    }
                }
                else
                {
                    if (orbitTicks < OrbitTransitionTicks)
                        orbitTicks++;
                }
            }
            else
            {
                heldThingDriftTicks = 0;
                orbitTicks = 0;
                heldThingStartPositions.Clear();
            }
        }

        /// <summary>
        /// Safely removes held thing and its start position at the given index.
        /// </summary>
        private void RemoveHeldThingAt(int index)
        {
            if (index >= 0 && index < heldThings.Count)
                heldThings.RemoveAt(index);
            if (index >= 0 && index < heldThingStartPositions.Count)
                heldThingStartPositions.RemoveAt(index);
        }

        /// <summary>
        /// Draws all held things and the spinning icon.
        /// </summary>
        public void DrawHeldThings()
        {
            if (spinningIconMat == null && !string.IsNullOrEmpty(Props.SpinningIconPath))
            {
                Shader shader = Shader.Find(Props.SpinningIconShader) ?? ShaderDatabase.TransparentPostLight;
                spinningIconMat = MaterialPool.MatFrom(Props.SpinningIconPath, shader);
            }

            float spinAngle = (Find.TickManager.TicksGame * 4f) % 360f;
            Color iconColor = Color.white;
            if (Props.SpinIconSkinColor && parent.pawn?.story?.SkinColor != null)
                iconColor = parent.pawn.story.SkinColor;

            TelekinesisUtil.DrawHeldThings(
                heldThings,
                parent.pawn,
                heldThingStartPositions,
                heldThingDriftTicks,
                DriftDurationTicks,
                orbitTicks,
                OrbitTransitionTicks,
                spinningIconMat,
                spinAngle,
                Props.SpinIconSize,
                iconColor,
                Props.DynamicSizeOffset
            );
        }
    }
}

