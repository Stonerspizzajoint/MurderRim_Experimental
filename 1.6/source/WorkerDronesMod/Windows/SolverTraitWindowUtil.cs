using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WorkerDronesMod
{
    public class SolverTraitWindowUtil
    {
        private Pawn pawn;
        private SolverTraitProgress progress;
        private Vector2 windowPos = new Vector2(200, 100);
        private Vector2 windowSize = new Vector2(900, 600);
        private Vector2 scrollPos = Vector2.zero;
        private float gridCellSize = 96f;
        private float gridPadding = 32f;
        private SolverTraitDef selectedTrait;
        private SolverTraitDef traitToApply;
        private SolverTraitDef lastHoveredTrait;
        private bool editMode = false;
        public float defaultX;
        public float defaultY;

        public SolverTraitWindowUtil(Pawn pawn, SolverTraitProgress progress)
        {
            this.pawn = pawn;
            this.progress = progress;
        }

        public void OpenWindow()
        {
            Find.WindowStack.Add(new SolverTraitWindow(this));
        }

        private class SolverTraitWindow : Window
        {
            private SolverTraitWindowUtil util;
            public override Vector2 InitialSize => util.windowSize;

            public SolverTraitWindow(SolverTraitWindowUtil util)
            {
                this.util = util;
                forcePause = true;
                absorbInputAroundWindow = true;
                draggable = true;
                doCloseX = true;
                closeOnClickedOutside = true;
            }

            public override void DoWindowContents(Rect inRect)
            {
                util.DrawTraitWindow(inRect);
            }
        }

        public void DrawTraitWindow(Rect inRect)
        {
            EnsureDefaultUnlockedTraits();

            // Themed background and border
            Rect windowRect = inRect;
            Color prevColor = GUI.color;
            GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.98f); // Deep blue-black
            Widgets.DrawBoxSolid(windowRect, GUI.color);
            GUI.color = new Color(0.7f, 0.95f, 1f, 0.18f); // Neon blue border
            Widgets.DrawBox(windowRect, 4);
            GUI.color = prevColor;

            // Themed header
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.7f, 0.95f, 1f, 1f);
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 40), "ABSOLUTE SOLVER CONTROL");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // Themed skill points display
            float skillY = inRect.y + 40;
            float skillX = inRect.x + 10;
            float iconSize = 32f; // Change this to scale the icon (e.g., 24f, 32f, 40f)
            float textOffset = iconSize + 10f;
            float labelHeight = Mathf.Max(iconSize, 36f); // Ensure enough height for text

            // Optional: Use a custom icon, fallback to a colored circle
            Texture2D skillIcon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmo/Icon_SolverSpinning", false);
            if (skillIcon != null)
            {
                GUI.DrawTexture(new Rect(skillX, skillY, iconSize, iconSize), skillIcon, ScaleMode.ScaleToFit, true);
            }
            else
            {
                // Draw a glowing yellow circle if no icon
                Color prev = GUI.color;
                GUI.color = new Color(1f, 0.95f, 0.3f, 0.85f);
                Widgets.DrawBoxSolid(new Rect(skillX, skillY, iconSize, iconSize), GUI.color);
                GUI.color = prev;
            }

            Text.Font = GameFont.Medium;
            GUI.color = new Color(1f, 0.95f, 0.3f, 1f); // Neon yellow
            Widgets.Label(new Rect(skillX + textOffset, skillY + (iconSize - 32f) / 2f, 220, labelHeight), $"Skill Points: {progress.unspentSkillPoints}");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;



            // Plus/Minus buttons for skill points in edit mode
            if (Prefs.DevMode && editMode)
            {
                float btnSize = 24f;
                float y = inRect.y + 40;
                float xPlus = inRect.x + 220;
                float xMinus = inRect.x + 250;

                if (Widgets.ButtonText(new Rect(xPlus, y, btnSize, btnSize), "+"))
                {
                    progress.unspentSkillPoints++;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                }
                if (Widgets.ButtonText(new Rect(xMinus, y, btnSize, btnSize), "-"))
                {
                    progress.unspentSkillPoints = Mathf.Max(0, progress.unspentSkillPoints - 1);
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }

            // Only show in DevMode
            if (Prefs.DevMode)
            {
                float btnW = 60f;
                float btnH = 24f;
                float pad = 8f;
                Rect editBtnRect = new Rect(inRect.xMax - btnW - pad, inRect.y + pad, btnW, btnH);
                if (Widgets.ButtonText(editBtnRect, "EDIT"))
                {
                    editMode = !editMode;
                }

                // Draw X or checkmark next to the button
                Rect iconRect = new Rect(editBtnRect.x - btnH - 4f, editBtnRect.y, btnH, btnH);
                Texture2D icon = editMode ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
                GUI.DrawTexture(iconRect, icon);
            }

            // After drawing skill points label
            if (Prefs.DevMode && editMode && selectedTrait != null)
            {
                Event e = Event.current;
                if (e.type == EventType.KeyDown && GUI.GetNameOfFocusedControl() == string.Empty)
                {
                    float step = 0.5f;
                    if (e.shift) step = 2f;
                    if (e.control) step = 0.1f;

                    bool moved = false;
                    if (e.keyCode == KeyCode.LeftArrow)
                    {
                        selectedTrait.defaultX -= step;
                        moved = true;
                    }
                    else if (e.keyCode == KeyCode.RightArrow)
                    {
                        selectedTrait.defaultX += step;
                        moved = true;
                    }
                    else if (e.keyCode == KeyCode.UpArrow)
                    {
                        selectedTrait.defaultY -= step;
                        moved = true;
                    }
                    else if (e.keyCode == KeyCode.DownArrow)
                    {
                        selectedTrait.defaultY += step;
                        moved = true;
                    }
                    if (moved)
                    {
                        selectedTrait.defaultX = Mathf.Round(selectedTrait.defaultX * 2f) / 2f;
                        selectedTrait.defaultY = Mathf.Round(selectedTrait.defaultY * 2f) / 2f;
                        e.Use();
                    }
                }
            }

            // Scrollable area
            float contentWidth = 0f, contentHeight = 0f;
            foreach (var def in DefDatabase<SolverTraitDef>.AllDefsListForReading)
            {
                float x = def.defaultX * gridCellSize + gridPadding;
                float y = def.defaultY * gridCellSize + gridPadding;
                contentWidth = Mathf.Max(contentWidth, x + def.size * gridCellSize);
                contentHeight = Mathf.Max(contentHeight, y + def.size * gridCellSize);
            }
            Rect scrollOutRect = new Rect(inRect.x, inRect.y + 70, inRect.width, inRect.height - 80);
            Rect scrollViewRect = new Rect(0, 0, contentWidth + gridPadding * 2, contentHeight + gridPadding * 2);

            Widgets.BeginScrollView(scrollOutRect, ref scrollPos, scrollViewRect);

            // Draw lines between traits and their requirements
            foreach (var def in DefDatabase<SolverTraitDef>.AllDefsListForReading)
            {
                if (def.requiredSolverTraits != null)
                {
                    foreach (var req in def.requiredSolverTraits)
                    {
                        DrawLineBetweenTraits(def, req);
                    }
                }
            }

            // Draw trait nodes
            foreach (var def in DefDatabase<SolverTraitDef>.AllDefsListForReading)
            {
                DrawTraitNode(def);
            }

            // Deselect trait if clicking outside all nodes, the info window rect, and the apply button
            if (selectedTrait != null && Event.current.type == EventType.MouseDown && Event.current.button == 1)
            {
                // Info window rect
                float infoWidth = 320f;
                Rect infoRect = new Rect(inRect.xMax - infoWidth - 12, inRect.y + 70, infoWidth, inRect.height - 80);

                // Check if click is inside any node
                bool insideNode = false;
                foreach (var def in DefDatabase<SolverTraitDef>.AllDefsListForReading)
                {
                    Vector2 pos = new Vector2(def.defaultX * gridCellSize + gridPadding, def.defaultY * gridCellSize + gridPadding);
                    Rect nodeRect = new Rect(pos.x, pos.y, def.size * gridCellSize, def.size * gridCellSize);
                    if (nodeRect.Contains(Event.current.mousePosition))
                    {
                        insideNode = true;
                        break;
                    }
                }

                // Check if click is inside the apply button
                bool insideApplyButton = false;
                if (selectedTrait != null)
                {
                    float x = infoRect.x + 10;
                    float w = infoRect.width - 20;
                    float btnHeight = 32f;
                    Rect btnRect = new Rect(x, infoRect.yMax - btnHeight - 10, w, btnHeight);
                    if (btnRect.Contains(Event.current.mousePosition))
                    {
                        insideApplyButton = true;
                    }
                }

                // If not inside any node, not inside info window, and not inside apply button, deselect
                if (!insideNode && !infoRect.Contains(Event.current.mousePosition) && !insideApplyButton)
                {
                    selectedTrait = null;
                    traitToApply = null;
                    Event.current.Use();
                }
            }


            Widgets.EndScrollView();

            // Draw selected trait info
            if (selectedTrait != null)
            {
                float infoWidth = 320f;
                Rect infoRect = new Rect(inRect.xMax - infoWidth - 12, inRect.y + 70, infoWidth, inRect.height - 80);
                // Themed info panel
                Color prevInfoColor = GUI.color;
                GUI.color = new Color(0.12f, 0.16f, 0.22f, 0.98f);
                Widgets.DrawBoxSolid(infoRect, GUI.color);
                GUI.color = new Color(0.7f, 0.95f, 1f, 0.18f);
                Widgets.DrawBox(infoRect, 2);
                GUI.color = prevInfoColor;

                float y = infoRect.y + 10;
                float x = infoRect.x + 10;
                float w = infoRect.width - 20;

                // Label (dynamic height)
                Text.Font = GameFont.Medium;
                GUI.color = new Color(0.7f, 0.95f, 1f, 1f);
                string label = selectedTrait.label ?? "";
                float labelHeight2 = Text.CalcHeight(label, w);
                Widgets.Label(new Rect(x, y, w, labelHeight2), label);
                y += labelHeight2 + 6f;
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                // Description (dynamic height)
                string desc = selectedTrait.description ?? "";
                float descHeight = Text.CalcHeight(desc, w);
                Widgets.Label(new Rect(x, y, w, descHeight), desc);
                y += descHeight + 6f;

                // Extra tooltip (dynamic height)
                if (!string.IsNullOrEmpty(selectedTrait.tooltipExtra))
                {
                    string extra = selectedTrait.tooltipExtra;
                    float extraHeight = Text.CalcHeight(extra, w);
                    Widgets.Label(new Rect(x, y, w, extraHeight), extra);
                    y += extraHeight + 6f;
                }

                // Stat Offsets
                if (selectedTrait.statOffsets != null && selectedTrait.statOffsets.Count > 0)
                {
                    GUI.color = new Color(0.7f, 0.95f, 1f, 1f);
                    string offsetsHeader = "Stat Offsets:";
                    float offsetsHeaderHeight = Text.CalcHeight(offsetsHeader, w);
                    Widgets.Label(new Rect(x, y, w, offsetsHeaderHeight), offsetsHeader);
                    y += offsetsHeaderHeight + 2f;
                    GUI.color = Color.white;
                    foreach (var mod in selectedTrait.statOffsets)
                    {
                        string valueStr = mod.stat.Worker.ValueToString(mod.value, false, ToStringNumberSense.Offset);
                        string line = $"{mod.stat.LabelCap}: {valueStr}";
                        float lineHeight = Text.CalcHeight(line, w - 10f);
                        Widgets.Label(new Rect(x + 10, y, w - 10f, lineHeight), line);
                        y += lineHeight + 2f;
                    }
                    y += 2f;
                }

                // Stat Factors
                if (selectedTrait.statFactors != null && selectedTrait.statFactors.Count > 0)
                {
                    GUI.color = new Color(0.7f, 0.95f, 1f, 1f);
                    string factorsHeader = "Stat Factors:";
                    float factorsHeaderHeight = Text.CalcHeight(factorsHeader, w);
                    Widgets.Label(new Rect(x, y, w, factorsHeaderHeight), factorsHeader);
                    y += factorsHeaderHeight + 2f;
                    GUI.color = Color.white;
                    foreach (var mod in selectedTrait.statFactors)
                    {
                        string valueStr = mod.stat.Worker.ValueToString(mod.value, false, ToStringNumberSense.Factor);
                        string line = $"{mod.stat.LabelCap}: {valueStr}";
                        float lineHeight = Text.CalcHeight(line, w - 10f);
                        Widgets.Label(new Rect(x + 10, y, w - 10f, lineHeight), line);
                        y += lineHeight + 2f;
                    }
                    y += 2f;
                }

                // Ability
                if (selectedTrait.GivenAbility != null)
                {
                    GUI.color = new Color(0.7f, 0.95f, 1f, 1f);
                    string abilityHeader = "Grants Ability:";
                    float abilityHeaderHeight = Text.CalcHeight(abilityHeader, w);
                    Widgets.Label(new Rect(x, y, w, abilityHeaderHeight), abilityHeader);
                    y += abilityHeaderHeight + 2f;
                    GUI.color = Color.white;
                    string abilityLabel = selectedTrait.GivenAbility.label;
                    float abilityLabelHeight = Text.CalcHeight(abilityLabel, w - 10f);
                    Widgets.Label(new Rect(x + 10, y, w - 10f, abilityLabelHeight), abilityLabel);
                    y += abilityLabelHeight + 2f;
                    if (!string.IsNullOrEmpty(selectedTrait.GivenAbility.description))
                    {
                        string abilityDesc = selectedTrait.GivenAbility.description;
                        float abilityDescHeight = Text.CalcHeight(abilityDesc, w - 10f);
                        Widgets.Label(new Rect(x + 10, y, w - 10f, abilityDescHeight), abilityDesc);
                        y += abilityDescHeight + 2f;
                    }
                    y += 2f;
                }

                // Hediff
                if (selectedTrait.GivenHediff != null)
                {
                    GUI.color = new Color(0.7f, 0.95f, 1f, 1f);
                    string hediffHeader = "Grants Hediff:";
                    float hediffHeaderHeight = Text.CalcHeight(hediffHeader, w);
                    Widgets.Label(new Rect(x, y, w, hediffHeaderHeight), hediffHeader);
                    y += hediffHeaderHeight + 2f;
                    GUI.color = Color.white;
                    string hediffLabel = selectedTrait.GivenHediff.label;
                    float hediffLabelHeight = Text.CalcHeight(hediffLabel, w - 10f);
                    Widgets.Label(new Rect(x + 10, y, w - 10f, hediffLabelHeight), hediffLabel);
                    y += hediffLabelHeight + 2f;
                    if (!string.IsNullOrEmpty(selectedTrait.GivenHediff.description))
                    {
                        string hediffDesc = selectedTrait.GivenHediff.description;
                        float hediffDescHeight = Text.CalcHeight(hediffDesc, w - 10f);
                        Widgets.Label(new Rect(x + 10, y, w - 10f, hediffDescHeight), hediffDesc);
                        y += hediffDescHeight + 2f;
                    }
                    y += 2f;
                }


                // Themed Apply button
                bool canUnlock = CanUnlock(selectedTrait);
                bool alreadyUnlocked = progress.unlockedTraits.Contains(selectedTrait.defName);
                string buttonLabel = alreadyUnlocked ? "Unlocked" : canUnlock ? "Apply Trait" : "Cannot Unlock";
                float btnHeight = 32f;

                // Display point cost above the apply button only if cost > 0
                if (selectedTrait.skillPointCost > 0)
                {
                    Text.Font = GameFont.Tiny;
                    GUI.color = new Color(1f, 0.95f, 0.3f, 0.85f); // Neon yellow
                    string costText = $"Point Cost: {selectedTrait.skillPointCost}";
                    float costHeight = Text.CalcHeight(costText, w);
                    Rect costRect = new Rect(x, infoRect.yMax - btnHeight - 10 - costHeight - 2f, w, costHeight);
                    Widgets.Label(costRect, costText);
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }


                // Now draw the button below the cost
                Rect btnRect = new Rect(x, infoRect.yMax - btnHeight - 10, w, btnHeight);

                GUI.color = alreadyUnlocked ? Color.gray :
                    canUnlock ? new Color(0.7f, 0.95f, 1f, 1f) : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                if (Widgets.ButtonText(btnRect, buttonLabel))
                {
                    if (canUnlock && !alreadyUnlocked)
                    {
                        progress.unspentSkillPoints -= selectedTrait.skillPointCost;
                        SolverTraitEffectManager.AddSolverTrait(pawn, progress, selectedTrait);

                        SoundDef sound = selectedTrait.unlockSound ?? SoundDefOf.Tick_High;
                        sound.PlayOneShotOnCamera();
                        Messages.Message($"Unlocked trait: {selectedTrait.label}", MessageTypeDefOf.TaskCompletion, false);
                    }

                    else
                    {
                        SoundDefOf.ClickReject.PlayOneShotOnCamera();
                    }
                }
                GUI.color = Color.white;

                // Draw "X" button for removing trait in edit mode
                if (Prefs.DevMode && editMode && alreadyUnlocked)
                {
                    float xBtnSize = 20f;
                    Rect xBtnRect = new Rect(btnRect.xMax - xBtnSize - 2f, btnRect.y + 2f, xBtnSize, xBtnSize);
                    if (Widgets.ButtonImage(xBtnRect, TexButton.CloseXSmall))
                    {
                        SolverTraitEffectManager.RemoveSolverTrait(pawn, progress, selectedTrait);
                        SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                        Messages.Message($"Removed trait: {selectedTrait.label}", MessageTypeDefOf.TaskCompletion, false);
                    }
                    TooltipHandler.TipRegion(xBtnRect, "Remove this trait (edit mode only)");
                }

                if (!canUnlock && !alreadyUnlocked)
                {
                    TooltipHandler.TipRegion(btnRect, GetUnlockFailReason(selectedTrait));
                }
            }
        }

        private string GetUnlockFailReason(SolverTraitDef def)
        {
            if (progress.unlockedTraits.Contains(def.defName))
                return "Already unlocked.";
            if (progress.unspentSkillPoints < def.skillPointCost)
                return $"Not enough skill points (need {def.skillPointCost}, have {progress.unspentSkillPoints}).";
            if (def.requiredTierLevel > 0)
            {
                int highestTier = progress.unlockedTraits
                    .Select(n => DefDatabase<SolverTraitDef>.GetNamedSilentFail(n))
                    .Where(d => d != null)
                    .Select(d => d.tierLevel)
                    .DefaultIfEmpty(0)
                    .Max();
                if (highestTier < def.requiredTierLevel)
                    return $"Requires tier {def.requiredTierLevel} (current: {highestTier}).";
            }
            if (def.requiredSolverTraits != null && def.requiredSolverTraits.Count > 0)
            {
                if (def.requireOnlyOneTrait)
                {
                    bool anyUnlocked = def.requiredSolverTraits.Any(req => req != null && progress.unlockedTraits.Contains(req.defName));
                    if (!anyUnlocked)
                        return $"Requires at least one of: {string.Join(", ", def.requiredSolverTraits.Where(r => r != null).Select(r => r.label ?? r.defName))}";
                }
                else
                {
                    foreach (var req in def.requiredSolverTraits)
                    {
                        if (req == null || !progress.unlockedTraits.Contains(req.defName))
                            return $"Requires trait: {req?.label ?? req?.defName ?? "Unknown"}";
                    }
                }
            }
            if (def.CoreModule)
            {
                bool alreadyCoreInTier = progress.unlockedTraits
                    .Select(n => DefDatabase<SolverTraitDef>.GetNamedSilentFail(n))
                    .Where(d => d != null && d.CoreModule && d.tierLevel == def.tierLevel)
                    .Any();
                if (alreadyCoreInTier)
                    return $"Only one core module can be selected per tier (tier {def.tierLevel}).";
            }
            return "Unknown reason.";
        }

        private void DrawTraitNode(SolverTraitDef def)
        {
            Vector2 pos = new Vector2(def.defaultX * gridCellSize + gridPadding, def.defaultY * gridCellSize + gridPadding);
            Rect nodeRect = new Rect(pos.x, pos.y, def.size * gridCellSize, def.size * gridCellSize);

            // Themed node color with override support
            Color nodeColor;
            bool hasCustomColor = !string.IsNullOrEmpty(def.color);
            if (hasCustomColor)
            {
                nodeColor = ParseColor(def.color, Color.white);
            }
            else
            {
                // Use category color if no custom color
                if (def.solverCategory == SolverCategory.Corruption)
                    nodeColor = new Color(1f, 0.9f, 0.2f); // Solver yellow
                else if (def.solverCategory == SolverCategory.Mutation)
                    nodeColor = new Color(0.2f, 1f, 0.4f); // Green
                else // Absolute or fallback
                    nodeColor = new Color(0.7f, 0.95f, 1f); // Neon cyan
            }


            bool unlocked = progress.unlockedTraits.Contains(def.defName);
            bool canUnlock = CanUnlock(def);

            // Always use the category color for the background, except for locked+unavailable
            if (!unlocked && !canUnlock)
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // grayed out
            else
                GUI.color = nodeColor;


            Widgets.DrawBoxSolid(nodeRect, GUI.color * new Color(1, 1, 1, 0.18f));
            GUI.color = Color.white;

            // Icon (with fallback to RimWorld's "no texture" if missing, and no error spam)
            Texture2D icon = null;
            if (!string.IsNullOrEmpty(def.iconPath))
            {
                icon = ContentFinder<Texture2D>.Get(def.iconPath, false); // Don't report failure
            }
            if (icon == null)
            {
                // Use RimWorld's built-in "no texture" placeholder
                icon = BaseContent.BadTex;
            }
            if (icon != null)
            {
                Color iconTint;
                if (!unlocked && !canUnlock)
                    iconTint = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                else
                    iconTint = nodeColor; // Use nodeColor for tinting

                float iconPad = 8f;
                float iconSize = nodeRect.width - 2 * iconPad;
                float maxIconSize = iconSize * 1.4142f; // sqrt(2) for diagonal fit
                Rect iconRect = new Rect(
                    nodeRect.x + (nodeRect.width - maxIconSize) / 2f,
                    nodeRect.y + (nodeRect.height - maxIconSize) / 2f,
                    maxIconSize,
                    maxIconSize
                );

                // Draw a background box to mask the icon (optional, for a clean look)
                Color prevIconColor = GUI.color;
                GUI.color = nodeColor * new Color(1, 1, 1, 0.18f);
                Widgets.DrawBoxSolid(nodeRect, GUI.color);
                GUI.color = prevIconColor;

                // Draw the spinning icon
                if (def.GlowingIcon)
                {
                    float t = 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 2.5f);
                    Color glow = Color.Lerp(iconTint, new Color(0.7f, 0.95f, 1f, 1f), t);
                    GUI.color = glow;
                    float iconPad2 = 8f;
                    float size = nodeRect.width - 2 * iconPad2;
                    Rect iconRect2 = new Rect(
                        nodeRect.x + (nodeRect.width - size) / 2f,
                        nodeRect.y + (nodeRect.height - size) / 2f,
                        size,
                        size
                    );
                    GUI.DrawTexture(iconRect2, icon);
                    GUI.color = Color.white;
                }
                else
                {
                    // Draw the icon normally, centered, with tint
                    float normalIconSize = nodeRect.width - 2 * iconPad;
                    Rect normalIconRect = new Rect(
                        nodeRect.x + (nodeRect.width - normalIconSize) / 2f,
                        nodeRect.y + (nodeRect.height - normalIconSize) / 2f,
                        normalIconSize,
                        normalIconSize
                    );
                    GUI.color = iconTint;
                    GUI.DrawTexture(normalIconRect, icon);
                    GUI.color = Color.white;
                }
            }




            // Core module visual indicator
            if (def.CoreModule)
            {
                float badgeSize = 20f;
                float badgePad = 4f;
                Rect badgeRect = new Rect(nodeRect.xMax - badgeSize - badgePad, nodeRect.y + badgePad, badgeSize, badgeSize);

                // Try to use a custom icon, fallback to a yellow diamond
                Texture2D coreIcon = ContentFinder<Texture2D>.Get("UI/Icons/CoreModule", false);
                if (coreIcon != null)
                {
                    GUI.DrawTexture(badgeRect, coreIcon, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    // Draw a yellow diamond as fallback
                    Vector2 center = new Vector2(badgeRect.x + badgeRect.width / 2f, badgeRect.y + badgeRect.height / 2f);
                    float half = badgeRect.width / 2f;
                    Vector3[] diamond = new Vector3[]
                    {
            new Vector3(center.x, center.y - half, 0),
            new Vector3(center.x + half, center.y, 0),
            new Vector3(center.x, center.y + half, 0),
            new Vector3(center.x - half, center.y, 0)
                    };
                    Color prev = GUI.color;
                    GUI.color = new Color(1f, 0.95f, 0.3f, 0.95f); // Solver yellow
                    Widgets.DrawLine(diamond[0], diamond[1], GUI.color, 3f);
                    Widgets.DrawLine(diamond[1], diamond[2], GUI.color, 3f);
                    Widgets.DrawLine(diamond[2], diamond[3], GUI.color, 3f);
                    Widgets.DrawLine(diamond[3], diamond[0], GUI.color, 3f);
                    GUI.color = prev;
                }
                TooltipHandler.TipRegion(badgeRect, "Core Module");
            }


            if (editMode && selectedTrait == def)
            {
                float btnSize = 24f;
                float pad = 4f;
                float y = nodeRect.y - btnSize - pad;
                float x = nodeRect.x + nodeRect.width - btnSize - pad;
                Rect copyBtnRect = new Rect(x, y, btnSize, btnSize);
                if (Widgets.ButtonImage(copyBtnRect, TexButton.Copy))
                {
                    GUIUtility.systemCopyBuffer = $"X: {def.defaultX}, Y: {def.defaultY}";
                    Messages.Message($"Copied: X={def.defaultX}, Y={def.defaultY}", MessageTypeDefOf.TaskCompletion, false);
                }
                TooltipHandler.TipRegion(copyBtnRect, "Copy trait grid coordinates to clipboard");

                float hintY = nodeRect.y - 18f;
                float hintX = nodeRect.x;
                Rect hintRect = new Rect(hintX, hintY, nodeRect.width, 16f);
                Text.Anchor = TextAnchor.UpperCenter;
                GUI.color = new Color(1f, 1f, 1f, 0.7f);
                Widgets.Label(hintRect, "←↑↓→ to move (Shift=fast, Ctrl=fine)");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            // Only show label box and text if node size is 1 or greater
            if (def.size >= 1f)
            {
                string labelText = def.label ?? "";
                float labelPad = 4f;
                float labelBoxHeight = 36f; // Enough for two lines
                float labelBoxWidth = nodeRect.width - 2 * labelPad;
                Rect labelBoxRect = new Rect(
                    nodeRect.x + labelPad,
                    nodeRect.y + nodeRect.height - labelBoxHeight - labelPad,
                    labelBoxWidth,
                    labelBoxHeight
                );

                // Themed box color: nodeColor, but mostly transparent
                Color boxColor = nodeColor;
                boxColor.a = 0.65f;
                Color prev2 = GUI.color;
                GUI.color = boxColor;
                Widgets.DrawBoxSolid(labelBoxRect, GUI.color);
                GUI.color = prev2;

                // Draw the label, word-wrapped and centered
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                Widgets.Label(labelBoxRect, labelText);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            TooltipHandler.TipRegion(nodeRect, def.description);

            // Double-click to select/deselect for application
            Event e = Event.current;
            if (nodeRect.Contains(e.mousePosition))
            {
                if (nodeRect.Contains(e.mousePosition))
                {
                    if (lastHoveredTrait != def)
                    {
                        SoundDefOf.Mouseover_Standard.PlayOneShotOnCamera();
                        lastHoveredTrait = def;
                    }
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        if (selectedTrait != def)
                        {
                            selectedTrait = def;
                            SoundDefOf.Tick_High.PlayOneShotOnCamera(); // Select sound
                        }
                        else
                        {
                            selectedTrait = null;
                            SoundDefOf.Tick_Low.PlayOneShotOnCamera(); // Deselect sound
                        }
                        e.Use();
                    }
                }
            }


            // Draw border for unlocked/selected
            if (unlocked)
            {
                Color prev = GUI.color;
                GUI.color = Color.green;
                Widgets.DrawBox(nodeRect, 3);
                GUI.color = prev;
            }
            if (selectedTrait == def)
            {
                Color prev = GUI.color;
                GUI.color = new Color(0.3f, 0.8f, 1f, 0.7f);
                Widgets.DrawBox(nodeRect, 2);
                GUI.color = prev;
            }
            if (traitToApply == def)
            {
                Color prev = GUI.color;
                GUI.color = Color.yellow;
                Widgets.DrawBox(nodeRect, 2);
                GUI.color = prev;
            }
        }

        private void DrawLineBetweenTraits(SolverTraitDef from, SolverTraitDef to)
        {
            Vector2 fromPos = new Vector2(from.defaultX * gridCellSize + gridPadding + from.size * gridCellSize / 2f,
                                          from.defaultY * gridCellSize + gridPadding + from.size * gridCellSize / 2f);
            Vector2 toPos = new Vector2(to.defaultX * gridCellSize + gridPadding + to.size * gridCellSize / 2f,
                                        to.defaultY * gridCellSize + gridPadding + to.size * gridCellSize / 2f);

            // Themed neon blue lines
            Widgets.DrawLine(fromPos, toPos, new Color(0.7f, 0.95f, 1f, 0.5f), 3f);
        }

        private bool CanUnlock(SolverTraitDef def)
        {
            if (progress.unlockedTraits.Contains(def.defName))
                return false;
            if (progress.unspentSkillPoints < def.skillPointCost)
                return false;
            if (def.requiredTierLevel > 0)
            {
                int highestTier = progress.unlockedTraits
                    .Select(n => DefDatabase<SolverTraitDef>.GetNamedSilentFail(n))
                    .Where(d => d != null)
                    .Select(d => d.tierLevel)
                    .DefaultIfEmpty(0)
                    .Max();
                if (highestTier < def.requiredTierLevel)
                    return false;
            }
            if (def.requiredSolverTraits != null && def.requiredSolverTraits.Count > 0)
            {
                if (def.requireOnlyOneTrait)
                {
                    // At least one required trait must be unlocked
                    bool anyUnlocked = def.requiredSolverTraits.Any(req => req != null && progress.unlockedTraits.Contains(req.defName));
                    if (!anyUnlocked)
                        return false;
                }
                else
                {
                    // All required traits must be unlocked
                    foreach (var req in def.requiredSolverTraits)
                    {
                        if (req == null || !progress.unlockedTraits.Contains(req.defName))
                            return false;
                    }
                }
            }
            // Enforce only one core part per tier
            if (def.CoreModule)
            {
                bool alreadyCoreInTier = progress.unlockedTraits
                    .Select(n => DefDatabase<SolverTraitDef>.GetNamedSilentFail(n))
                    .Where(d => d != null && d.CoreModule && d.tierLevel == def.tierLevel)
                    .Any();
                if (alreadyCoreInTier)
                    return false;
            }
            return true;
        }

        private void EnsureDefaultUnlockedTraits()
        {
            foreach (var def in DefDatabase<SolverTraitDef>.AllDefsListForReading)
            {
                if (def.DefaultUnlocked && !progress.unlockedTraits.Contains(def.defName))
                {
                    progress.unlockedTraits.Add(def.defName);
                }
            }
        }


        private static Color ParseColor(string colorString, Color fallback)
        {
            if (string.IsNullOrEmpty(colorString))
                return fallback;

            // Try "R,G,B" format
            var parts = colorString.Split(',');
            if (parts.Length == 3)
            {
                if (byte.TryParse(parts[0], out byte r) &&
                    byte.TryParse(parts[1], out byte g) &&
                    byte.TryParse(parts[2], out byte b))
                {
                    return new Color(r / 255f, g / 255f, b / 255f, 1f);
                }
            }

            // Fallback to HTML color (e.g. "#RRGGBB")
            Color color;
            if (ColorUtility.TryParseHtmlString(colorString, out color))
                return color;

            return fallback;
        }
    }
}

