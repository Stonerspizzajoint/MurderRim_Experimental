using Verse;

namespace WorkerDronesMod
{
    public class WorkerDronesModSettings : ModSettings
    {
        public bool heatGainInSunlight = true;
        public bool overheatingProtectionEnabled = true; // <-- Add this line

        public override void ExposeData()
        {
            Scribe_Values.Look(ref heatGainInSunlight, "enableHeatGainInSunlight", true);
            Scribe_Values.Look(ref overheatingProtectionEnabled, "overheatingProtectionEnabled", true); // <-- Add this line
        }
    }
}


