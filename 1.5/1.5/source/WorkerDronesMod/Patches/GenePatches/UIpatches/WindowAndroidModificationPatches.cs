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

namespace WorkerDronesMod.Patches
{

    // ─── HELPER CLASSES ─────────────────────────────────────────────────────
    // Helper class that checks whether a gene’s required research is complete
    // and also validates an entire gene selection for required genes.
    public static class GeneValidatorHelper
    {
        /// <summary>
        /// Returns true if the gene is allowed (its required research is finished, and it isn't blocked).
        /// </summary>
        public static bool Validate(GeneDef gene)
        {
            // 1) Research requirement
            var researchExt = gene.GetModExtension<GeneResearchExtension>();
            if (researchExt != null && researchExt.requiredResearch != null && !researchExt.requiredResearch.IsFinished)
            {
                return false;
            }

            // 2) Block‐from‐window requirement
            var blockExt = gene.GetModExtension<BlockFromAndroidWindowExtension>();
            if (blockExt != null && blockExt.blockFromAndroidWindow)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the current selection contains at least one of the required genes
        /// (taking into account grouping by geneTypeTag) and that each required gene’s research is finished.
        /// </summary>
        public static bool ValidateSelection(IEnumerable<GeneDef> genes)
        {
            // Instead of a simple Any(), one might perform a similar grouping check as below.
            var required = genes.Where(g =>
            {
                var blockExt = g.GetModExtension<BlockFromAndroidWindowExtension>();
                if (blockExt == null || !blockExt.mustHaveAtLeastOne)
                    return false;

                var researchExt = g.GetModExtension<GeneResearchExtension>();
                if (researchExt != null && researchExt.requiredResearch != null && !researchExt.requiredResearch.IsFinished)
                    return false;

                return true;
            });

            // One could further group by geneTypeTag here if needed.
            return required.Any();
        }
    }

