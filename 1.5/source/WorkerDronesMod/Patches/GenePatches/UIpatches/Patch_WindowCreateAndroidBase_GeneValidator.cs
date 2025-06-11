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
        // Use a prefix so we can override the result if necessary.
        static bool Prefix(Window_CreateAndroidBase __instance, ref bool __result)
        {
            List<GeneDef> selectedGenes = __instance.SelectedGenes;

            // Loop through every selected gene.
            foreach (GeneDef geneDef in selectedGenes)
            {
                // First, check if the gene has our multiple prerequisites extension.
                var prereqExt = geneDef.GetModExtension<GenePrerequisitesExtension>();
                if (prereqExt != null && prereqExt.prerequisiteGeneDefNames != null
                    && prereqExt.prerequisiteGeneDefNames.Count > 0)
                {
                    // Look for at least one gene in the selected list whose defName is in the prerequisites list.
                    bool foundAtLeastOne = selectedGenes.Any(g => prereqExt.prerequisiteGeneDefNames.Contains(g.defName));
                    if (!foundAtLeastOne)
                    {
                        // Show an error message. (Optionally, list one or more of the prerequisite names.)
                        string missingPrereqs = string.Join(", ", prereqExt.prerequisiteGeneDefNames);
                        Messages.Message("VREA.MessageComponentMissingPrerequisite".Translate(geneDef.label)
                            + ": " + missingPrereqs, null, MessageTypeDefOf.RejectInput, false);
                        __result = false;
                        return false; // Skip the original method.
                    }
                }
                else if (geneDef.prerequisite != null && !selectedGenes.Contains(geneDef.prerequisite))
                {
                    // If there's a single prerequisite defined normally and it's missing.
                    Messages.Message("VREA.MessageComponentMissingPrerequisite".Translate(geneDef.label)
                        .CapitalizeFirst() + ": " + geneDef.prerequisite.LabelCap, null, MessageTypeDefOf.RejectInput, false);
                    __result = false;
                    return false;
                }
            }
            // All selected genes pass the checks, so let the original method run.
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
                // Build the first message: "One of (N) required"
                int requiredCount = prereqExt.prerequisiteGeneDefNames.Count;
                string prereqMessage1 = $"One of ({requiredCount}) required";

                // Instead of listing the raw defNames, fetch each candidate gene’s label.
                var candidateOptionLabels = prereqExt.prerequisiteGeneDefNames
                    .Select((string defName) =>
                    {
                        GeneDef candidateGeneDef = DefDatabase<GeneDef>.GetNamedSilentFail(defName);
                        return candidateGeneDef != null ? candidateGeneDef.LabelCap.ToString() : defName;
                    });
                string optionsText = string.Join(", ", candidateOptionLabels);

                string prereqMessage2 = $"Options: {optionsText}";

                // Try to get the active creation window (of type Window_CreateAndroidBase).
                var window = Find.WindowStack?.Windows.OfType<Window_CreateAndroidBase>().FirstOrDefault();
                if (window != null)
                {
                    // Find which candidate prerequisite genes are currently selected.
                    var candidateSelected = window.SelectedGenes
                        .Where(g => prereqExt.prerequisiteGeneDefNames.Contains(g.defName))
                        .ToList();

                    if (candidateSelected.Count == 0)
                    {
                        // None selected: show the candidate list in red.
                        prereqMessage2 = $"Options: <color=red>{optionsText}</color>";
                    }
                    else if (candidateSelected.Count == 1)
                    {
                        // Only one is selected: show that gene’s label.
                        prereqMessage2 = $"Chosen: {candidateSelected.First().LabelCap}";
                    }
                    else
                    {
                        // More than one are selected: list all chosen gene labels.
                        string chosenText = string.Join(", ", candidateSelected.Select(g => g.LabelCap));
                        prereqMessage2 = $"Chosen: {chosenText}";
                    }
                }

                // Insert the two new lines after the second line of the original description.
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
                    // If no two lines exist, append the messages.
                    __result = __result + "\n" + prereqMessage1 + "\n" + prereqMessage2;
                }
            }
        }
    }
}
