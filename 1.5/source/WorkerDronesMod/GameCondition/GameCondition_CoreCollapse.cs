using System;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace WorkerDronesMod
{
    public class GameCondition_CoreCollapse : GameCondition
    {
        // Duration to reach full effect (ticks)
        public override int TransitionTicks => 10000;

        // Dramatic dark sky tint
        private static readonly SkyColorSet CoreCollapseColors = new SkyColorSet(
            new ColorInt(20, 20, 30).ToColor,   // ambient
            new ColorInt(100, 110, 120).ToColor, // sun
            new Color(0.5f, 0.55f, 0.6f),        // shadow
            0.8f                                 // glow
        );

        // Maximum negative temperature offset
        private const float MaxTempOffset = -60f;

        // Animal density impact: near total wipeout
        private const float AnimalDensityImpact = 0.9f;

        // Track game ticks since start
        private int ticksPassed;

        // One-time effect trigger
        private bool effectTriggered;

        private bool sentLetter;

        public override void Init()
        {
            base.Init();
            // Send starting letter
            if (!sentLetter)
            {
                Find.LetterStack.ReceiveLetter(
                    "Core Collapse",
                    "The planet's core has ruptured beyond repair, plunging all surface life into endless ice.",
                    LetterDefOf.ThreatBig
                );
                sentLetter = true;
            }
        }

        public override void GameConditionTick()
        {
            base.GameConditionTick();
            ticksPassed++;

            // Trigger screen shake and sound once at full collapse
            if (!effectTriggered && ticksPassed >= TransitionTicks)
            {
                Find.CameraDriver.shaker.DoShake(3f);
                // Play collapse sound on map center
                if (Find.Maps.Count > 0)
                {
                    var map = Find.Maps[0];
                    var sound = SoundDef.Named("CoreCollapse_SoundDef");
                    sound.PlayOneShot(new TargetInfo(map.Center, map, false));
                }
                effectTriggered = true;
            }
        }

        public override float SkyTargetLerpFactor(Map map)
        {
            return GameConditionUtility.LerpInOutValue(this, TransitionTicks, 0.5f);
        }

        public override SkyTarget? SkyTarget(Map map)
        {
            return new SkyTarget?(new SkyTarget(0.7f, CoreCollapseColors, 1f, 1f));
        }

        public override float TemperatureOffset()
        {
            return GameConditionUtility.LerpInOutValue(this, TransitionTicks, MaxTempOffset);
        }

        public override float AnimalDensityFactor(Map map)
        {
            return Mathf.Max(0f, 1f - GameConditionUtility.LerpInOutValue(this, TransitionTicks, AnimalDensityImpact));
        }

        public override bool AllowEnjoyableOutsideNow(Map map)
        {
            return false;
        }
    }
}

