using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    public class WorkerDronesMod : Mod
    {
        public static WorkerDronesModSettings settings;

        public WorkerDronesMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<WorkerDronesModSettings>();
        }

        public override string SettingsCategory() => "MurderRim –  Assembly Required";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled(
                "Heat Gain in Sunlight",
                ref settings.heatGainInSunlight,
                "If disabled, all solver users do NOT gain heat in sunlight. Heat cannot be lowered while in sunlight however."
            );

            listing.End();
        }
    }
}

