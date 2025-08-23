using HarmonyLib;
using Verse;

namespace WorkerDronesMod.FacialAnimationCompat
{
    [HarmonyPatch(typeof(TickManager), nameof(TickManager.DoSingleTick))]
    public static class Patch_TickManager_DoSingleTick_FacialAnimationBatch
    {
        static void Postfix()
        {
            WorkerDronesMod.FacialAnimationCompat.FacialAnimationBatcher.ProcessQueue();
        }
    }
}

