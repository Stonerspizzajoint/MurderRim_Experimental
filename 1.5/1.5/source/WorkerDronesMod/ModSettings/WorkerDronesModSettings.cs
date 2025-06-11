using Verse;

namespace WorkerDronesMod
{
    public class WorkerDronesModSettings : ModSettings
    {
        public static bool OverheatingProtectionEnabled = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref OverheatingProtectionEnabled, "OverheatingProtectionEnabled", true);
        }
    }
}

