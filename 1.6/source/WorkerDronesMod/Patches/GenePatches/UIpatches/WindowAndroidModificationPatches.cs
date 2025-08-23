using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using VREAndroids;
using WorkerDronesMod; // <-- Make sure to include this for GeneDisplayingUtils

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Window_CreateAndroidBase), "GeneValidator", new Type[] { typeof(GeneDef) })]
    public static class Patch_Window_CreateAndroidBase_GeneValidator
    {
        [HarmonyPostfix]
        public static void Postfix(Window_CreateAndroidBase __instance, GeneDef x, ref bool __result)
        {
            if (!__result) return;

            // Use stricter logic for Window_AndroidCreation and Window_AndroidModification
            if (__instance is Window_AndroidCreation || __instance is Window_AndroidModification)
            {
                if (!GeneDisplayingUtils.IsGeneAllowed(x))
                    __result = false;
            }
            // Use base logic for starting colonist creation (or other uses)
            else
            {
                if (!GeneDisplayingUtils.IsGeneAllowedForBase(x))
                    __result = false;
            }
        }
    }


    // Transpiler for the DrawSearchRect method in Window_CreateAndroidXenotype
    [HarmonyPatch(typeof(Window_CreateAndroidXenotype), "DrawSearchRect")]
    public static class Patch_Window_CreateAndroidXenotype_DrawSearchRect
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var targetField = AccessTools.Field(typeof(CustomXenotype), "genes");
            var whereMethod = GeneDisplayingUtils.GetWhereMethod();
            bool patched = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (!patched && codes[i].LoadsField(targetField))
                {
                    patched = true;
                    yield return codes[i];
                    yield return new CodeInstruction(OpCodes.Ldnull);
                    yield return new CodeInstruction(OpCodes.Ldftn, AccessTools.Method(typeof(GeneDisplayingUtils), nameof(GeneDisplayingUtils.IsGeneAllowed)));
                    yield return new CodeInstruction(OpCodes.Newobj,
                        AccessTools.Constructor(typeof(Func<GeneDef, bool>), new[] { typeof(object), typeof(IntPtr) }));
                    yield return new CodeInstruction(OpCodes.Call, whereMethod);
                    continue;
                }
                yield return codes[i];
            }
        }
    }


    // Patch AcceptInner for the modification window (which compares against an existing android).
    [HarmonyPatch(typeof(Window_AndroidCreation), "AcceptInner")]
    public static class Patch_Window_AndroidCreation_AcceptInner
    {
        [HarmonyPrefix]
        public static bool Prefix(Window_AndroidCreation __instance)
        {
            bool xenotypeLoaded = Traverse.Create(__instance).Field("xenotypeNameLocked").GetValue<bool>();
            if (!xenotypeLoaded) return true;

            List<GeneDef> selectedGenes = Traverse.Create(__instance).Field("selectedGenes").GetValue<List<GeneDef>>();
            if (selectedGenes == null) return true;

            var invalidGeneInfos = selectedGenes
                .Where(g => !GeneDisplayingUtils.IsGeneAllowed(g))
                .Select(g => (Gene: g, Reason: GeneDisplayingUtils.GetBlockReason(g)))
                .ToList();

            if (invalidGeneInfos.Any())
            {
                TaggedString message = "This xenotype contains genes that cannot be used:\n\n";
                message += GenText.ToLineList(
                    invalidGeneInfos.Select(x => $"{x.Gene.LabelCap}: {x.Reason}"),
                    "  - "
                );
                Find.WindowStack.Add(new Dialog_MessageBox(
                    text: message,
                    buttonAText: "OK".Translate(),
                    buttonADestructive: false
                ));
                return false;
            }
            return true;
        }
    }

    // Patch AcceptInner for the modification window.
    [HarmonyPatch(typeof(Window_AndroidModification), "AcceptInner")]
    public static class Patch_Window_AndroidModification_AcceptInner
    {
        [HarmonyPrefix]
        public static bool Prefix(Window_AndroidModification __instance)
        {
            bool xenotypeLoaded = Traverse.Create(__instance).Field("xenotypeNameLocked").GetValue<bool>();
            if (!xenotypeLoaded) return true;

            List<GeneDef> selectedGenes = Traverse.Create(__instance).Field("selectedGenes").GetValue<List<GeneDef>>();
            if (selectedGenes == null) return true;

            List<GeneDef> activeGenes = __instance.android.genes.GenesListForReading.Select(g => g.def).ToList();
            var invalidGenes = selectedGenes.Where(g =>
            {
                if (!GeneDisplayingUtils.IsGeneAllowed(g))
                    return true;
                var researchExt = g.GetModExtension<GeneResearchExtension>();
                if (researchExt != null && researchExt.requiredResearch != null && !researchExt.requiredResearch.IsFinished)
                    return !activeGenes.Contains(g);
                return false;
            }).ToList();

            if (invalidGenes.Any())
            {
                List<string> invalidLabels = invalidGenes
                    .Select(g => GeneDisplayingUtils.GetBlockReason(g))
                    .Distinct()
                    .ToList();

                TaggedString message = "This xenotype contains components that require unfinished research or are blocked:\n\n";
                message += GenText.ToLineList(invalidLabels, "  - ");
                Find.WindowStack.Add(new Dialog_MessageBox(
                    text: message,
                    buttonAText: "OK".Translate(),
                    buttonADestructive: false
                ));
                return false;
            }
            return true;
        }
    }
}