    // Helper to retrieve the correct Enumerable.Where method for GeneDef.
    public static class ReflectionHelper
    {
        public static MethodInfo GetWhereMethod()
        {
            var candidate = typeof(Enumerable)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Where" &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[1].ParameterType.IsGenericType &&
                    m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>));
            if (candidate == null)
            {
                throw new InvalidOperationException("Could not find Enumerable.Where<TSource>(IEnumerable<TSource>, Func<TSource,bool>)");
            }
            return candidate.MakeGenericMethod(typeof(GeneDef));
        }
    }

    // ─── PATCHES FOR THE MODIFICATION WINDOW ─────────────────────────────

    // Patch the GeneValidator method in the modification window to prevent invalid genes.
    [HarmonyPatch(typeof(Window_AndroidModification))]
    public static class WindowCreateAndroidBasePatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("GeneValidator", new Type[] { typeof(GeneDef) })]
        public static void GeneValidatorPostfix(GeneDef x, ref bool __result)
        {
            if (!__result)
                return;

            if (!GeneValidatorHelper.Validate(x))
            {
                __result = false;
            }
        }
    }

    // Transpiler for the DrawSearchRect method in Window_CreateAndroidXenotype
    [HarmonyPatch(typeof(Window_CreateAndroidXenotype))]
    public static class WindowCreateAndroidXenotypePatches
    {
        [HarmonyTranspiler]
        [HarmonyPatch("DrawSearchRect")]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var targetField = AccessTools.Field(typeof(CustomXenotype), "genes");
            var whereMethod = ReflectionHelper.GetWhereMethod();
            bool patched = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (!patched && codes[i].LoadsField(targetField))
                {
                    patched = true;
                    yield return codes[i];
                    yield return new CodeInstruction(OpCodes.Ldnull);
                    yield return new CodeInstruction(OpCodes.Ldftn, AccessTools.Method(typeof(GeneValidatorHelper), "Validate"));
                    yield return new CodeInstruction(OpCodes.Newobj,
                        AccessTools.Constructor(typeof(Func<GeneDef, bool>), new[] { typeof(object), typeof(IntPtr) }));
                    yield return new CodeInstruction(OpCodes.Call, whereMethod);
                    continue;
                }
                yield return codes[i];
            }
        }
    }

    // In the creation window, the gene list comes from the "selectedGenes" field.
    [HarmonyPatch(typeof(Window_CreateAndroidBase))]
    public static class WindowAndroidCreation_DrawSearchRect_Prefix
    {
        [HarmonyPostfix]
        [HarmonyPatch("GeneValidator", new Type[] { typeof(GeneDef) })]
        public static void GeneValidatorPostfix(Window_AndroidCreation __instance, GeneDef x, ref bool __result)
        {
            if (!__result)
                return;

            var station = Traverse.Create(__instance).Field("station").GetValue<Building_AndroidCreationStation>();
            if (station == null)
                return;

            if (!GeneValidatorHelper.Validate(x))
            {
                __result = false;
            }
        }
    }

    // ─── ACCEPT INNER PATCHES ───────────────────────────────────────────────

    // Patch AcceptInner for the modification window (which compares against an existing android).
    [HarmonyPatch(typeof(Window_AndroidCreation))]
    public static class CustomXenotypeValidatorCreationPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("AcceptInner")]
        public static bool ValidateCustomXenotype(Window_AndroidCreation __instance)
        {
            bool xenotypeLoaded = Traverse.Create(__instance).Field("xenotypeNameLocked").GetValue<bool>();
            if (!xenotypeLoaded)
                return true;

            List<GeneDef> selectedGenes = Traverse.Create(__instance).Field("selectedGenes").GetValue<List<GeneDef>>();
            if (selectedGenes == null)
                return true;

            var validGenes = selectedGenes.Where(g => GeneValidatorHelper.Validate(g)).ToList();

            var requiredGenes = Utils.AndroidGenesGenesInOrder
                .Where(g => g.GetModExtension<BlockFromAndroidWindowExtension>() != null &&
                            g.GetModExtension<BlockFromAndroidWindowExtension>().cannotBeRemoved)
                .ToList();

            var missingRequired = requiredGenes.Where(g => !selectedGenes.Contains(g)).ToList();
            if (missingRequired.Any())
            {
                validGenes.AddRange(missingRequired);
                Traverse.Create(__instance).Field("selectedGenes").SetValue(validGenes);

                List<string> invalidLabels = missingRequired.Select(g => $"Locked: {g.label}").Distinct().ToList();
                TaggedString message = "You cannot remove the following locked genes:\n\n";
                message += GenText.ToLineList(invalidLabels, "  - ");

                Find.WindowStack.Add(new Dialog_MessageBox(
                    text: message,
                    buttonAText: "OK".Translate(),
                    buttonADestructive: false
                ));

                return false;
            }

            if (validGenes.Count != selectedGenes.Count)
            {
                Traverse.Create(__instance).Field("selectedGenes").SetValue(validGenes);
                List<string> researchLabels = selectedGenes
                    .Where(g => !validGenes.Contains(g))
                    .Select(g => g.GetModExtension<GeneResearchExtension>().requiredResearch.LabelCap.ToString())
                    .Distinct()
                    .ToList();

                TaggedString message = "This xenotype contains genes that require unfinished research:\n\n";
                message += GenText.ToLineList(researchLabels, "  - ");

                Find.WindowStack.Add(new Dialog_MessageBox(
                    text: message,
                    buttonAText: "OK".Translate(),
                    buttonADestructive: false
                ));
            }

            return true;
        }
    }

    // Patch AcceptInner for the modification window.
    [HarmonyPatch(typeof(Window_AndroidModification))]
    public static class CustomXenotypeValidatorPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("AcceptInner")]
        public static bool ValidateCustomXenotype(Window_AndroidModification __instance)
        {
            bool xenotypeLoaded = Traverse.Create(__instance).Field("xenotypeNameLocked").GetValue<bool>();
            if (!xenotypeLoaded)
                return true;

            List<GeneDef> selectedGenes = Traverse.Create(__instance).Field("selectedGenes").GetValue<List<GeneDef>>();
            if (selectedGenes == null)
                return true;

            List<GeneDef> activeGenes = __instance.android.genes.GenesListForReading.Select(g => g.def).ToList();
            var invalidGenes = selectedGenes.Where(g =>
            {
                var researchExt = g.GetModExtension<GeneResearchExtension>();
                if (researchExt != null && researchExt.requiredResearch != null && !researchExt.requiredResearch.IsFinished)
                {
                    return !activeGenes.Contains(g);
                }
                var blockExt = g.GetModExtension<BlockFromAndroidWindowExtension>();
                if (blockExt != null && blockExt.blockFromAndroidWindow)
                {
                    return true;
                }
                return false;
            }).ToList();

            if (invalidGenes.Any())
            {
                List<string> invalidLabels = invalidGenes
                    .Select(g =>
                    {
                        var blockExt = g.GetModExtension<BlockFromAndroidWindowExtension>();
                        if (blockExt != null && blockExt.blockFromAndroidWindow)
                        {
                            return $"Blocked: {g.label}";
                        }
                        return g.GetModExtension<GeneResearchExtension>().requiredResearch.LabelCap.ToString();
                    })
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

    // Patch the CanAccept method in the base class for Window_AndroidModification.
    [HarmonyPatch(typeof(Window_CreateAndroidBase), nameof(Window_CreateAndroidBase.CanAccept))]
    public static class Patch_Window_CreateAndroidBase_CanAccept
    {
        private static readonly Dictionary<Window_AndroidModification, List<GeneDef>> originalLocked
            = new Dictionary<Window_AndroidModification, List<GeneDef>>();

        static bool Prefix(Window_CreateAndroidBase __instance, ref bool __result)
        {
            if (!(__instance is Window_AndroidModification modWin))
                return true;

            if (!originalLocked.ContainsKey(modWin))
            {
                var pawn = modWin.android;
                if (pawn?.genes == null)
                    return true;

                originalLocked[modWin] = pawn.genes.GenesListForReading
                    .Where(g => g.def.GetModExtension<BlockFromAndroidWindowExtension>()?.cannotBeRemoved == true)
                    .Select(g => g.def)
                    .ToList();
            }

            var lockedOriginally = originalLocked[modWin];
            var selected = modWin.SelectedGenes;

            var missing = lockedOriginally
                .Where(g => !selected.Contains(g))
                .ToList();

            if (missing.Any())
            {
                selected.AddRange(missing);
                modWin.OnGenesChanged();

                var lines = missing.Select(g => " - " + g.LabelCap);
                string msg =
                    "The following component(s) are locked on this android and cannot be removed. They have been re‑added:\n\n"
                    + string.Join("\n", lines)
                    + "\n\nPlease confirm again when you’re ready.";

                Find.WindowStack.Add(new Dialog_MessageBox(msg, "OK".Translate()));

                __result = false;
                return false;
            }

            return true;
        }
    }

    // ─── PATCH FOR NON‑REMOVABLE GENES WITH WARNING (UPDATED WITH TYPE TAG LOGIC) ─────
    [HarmonyPatch(typeof(Window_CreateAndroidBase), "CanAccept")]
    public static class Patch_Window_CreateAndroidBase_CanAccept_Warning
    {
        static bool Prefix(Window_CreateAndroidBase __instance, ref bool __result)
        {
            List<GeneDef> newlyLocked = __instance.SelectedGenes
                .Where(g => g.GetModExtension<BlockFromAndroidWindowExtension>()?.cannotBeRemoved == true)
                .Distinct()
                .ToList();

            Pawn pawn = null;
            if (__instance is Window_AndroidModification modWin)
            {
                pawn = modWin.android;
            }
            if (pawn != null && pawn.genes != null)
            {
                var pawnLocked = pawn.genes.GenesListForReading
                    .Where(pg => pg.def.GetModExtension<BlockFromAndroidWindowExtension>()?.cannotBeRemoved == true)
                    .Select(pg => pg.def)
                    .ToList();
                newlyLocked = newlyLocked.Where(g => !pawnLocked.Contains(g)).ToList();
            }

            if (!newlyLocked.Any())
                return true;

            var lines = newlyLocked.Select(g => " - " + g.label);
            string msg = "The following component(s) are non‑removable during normal gameplay:\n\n" +
                         string.Join("\n", lines) +
                         "\n\nDo you want to continue with these components or remove them from your selection?";

            Dialog_MessageBox dialog = new Dialog_MessageBox(
                msg,
                "Continue",
                () =>
                {
                    // Updated required genes check to enforce at least one per geneTypeTag
                    var required = Utils.AndroidGenesGenesInOrder
                        .Where(g => g.GetModExtension<BlockFromAndroidWindowExtension>()?.mustHaveAtLeastOne == true)
                        .ToList();

                    var requiredGroups = required.GroupBy(g => g.GetModExtension<BlockFromAndroidWindowExtension>()?.geneTypeTag ?? "");
                    List<GeneDef> missing = new List<GeneDef>();

                    foreach (var group in requiredGroups)
                    {
                        string typeTag = group.Key;
                        bool groupSatisfied = __instance.SelectedGenes.Any(sel =>
                        {
                            var ext = sel.GetModExtension<BlockFromAndroidWindowExtension>();
                            return ext != null && ext.mustHaveAtLeastOne && (ext.geneTypeTag ?? "") == typeTag;
                        });
                        if (!groupSatisfied)
                        {
                            missing.AddRange(group);
                        }
                    }

                    if (__instance.SelectedGenes == null || missing.Any())
                    {
                        // Open a dialog to force picking missing required genes.
                        Find.WindowStack.Add(new Dialog_ChooseRequiredGene(__instance, missing));
                    }
                    else
                    {
                        __instance.Accept();
                    }
                },
                "Remove Components",
                () =>
                {
                    foreach (var gene in newlyLocked)
                    {
                        __instance.SelectedGenes.Remove(gene);
                    }
                    __instance.OnGenesChanged();
                }
            );

            Find.WindowStack.Add(dialog);

            __result = false;
            return false;
        }
    }

    // ─── PATCH FOR REQUIRED GENES (UPDATED WITH TYPE TAG LOGIC) ─────────────
    [HarmonyPatch(typeof(Window_CreateAndroidBase), "CanAccept")]
    public static class Patch_Window_CreateAndroidBase_CanAccept_RequiredGenes
    {
        [HarmonyPrefix]
        public static bool Prefix(Window_CreateAndroidBase __instance, ref bool __result)
        {
            var selected = __instance.SelectedGenes;
            if (selected == null) return true;

            var required = Utils.AndroidGenesGenesInOrder
                .Where(g => g.GetModExtension<BlockFromAndroidWindowExtension>()?.mustHaveAtLeastOne == true)
                .ToList();

            // Group required genes by their geneTypeTag (using empty string for null).
            var requiredGroups = required.GroupBy(g => g.GetModExtension<BlockFromAndroidWindowExtension>()?.geneTypeTag ?? "");
            List<GeneDef> missing = new List<GeneDef>();

            foreach (var group in requiredGroups)
            {
                string tag = group.Key;
                bool groupSatisfied = selected.Any(sel =>
                {
                    var ext = sel.GetModExtension<BlockFromAndroidWindowExtension>();
                    return ext != null && ext.mustHaveAtLeastOne && (ext.geneTypeTag ?? "") == tag;
                });
                if (!groupSatisfied)
                {
                    missing.AddRange(group);
                }
            }

            if (!missing.Any())
                return true;

            Find.WindowStack.Add(new Dialog_ChooseRequiredGene(__instance, missing));
            __result = false;
            return false;
        }
    }

    // ─── CUSTOM REQUIRED GENE SELECTION DIALOG (UPDATED HEADER) ─────────────
    [StaticConstructorOnStartup]
    public class Dialog_ChooseRequiredGene : Window
    {
        private readonly Window_CreateAndroidBase parentWindow;
        private readonly List<GeneDef> missingGenes;

        // Added scroll support.
        private Vector2 scrollPosition = Vector2.zero;

        private const float IconSize = 64f;
        private const float Padding = 12f;
        private const float GroupHeaderHeight = 30f; // Height for each group's header

        // Load custom background texture once.
        private static readonly Texture2D GeneBgTex =
            ContentFinder<Texture2D>.Get("UI/Icons/Genes/GeneBackground_Hardware", true);

        public override Vector2 InitialSize => new Vector2(520f, 340f);

        public Dialog_ChooseRequiredGene(Window_CreateAndroidBase parent, List<GeneDef> missing)
        {
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            closeOnAccept = false;
            closeOnCancel = false;

            parentWindow = parent;
            missingGenes = missing;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // 1) Draw the header.
            Text.Font = GameFont.Medium;
            string headerText = "Select at least one required component";
            var distinctTags = missingGenes
                .Select(g => g.GetModExtension<BlockFromAndroidWindowExtension>()?.geneTypeTag)
                .Where(tag => !string.IsNullOrEmpty(tag))
                .Distinct()
                .ToList();
            if (distinctTags.Any())
            {
                headerText = "Select at least one required component for each type";
            }
            // Calculate header height dynamically.
            float headerHeight = Text.CalcHeight(headerText, inRect.width);
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, headerHeight), headerText);
            float headerBottom = inRect.y + headerHeight + Padding;

            // 2) Set up the scrollable area (reserve space at bottom for Cancel button).
            float scrollAreaHeight = inRect.height - headerBottom - 40f;
            Rect scrollRect = new Rect(inRect.x, headerBottom, inRect.width, scrollAreaHeight);

            // Group missing genes by geneTypeTag (defaulting to "Other" if empty).
            var groups = missingGenes
                .GroupBy(g =>
                {
                    string tag = g.GetModExtension<BlockFromAndroidWindowExtension>()?.geneTypeTag;
                    return string.IsNullOrEmpty(tag) ? "Other" : tag;
                })
                .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            // Calculate total content height.
            float contentHeight = 0f;
            float usableWidth = inRect.width - Padding * 2;
            int columns = Mathf.FloorToInt(usableWidth / (IconSize + Padding));
            if (columns < 1)
                columns = 1;
            foreach (var group in groups)
            {
                contentHeight += GroupHeaderHeight + Padding; // header and spacing
                int count = group.Count();
                int rows = Mathf.CeilToInt(count / (float)columns);
                float gridHeight = rows * (IconSize + Padding) + Padding;
                contentHeight += gridHeight;
            }
            Rect contentRect = new Rect(0, 0, scrollRect.width - 16f, contentHeight);

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, contentRect, true);
            float curY = 0f;
            Text.Font = GameFont.Small;

            // 3) Draw each group and its gene icons.
            foreach (var group in groups)
            {
                // Draw group header.
                Rect groupHeaderRect = new Rect(Padding, curY, contentRect.width - Padding * 2, GroupHeaderHeight);
                Widgets.Label(groupHeaderRect, $"{group.Key}");
                curY += GroupHeaderHeight + Padding;

                int groupCount = group.Count();
                for (int i = 0; i < groupCount; i++)
                {
                    int col = i % columns;
                    int row = i / columns;
                    float x = Padding + col * (IconSize + Padding);
                    float y = curY + row * (IconSize + Padding);
                    Rect iconRect = new Rect(x, y, IconSize, IconSize);

                    GeneDef gene = group.ElementAt(i);

                    // Draw custom background.
                    GUI.color = gene.IconColor * 0.3f;
                    GUI.DrawTexture(iconRect, GeneBgTex);

                    // Draw gene icon.
                    Texture2D tex = gene.Icon ?? BaseContent.BadTex;
                    GUI.color = gene.IconColor;
                    GUI.DrawTexture(iconRect, tex);

                    GUI.color = Color.white;
                    Widgets.DrawHighlightIfMouseover(iconRect);

                    // Combine the gene's label and full description for the tooltip.
                    string tooltipText = $"{gene.LabelCap}\n\n{gene.DescriptionFull}";
                    TooltipHandler.TipRegion(iconRect, tooltipText);

                    // Draw highlight overlay if selected.
                    if (parentWindow.SelectedGenes.Contains(gene))
                    {
                        Widgets.DrawBoxSolid(iconRect, new Color(0, 1, 0, 0.3f));
                    }

                    // Toggle selection on click.
                    if (Widgets.ButtonInvisible(iconRect))
                    {
                        if (parentWindow.SelectedGenes.Contains(gene))
                        {
                            parentWindow.SelectedGenes.Remove(gene);
                            parentWindow.OnGenesChanged();
                            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                        }
                        else
                        {
                            parentWindow.SelectedGenes.Add(gene);
                            parentWindow.OnGenesChanged();
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        }
                    }
                }
                // Move curY down past the group's icon grid.
                int rows = Mathf.CeilToInt(groupCount / (float)columns);
                float gridHeight = rows * (IconSize + Padding) + Padding;
                curY += gridHeight;
            }
            Widgets.EndScrollView();

            // 4) Draw the Cancel button.
            Rect cancelRect = new Rect(
                inRect.x + inRect.width - 120f,
                inRect.yMax - 35f,
                100f,
                30f
            );
            if (Widgets.ButtonText(cancelRect, "Cancel"))
            {
                SoundDefOf.CancelMode.PlayOneShotOnCamera();
                Close();
            }

            // 5) Auto-close if all required groups are satisfied.
            if (RequiredGroupsSatisfied())
            {
                Close();
            }
        }

        private bool RequiredGroupsSatisfied()
        {
            // Gather all genes flagged as required.
            var required = missingGenes.Where(g => g.GetModExtension<BlockFromAndroidWindowExtension>()?.mustHaveAtLeastOne == true);
            // Group by geneTypeTag (using empty string if null).
            var groups = required.GroupBy(g => g.GetModExtension<BlockFromAndroidWindowExtension>()?.geneTypeTag ?? "");
            // For each required group, check that at least one gene in the parent's selection fulfills that group.
            foreach (var group in groups)
            {
                bool groupSatisfied = parentWindow.SelectedGenes.Any(sel =>
                {
                    var ext = sel.GetModExtension<BlockFromAndroidWindowExtension>();
                    return ext != null && ext.mustHaveAtLeastOne && (ext.geneTypeTag ?? "") == group.Key;
                });
                if (!groupSatisfied)
                    return false;
            }
            return true;
        }
    }



    // ─── GENE TOOLTIP PATCH ───────────────────────────────────────────────
    [HarmonyPatch(typeof(GeneDef), "get_DescriptionFull")]
    public static class Patch_GeneTooltip
    {
        static void Postfix(GeneDef __instance, ref string __result)
        {
            if (__instance.modExtensions != null)
            {
                foreach (var ext in __instance.modExtensions)
                {
                    if (ext is GeneResearchExtension geneExt && geneExt.requiredResearch != null)
                    {
                        __result += $"\n<b>Unlocked by:</b> {geneExt.requiredResearch.label}";
                    }
                    if (ext is BlockFromAndroidWindowExtension blockExt && blockExt.cannotBeRemoved)
                    {
                        __result += "\n<b><color=red>Cannot be removed</color></b>";
                    }
                }
            }
        }
    }
}











