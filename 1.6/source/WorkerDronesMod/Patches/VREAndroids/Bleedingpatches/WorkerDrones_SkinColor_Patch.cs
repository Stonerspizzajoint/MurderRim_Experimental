using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn_StoryTracker), "SkinColor", MethodType.Getter)]
    public static class WorkerDrones_SkinColor_Patch
    {
        // Finalizer always runs after all postfixes
        public static void Finalizer(Pawn_StoryTracker __instance, Pawn ___pawn, ref Color __result)
        {
            if (MD_Utils.IsDroneBody(___pawn))
            {
                __result = __instance.skinColorOverride ?? __instance.SkinColorBase;
            }
        }
    }
}
