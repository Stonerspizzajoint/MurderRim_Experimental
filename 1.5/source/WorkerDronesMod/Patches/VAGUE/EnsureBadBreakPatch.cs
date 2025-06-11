using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches.VAGUE
{
    [HarmonyPatch]
    internal static class EnsureBadBreakPatch
    {
        // Dynamically find the type and method using AccessTools.
        static MethodBase TargetMethod()
        {
            // Look up the type by its fully qualified name.
            Type targetType = AccessTools.TypeByName("VAGUE.BrokenJoyCurcuitsHandlerPatch");
            if (targetType == null)
            {
                Log.Error("[EnsureBadBreakPatch] Could not find type 'VAGUE.BrokenJoyCurcuitsHandlerPatch'.");
                return null;
            }

            MethodInfo targetMethod = AccessTools.Method(targetType, "EnsureBadBreakForPsychopathAndroids");
            if (targetMethod == null)
            {
                Log.Error("[EnsureBadBreakPatch] Could not find method 'EnsureBadBreakForPsychopathAndroids' on type 'VAGUE.BrokenJoyCurcuitsHandlerPatch'.");
            }
            return targetMethod;
        }

        // Prefix patch that checks for null components.
        [HarmonyPrefix]
        public static bool Prefix(Gene_SyntheticBody __instance, ref bool __result)
        {
            if (__instance?.pawn == null)
            {
                __result = true;
                return false;
            }

            // Check critical pawn components. If any are missing (likely due to death), skip the original.
            if (__instance.pawn.needs == null ||
                __instance.pawn.needs.mood == null ||
                __instance.pawn.genes == null ||
                __instance.pawn.mindState == null ||
                __instance.pawn.story == null)
            {
                __result = true;
                return false;
            }

            // Otherwise, allow the original method to run.
            return true;
        }
    }
}
