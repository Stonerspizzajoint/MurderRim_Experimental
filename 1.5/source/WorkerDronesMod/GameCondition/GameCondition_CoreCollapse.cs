using System;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace WorkerDronesMod
{
    public class GameCondition_CoreCollapse : GameCondition
    {
        private static readonly SkyColorSet CoreCollapseColors = new SkyColorSet(
            new ColorInt(20, 20, 30).ToColor,
            new ColorInt(100, 110, 120).ToColor,
            new Color(0.5f, 0.55f, 0.6f),
            0.8f
        );

        private const float MaxTempOffset = -60f;
        private const float AnimalDensityImpact = 0.9f;
        private const int TicksPerDay = 60000;

        private int ticksPassed;
        private bool warningTriggered;
        private bool collapseTriggered;

        private int collapseStartTick = -1;
        private int warningDelayTicks;

        private bool sentLetter;

        public override void Init()
        {
            base.Init();

            // Random delay on day 2 (between 0–60k ticks after day 1)
            warningDelayTicks = TicksPerDay + Rand.RangeInclusive(0, TicksPerDay);
        }

        public override void GameConditionTick()
        {
            base.GameConditionTick();
            ticksPassed++;

            if (!warningTriggered && ticksPassed >= warningDelayTicks)
            {
                // Shake and warn
                Find.CameraDriver.shaker.DoShake(2f);
                Messages.Message(
                    "A deep rumble shakes the ground... something terrible is happening beneath the crust...",
                    MessageTypeDefOf.ThreatBig
                );
                warningTriggered = true;
                collapseStartTick = ticksPassed + TicksPerDay / 2; // begins half a day later
            }

            if (warningTriggered && !collapseTriggered && ticksPassed >= collapseStartTick)
            {
                Find.CameraDriver.shaker.DoShake(3f);
                if (Find.Maps.Count > 0)
                {
                    var map = Find.Maps[0];
                    SoundDefOf.Thunder_OffMap.PlayOneShot(new TargetInfo(map.Center, map));
                }

                Find.LetterStack.ReceiveLetter(
                    "Core Collapse Initiated",
                    "The planet's core has ruptured beyond repair. Temperatures will now begin to fall to fatal levels, and the skies will grow ever darker.",
                    LetterDefOf.ThreatBig
                );

                collapseTriggered = true;
            }
        }

        public override float TemperatureOffset()
        {
            if (!collapseTriggered)
                return 0f;

            float t = Mathf.Clamp01((ticksPassed - collapseStartTick) / (float)TicksPerDay);
            return Mathf.Lerp(0f, MaxTempOffset, t);
        }

        public override float AnimalDensityFactor(Map map)
        {
            if (!collapseTriggered)
                return 1f;

            float t = Mathf.Clamp01((ticksPassed - collapseStartTick) / (float)TicksPerDay);
            return Mathf.Max(0f, 1f - t * AnimalDensityImpact);
        }

        public override SkyTarget? SkyTarget(Map map)
        {
            return new SkyTarget?(new SkyTarget(0.7f, CoreCollapseColors, 1f, 1f));
        }

        public override float SkyTargetLerpFactor(Map map)
        {
            return collapseTriggered ? Mathf.Clamp01((ticksPassed - collapseStartTick) / (float)TicksPerDay) : 0f;
        }

        public override bool AllowEnjoyableOutsideNow(Map map) => false;

        public override WeatherDef ForcedWeather()
        {
            if (!collapseTriggered)
                return null;

            Map map = Find.AnyPlayerHomeMap;
            if (map == null) return null;

            int tile = map.Tile;
            float seasonalTemp = GenTemperature.GetTemperatureFromSeasonAtTile(tile, 0);
            float effectiveTemp = seasonalTemp + TemperatureOffset();

            if (effectiveTemp <= 0f)
            {
                return WeatherDef.Named("SnowHard");
            }

            return null;
        }
    }
}

