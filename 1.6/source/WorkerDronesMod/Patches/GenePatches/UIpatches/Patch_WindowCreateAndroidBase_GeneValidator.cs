using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;
using VREAndroids; // (or whichever namespace contains your GenePrerequisitesExtension)

namespace WorkerDronesMod.Patches
{

    // Patch the CanAccept method in Window_CreateAndroidBase
    [HarmonyPatch(typeof(Window_CreateAndroidBase), "CanAccept")]
    public static class Patch_WindowCreateAndroidBase_CanAccept
    {
        static bool Prefix(Window_CreateAndroidBase __instance, ref bool __result)
        {
            List<GeneDef> selectedGenes = __instance.SelectedGenes;

            foreach (GeneDef geneDef in selectedGenes)
            {
                var prereqExt = geneDef.GetModExtension<GenePrerequisitesExtension>();
                if (prereqExt != null && prereqExt.prerequisiteGeneDefNames != null && prereqExt.prerequisiteGeneDefNames.Count > 0)
                {
                    // Accept if any selected gene matches the original or VREA_ counterpart
                    bool foundAtLeastOne = selectedGenes.Any(g =>
                        prereqExt.prerequisiteGeneDefNames.Contains(g.defName) ||
                        (g.defName.StartsWith("VREA_") && prereqExt.prerequisiteGeneDefNames.Contains(g.defName.Substring("VREA_".Length))) ||
                        prereqExt.prerequisiteGeneDefNames.Any(prereq => g.defName == "VREA_" + prereq)
                    );
                    if (!foundAtLeastOne)
                    {
                        string missingPrereqs = string.Join(", ", prereqExt.prerequisiteGeneDefNames);
                        Messages.Message("VREA.MessageComponentMissingPrerequisite".Translate(geneDef.label)
                            + ": " + missingPrereqs, null, MessageTypeDefOf.RejectInput, false);
                        __result = false;
                        return false;
                    }
                }
                else if (geneDef.prerequisite != null && !selectedGenes.Contains(geneDef.prerequisite))
                {
                    Messages.Message("VREA.MessageComponentMissingPrerequisite".Translate(geneDef.label)
                        .CapitalizeFirst() + ": " + geneDef.prerequisite.LabelCap, null, MessageTypeDefOf.RejectInput, false);
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GeneDef), "get_DescriptionFull")]
    public static class Patch_GeneDef_DescriptionFull
    {
        static void Postfix(GeneDef __instance, ref string __result)
        {
            var prereqExt = __instance.GetModExtension<GenePrerequisitesExtension>();
            if (prereqExt != null &&
                prereqExt.prerequisiteGeneDefNames != null &&
                prereqExt.prerequisiteGeneDefNames.Count > 0)
            {
                int requiredCount = prereqExt.prerequisiteGeneDefNames.Count;
                string prereqMessage1 = $"One of ({requiredCount}) required";

                // Display only the original gene labels
                var candidateOptionLabels = prereqExt.prerequisiteGeneDefNames
                    .Select(defName =>
                    {
                        GeneDef candidateGeneDef = DefDatabase<GeneDef>.GetNamedSilentFail(defName);
                        return candidateGeneDef != null ? candidateGeneDef.LabelCap.ToString() : defName;
                    });
                string optionsText = string.Join(", ", candidateOptionLabels);

                string prereqMessage2 = $"Options: {optionsText}";

                var window = Find.WindowStack?.Windows.OfType<Window_CreateAndroidBase>().FirstOrDefault();
                if (window != null)
                {
                    // Accept both original and VREA_ counterparts as selected
                    var candidateSelected = window.SelectedGenes
                        .Where(g =>
                            prereqExt.prerequisiteGeneDefNames.Contains(g.defName) ||
                            (g.defName.StartsWith("VREA_") && prereqExt.prerequisiteGeneDefNames.Contains(g.defName.Substring("VREA_".Length))) ||
                            prereqExt.prerequisiteGeneDefNames.Any(prereq => g.defName == "VREA_" + prereq)
                        )
                        .ToList();

                    if (candidateSelected.Count == 0)
                    {
                        prereqMessage2 = $"Options: <color=red>{optionsText}</color>";
                    }
                    else if (candidateSelected.Count == 1)
                    {
                        // Show the label of the original gene if possible
                        string label = candidateOptionLabels.FirstOrDefault(l =>
                            l == candidateSelected.First().LabelCap.ToString() ||
                            (candidateSelected.First().defName.StartsWith("VREA_") && l == candidateSelected.First().defName.Substring("VREA_".Length))
                        );
                        prereqMessage2 = $"Chosen: {label ?? candidateSelected.First().LabelCap}";
                    }
                    else
                    {
                        // List all chosen gene labels, showing only the original label if possible
                        string chosenText = string.Join(", ", candidateSelected.Select(g =>
                        {
                            GeneDef orig = DefDatabase<GeneDef>.GetNamedSilentFail(g.defName.StartsWith("VREA_") ? g.defName.Substring("VREA_".Length) : g.defName);
                            return orig != null ? orig.LabelCap.ToString() : g.LabelCap.ToString();
                        }));
                        prereqMessage2 = $"Chosen: {chosenText}";
                    }
                }

                var lines = __result.Split(new[] { '\n' }, StringSplitOptions.None);
                if (lines.Length >= 2)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(lines[0]);
                    sb.AppendLine(lines[1]);
                    sb.AppendLine(prereqMessage1);
                    sb.AppendLine(prereqMessage2);
                    for (int i = 2; i < lines.Length; i++)
                    {
                        sb.AppendLine(lines[i]);
                    }
                    __result = sb.ToString().TrimEnd('\n', '\r');
                }
                else
                {
                    __result = __result + "\n" + prereqMessage1 + "\n" + prereqMessage2;
                }
            }
        }
    }
}
