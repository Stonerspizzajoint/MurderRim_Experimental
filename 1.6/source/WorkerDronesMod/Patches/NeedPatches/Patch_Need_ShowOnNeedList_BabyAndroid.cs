using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Need), "ShowOnNeedList", MethodType.Getter)]
    public static class Patch_Need_ShowOnNeedList_BabyAndroid
    {
        // Cache the FieldInfo for performance
        private static readonly FieldInfo PawnField = typeof(Need).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);

        static bool Prefix(Need __instance, ref bool __result)
        {
            // Use reflection to get the protected pawn field
            var pawn = PawnField?.GetValue(__instance) as Pawn;
            if (pawn != null && BabyAndroidUtil.IsBabyAndroid(pawn))
            {
                // Always show Play need for baby androids
                if (__instance.def == MD_DefOf.Play)
                {
                    __result = true;
                    return false; // Skip original getter
                }

                // List of need defs to hide
                var hiddenNeeds = new[]
                {
                    MD_DefOf.Joy,         // Recreation
                    MD_DefOf.Beauty,      // Beauty
                    MD_DefOf.Comfort      // Comfort
                    // Add more NeedDefs here if desired
                };

                if (hiddenNeeds.Contains(__instance.def))
                {
                    __result = false;
                    return false; // Skip original getter
                }
            }
            return true; // Use vanilla logic otherwise
        }
    }
}

