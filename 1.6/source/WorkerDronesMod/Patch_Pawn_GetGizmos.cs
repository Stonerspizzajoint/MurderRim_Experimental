using HarmonyLib;
using Verse;
using System.Collections.Generic;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class Patch_Pawn_GetGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (var gizmo in __result)
                yield return gizmo;

            // Only show for android pawns in dev mode/god mode
            if (Prefs.DevMode && DebugSettings.godMode && __instance.IsAndroid())
            {
                foreach (var gizmo in BabyAndroidDebugGizmo.GetTestGizmos(__instance))
                    yield return gizmo;
            }
        }
    }
}

