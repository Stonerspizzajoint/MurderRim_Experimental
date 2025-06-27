using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Need_Rest), nameof(Need_Rest.NeedInterval))]
    public static class Patch_NeedRest_Interval_Override
    {
        private static readonly FieldInfo PawnField = typeof(Need).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPrefix]
        public static bool Prefix(Need_Rest __instance)
        {
            Pawn pawn = (Pawn)PawnField.GetValue(__instance);
            if (pawn == null) return true; // fallback

            // If android AND doesn't have memory sleep gene → force max rest
            if (pawn.IsAndroid() && (pawn.genes?.HasActiveGene(MD_DefOf.MD_MemorySleepProcessing) != true))
            {
                __instance.CurLevel = 1.0f; // lock at full
                return false; // skip original NeedInterval
            }

            return true; // run default logic otherwise
        }
    }
}

