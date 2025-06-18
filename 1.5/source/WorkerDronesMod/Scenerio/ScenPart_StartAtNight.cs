using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace WorkerDronesMod
{
    public class ScenPart_StartAtNight : ScenPart
    {
        // Default to 22:00 (10 PM) which is deep night in RimWorld
        public float startHour = 22f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref startHour, "startHour", 22f);
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            base.DoEditInterface(listing);
            Rect rect = listing.GetScenPartRect(this, ScenPart.RowHeight * 2);

            // Time display
            Rect labelRect = rect.TopPartPixels(ScenPart.RowHeight);
            Widgets.Label(labelRect, $"Start time: {startHour:00}:00");

            // Night hour slider (18-6)
            Rect sliderRect = rect.BottomPartPixels(ScenPart.RowHeight);
            startHour = Widgets.HorizontalSlider(
                sliderRect,
                startHour,
                18f,   // 6 PM
                30f,   // 6 AM next day (wrapped)
                true,
                "18:00 (6 PM)",
                "6:00 (6 AM)",
                roundTo: 1f
            );

            // Wrap values above 24
            if (startHour >= 24f)
            {
                startHour -= 24f;
            }
        }

        public override void PostGameStart()
        {
            base.PostGameStart();

            LongEventHandler.QueueLongEvent(() =>
            {
                // Normalize hour to 0-24 range
                float normalizedHour = startHour % 24f;
                if (normalizedHour < 0) normalizedHour += 24f;

                // Convert to fraction of day (0-1)
                float fraction = normalizedHour / 24f;

                // Calculate starting tick for the first day
                int startTicks = Mathf.RoundToInt(fraction * GenDate.TicksPerDay);

                // Set game time (absolute ticks since world creation)
                Find.TickManager.DebugSetTicksGame(startTicks);
            },
            "LoadingMap", false, null);
        }

        public override string Summary(Scenario scen)
        {
            float displayHour = startHour % 24f;
            if (displayHour < 0) displayHour += 24f;

            string period = (displayHour >= 18f || displayHour < 6f) ? "night" : "day";
            return $"Starts at {displayHour:00}:00 ({period} time)";
        }

        // Handle def initialization safely
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
