using HarmonyLib;
using Verse;

namespace WorkerDronesMod.FacialAnimationCompat
{
    [StaticConstructorOnStartup]
    public static class FacialAnimationCompatInit
    {
        static FacialAnimationCompatInit()
        {
            try
            {
                var harmony = new Harmony("WorkerDronesMod.FacialAnimationCompat");
                harmony.PatchAll(); // Automatically patches all types in this assembly with Harmony attributes
                Log.Message("[WorkerDronesMod] Facial Animation compatibility patches applied.");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[WorkerDronesMod] Error while applying Facial Animation patches: {ex}");
            }
        }
    }
}

