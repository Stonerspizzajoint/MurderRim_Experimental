using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(ITab_Pawn_Feeding), "IsVisible", MethodType.Getter)]
    public static class Patch_ITab_Pawn_Feeding_IsVisible_BabyAndroid
    {
        // Cache PropertyInfo for performance
        private static readonly PropertyInfo SelPawnProperty = typeof(ITab_Pawn_Feeding).GetProperty("SelPawn", BindingFlags.Instance | BindingFlags.NonPublic);

        static bool Prefix(ITab_Pawn_Feeding __instance, ref bool __result)
        {
            var selPawn = SelPawnProperty?.GetValue(__instance) as Pawn;
            if (selPawn != null && BabyAndroidUtil.IsBabyAndroid(selPawn))
            {
                __result = false;
                return false; // Hide the tab for android babies
            }
            return true; // Use vanilla logic otherwise
        }
    }
}

