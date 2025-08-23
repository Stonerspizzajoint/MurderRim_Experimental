using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(ThoughtWorker_PsychologicallyNude), "CurrentStateInternal")]
    public static class Patch_PsychologicallyNude_NeverConsider
    {
        static bool Prefix(Pawn p, ref ThoughtState __result)
        {
            // If the pawn is a core heart, treat them as never naked.
            if (p != null && CoreHeartUtils.IsCoreHeart(p))
            {
                __result = ThoughtState.Inactive;
                return false; // Skip the original method so the thought never gets applied.
            }
            return true;
        }
    }
}
