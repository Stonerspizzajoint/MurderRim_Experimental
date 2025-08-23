using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace WorkerDronesMod
{
    public class ScenPart_StartAtNight : ScenPart
    {
        // Default to 20
        public float startHour = 20f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref startHour, "startHour", 22f);
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            base.DoEditInterface(listing);
            Rect rect = listing.GetScenPartRect(this, ScenPart.RowHeight * 2);

            // Wrap startHour to 0-23
            if (startHour >= 24f) startHour -= 24f;
            if (startHour < 0f) startHour += 24f;

            // Display time as RimWorld does (integer hour, 0–23)
            int hourInt = Mathf.FloorToInt(startHour) % 24;
            string timeString = $"{hourInt:00}:00";
            string period = (hourInt >= 18 || hourInt < 6) ? "night" : "day";

            // Time display
            Rect labelRect = rect.TopPartPixels(ScenPart.RowHeight);
            Widgets.Label(labelRect, $"Start time: {timeString} ({period})");

            // Hour slider (0–23)
            Rect sliderRect = rect.BottomPartPixels(ScenPart.RowHeight);
            startHour = Widgets.HorizontalSlider(
                sliderRect,
                startHour,
                0f,   // 0:00
                23f,  // 23:00
                true,
                "0:00",
                "23:00",
                roundTo: 1f
            );
        }

        public override void PostGameStart()
        {
            base.PostGameStart();

            LongEventHandler.QueueLongEvent(() =>
            {
                Map map = Find.CurrentMap;
                float longitude = map != null ? Find.WorldGrid.LongLatOf(map.Tile).x : 0f;

                // Calculate tick offset for desired local hour
                int desiredLocalTick = Mathf.RoundToInt(startHour * GenDate.TicksPerHour);
                int timeZoneOffset = (int)GenDate.LocalTicksOffsetFromLongitude(longitude);
                int startTicks = desiredLocalTick - timeZoneOffset;

                // Ensure within first day
                startTicks = (startTicks + GenDate.TicksPerDay) % GenDate.TicksPerDay;

                Find.TickManager.DebugSetTicksGame(startTicks);
            },
            "LoadingMap", false, null);
        }

        public override string Summary(Scenario scen)
        {
            int hourInt = Mathf.FloorToInt(startHour) % 24;
            string timeString = $"{hourInt:00}:00";
            string period = (hourInt >= 18 || hourInt < 6) ? "night" : "day";
            return $"Starts at {timeString} ({period})";
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (def == null)
            {
                yield return "ScenPartDef is null! Ensure XML definition exists.";
            }
        }
    }
}
