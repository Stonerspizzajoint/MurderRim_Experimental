using System;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;
using LudeonTK;

namespace WorkerDronesMod
{
    public class GameCondition_CoreCollapse : GameCondition
    {
        // Customize in XML or code before Init() runs:
        public float WarningShakeIntensity = 0.5f;
        public float CollapseShakeIntensity = 3f;
        public int CollapseShakeDurationTicks = 20;
        public int CollapseShakeIntervalTicks = 5;

        public int WarningTestDelayTicks = 500;  // DevMode only
        public int CollapseTestOffsetTicks = 1000; // DevMode only

        public const int TicksPerDay = 60000;
        public float MaxTempOffset = -60f;
        public float AnimalDensityImpact = 0.9f;

        private static readonly SkyColorSet CoreCollapseColors = new SkyColorSet(
            new ColorInt(20, 20, 30).ToColor,
            new ColorInt(100, 110, 120).ToColor,
            new Color(0.5f, 0.55f, 0.6f),
            0.8f
        );

        public int ticksPassed;
        public int warningDelayTicks;
        public int collapseStartTick = -1;
        public int collapseTriggeredTick = -1;
        public bool warningTriggered;
        public bool collapseTriggered;

        public override void Init()
        {
            base.Init();

            if (Prefs.DevMode)
            {
                warningDelayTicks = WarningTestDelayTicks;
                collapseStartTick = warningDelayTicks + CollapseTestOffsetTicks;
            }
            else
            {
                warningDelayTicks = TicksPerDay + Rand.RangeInclusive(0, TicksPerDay);
            }
        }

        public override void GameConditionTick()
        {
            base.GameConditionTick();
            ticksPassed++;

            // Warning moment: one shake + message
            if (!warningTriggered && ticksPassed >= warningDelayTicks)
            {
                warningTriggered = true;
                Find.CameraDriver.shaker.DoShake(WarningShakeIntensity);
                Messages.Message(
                    "A deep rumble shakes the ground... something terrible is happening beneath the crust...",
                    MessageTypeDefOf.ThreatBig
                );
                if (collapseStartTick < 0)
                    collapseStartTick = ticksPassed + (Prefs.DevMode
                        ? CollapseTestOffsetTicks
                        : TicksPerDay / 2);
            }

            // Collapse moment: one shake + thunder + letter
            if (warningTriggered && !collapseTriggered && ticksPassed >= collapseStartTick)
            {
                collapseTriggered = true;
                collapseTriggeredTick = ticksPassed;
                Find.CameraDriver.shaker.DoShake(CollapseShakeIntensity);

                if (Find.Maps.Count > 0)
                {
                    var map = Find.Maps[0];
                    SoundDefOf.Thunder_OffMap.PlayOneShot(
                        new TargetInfo(map.Center, map));
                }

                Find.LetterStack.ReceiveLetter(
                    "Core Collapse Initiated",
                    "The planet's core has ruptured beyond repair. Temperatures will now begin to fall to fatal levels, and the skies will grow ever darker.",
                    LetterDefOf.ThreatBig
                );
            }

            // (Optional) If you want the collapse shakes to last a few ticks:
            if (collapseTriggered)
            {
                int dt = ticksPassed - collapseTriggeredTick;
                if (dt > 0 && dt <= CollapseShakeDurationTicks && dt % CollapseShakeIntervalTicks == 0)
                {
                    Find.CameraDriver.shaker.DoShake(CollapseShakeIntensity);
                    if (Find.Maps.Count > 0)
                        SoundDefOf.Thunder_OffMap.PlayOneShot(
                            new TargetInfo(Find.Maps[0].Center, Find.Maps[0]));
                }
            }
        }

        public override float TemperatureOffset()
        {
            if (!collapseTriggered) return 0f;
            float t = Mathf.Clamp01((ticksPassed - collapseStartTick) / (float)TicksPerDay);
            return Mathf.Lerp(0f, MaxTempOffset, t);
        }

        public override float AnimalDensityFactor(Map map)
        {
            if (!collapseTriggered) return 1f;
            float t = Mathf.Clamp01((ticksPassed - collapseStartTick) / (float)TicksPerDay);
            return Mathf.Max(0f, 1f - t * AnimalDensityImpact);
        }

        public override SkyTarget? SkyTarget(Map map)
        {
            // always use our dark palette
            return new SkyTarget?(new SkyTarget(0.7f, CoreCollapseColors, 1f, 1f));
        }

        public override float SkyTargetLerpFactor(Map map)
        {
            if (!collapseTriggered) return 0f;

            // base progression from 0→1 over a day
            float prog = Mathf.Clamp01((ticksPassed - collapseStartTick) / (float)TicksPerDay);

            // scale by night factor so days still brighten:
            // sunGlow is ~0 at midnight, ~1 at noon
            float sunGlow = GenCelestial.CurCelestialSunGlow(map);
            float nightFactor = 1f - sunGlow;

            return prog * nightFactor;
        }

        public override bool AllowEnjoyableOutsideNow(Map map) => false;

        public override WeatherDef ForcedWeather()
        {
            if (!collapseTriggered) return null;
            var map = Find.AnyPlayerHomeMap;
            if (map == null) return null;

            float seasonalTemp = GenTemperature.GetTemperatureFromSeasonAtTile(map.Tile, 0);
            float effectiveTemp = seasonalTemp + TemperatureOffset();
            return effectiveTemp <= 0f
                ? WeatherDef.Named("SnowHard")
                : null;
        }
    }

    [StaticConstructorOnStartup]
    public static class CoreCollapseDebug
    {
        [DebugAction("Game Conditions", "Trigger Core Collapse Warning",
            actionType = DebugActionType.Action)]
        private static void Debug_TriggerWarning()
        {
            var cond = new GameCondition_CoreCollapse();
            cond.Init();
            Find.World.gameConditionManager.RegisterCondition(cond);
            if (Find.CurrentMap != null)
                Find.CurrentMap.gameConditionManager.RegisterCondition(cond);
            // fast‑forward to warning
            cond.ticksPassed = cond.warningDelayTicks;
            cond.GameConditionTick();
        }

        [DebugAction("Game Conditions", "Trigger Full Core Collapse",
            actionType = DebugActionType.Action)]
        private static void Debug_TriggerFullCollapse()
        {
            var cond = new GameCondition_CoreCollapse();
            cond.Init();
            Find.World.gameConditionManager.RegisterCondition(cond);
            if (Find.CurrentMap != null)
                Find.CurrentMap.gameConditionManager.RegisterCondition(cond);
            // fast‑forward to collapse
            cond.ticksPassed = cond.warningDelayTicks + (Prefs.DevMode
                ? cond.CollapseTestOffsetTicks
                : GameCondition_CoreCollapse.TicksPerDay / 2);
            cond.GameConditionTick();
        }
    }
}




