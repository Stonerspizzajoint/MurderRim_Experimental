using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch]
    public static class Patch_PawnRenderer_GetBodyPos_AnimalBed
    {
        // Explicitly locate the target method using the proper type for the out parameter.
        static MethodBase TargetMethod()
        {
            // The target method signature: Vector3 GetBodyPos(Vector3, PawnPosture, out bool)
            return AccessTools.Method(typeof(PawnRenderer), "GetBodyPos", new Type[] { typeof(Vector3), typeof(PawnPosture), typeof(bool).MakeByRefType() });
        }

        static void Postfix(PawnRenderer __instance, Vector3 drawLoc, PawnPosture posture, ref bool showBody, ref Vector3 __result)
        {
            // Retrieve the pawn from the PawnRenderer using Traverse (since the field is private).
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null)
                return;

            // Only adjust if the pawn is executing a LayDown job.
            if (pawn.CurJob == null || pawn.CurJob.def != JobDefOf.LayDown)
                return;

            // Get the current bed.
            Building_Bed bed = pawn.CurrentBed();
            if (bed == null || bed.def.building == null)
                return;

            // Determine whether the bed qualifies as an animal bed.  
            // It qualifies if either its building def is not marked as humanlike,
            // or if its defName starts with "AnimalBedFurnitureBase".
            bool isAnimalBed = (!bed.def.building.bed_humanlike) || bed.def.defName.StartsWith("AnimalBedFurnitureBase");
            if (!isAnimalBed)
                return;

            // Only override if the pawn has our custom component that allows animal bed usage.
            CompAnimalBedUser comp = pawn.TryGetComp<CompAnimalBedUser>();
            if (comp == null || !comp.Props.canUseAnimalBeds)
                return;

            // Override the body position with the bed's DrawPos plus an offset.
            // You may need to tweak the offset vector until the pawn's texture lines up as desired.
            __result = bed.DrawPos + comp.Props.sleepDrawOffset;

            // Optionally force showBody to true.
            showBody = true;
        }
    }
    [HarmonyPatch(typeof(PawnRenderer), "BodyAngle")]
    public static class Patch_PawnRenderer_BodyAngle_AnimalBed
    {
        static bool Prefix(PawnRenderer __instance, PawnRenderFlags flags, ref float __result)
        {
            // Retrieve the pawn. Use Traverse to access the private "pawn" field.
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null)
                return true;

            // Only adjust if the pawn is lying down.
            if (pawn.CurJob == null || pawn.CurJob.def != JobDefOf.LayDown)
                return true;

            // Get the pawn's current bed.
            Building_Bed bed = pawn.CurrentBed();
            if (bed == null || bed.def.building == null)
                return true;

            // Check if the bed qualifies as an animal bed.
            bool isAnimalBed = (!bed.def.building.bed_humanlike) ||
                               bed.def.defName.StartsWith("AnimalBedFurnitureBase");
            if (!isAnimalBed)
                return true;

            // Only adjust for pawns with the custom component.
            CompAnimalBedUser comp = pawn.TryGetComp<CompAnimalBedUser>();
            if (comp == null || !comp.Props.canUseAnimalBeds)
                return true;

            // Instead of adding a default 2 (i.e. 180°) offset,
            // we add 1 to its current rotation value, which amounts to a 90° offset.
            int newRotInt = (bed.Rotation.AsInt + 1) % 4;
            Rot4 newRot = new Rot4(newRotInt);
            __result = newRot.AsAngle;
            return false;
        }
    }
}
