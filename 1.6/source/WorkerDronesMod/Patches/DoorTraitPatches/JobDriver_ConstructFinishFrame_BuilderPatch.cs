using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    // PATCH 1: Capture the builder in JobDriver_ConstructFinishFrame.
    [HarmonyPatch(typeof(JobDriver_ConstructFinishFrame), "MakeNewToils")]
    public static class JobDriver_ConstructFinishFrame_BuilderPatch
    {
        public static void Prefix(JobDriver_ConstructFinishFrame __instance)
        {
            // Access the frame from the job's target.
            Frame frame = __instance.job.targetA.Thing as Frame;
            if (frame != null)
            {
                Pawn builder = __instance.pawn;
                if (!DoorBuilderTracker.BuilderByFrame.ContainsKey(frame))
                {
                    DoorBuilderTracker.BuilderByFrame.Add(frame, builder);
                }
            }
            else
            {
                Log.Warning("[WorkerDronesMod] JobDriver_ConstructFinishFrame: TargetA is not a Frame!");
            }
        }
    }
}
