using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WorkerDronesMod
{
    [StaticConstructorOnStartup]
    public class GeneGizmo_NeutroamineOil : GeneGizmo_ResourceHemogen
    {
        // =====================================================
        // Custom Oil Bar Texture Replacement Settings
        // =====================================================
        // Offsets and scaling for the overall oil bar (which is the gizmo’s zone).
        public static float OilBarXOffset = 5f;      // X offset for the gizmo (scaled)
        public static float OilBarYOffset = -5f;     // Y offset for the gizmo (scaled)
        public static float OilBarScale = 2.5f;      // Scale factor for the gizmo display
        private const float OilBarWidth = 40f;        // Unscaled overall bar width
        private const float OilBarHeight = 40f;       // Unscaled overall bar height

        // Texture paths for our oil fill bar and the overlay icon.
        public static Texture2D OilBarTex = ContentFinder<Texture2D>.Get("UI/Icons/Gizmo/OilBar/Bar/OilGizmoBar", false);
        public static Texture2D OilOverlayTex = ContentFinder<Texture2D>.Get("UI/Icons/Gizmo/OilBar/OilGizmoIcon", false);
        // New: Efficiency bonus icon texture (using your provided settings).
        public static Texture2D EfficiencyBonusIcon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmo/Effects/UI_EfficiencyBonusEffect", false);
        // New: Editable fields for the efficiency bonus icon's position offset (relative to barRect) and size.
        public static Vector2 EfficiencyBonusIconOffset = new Vector2(10, 20); // Use your existing offset settings.
        public static float EfficiencyBonusIconSize = 30f; // Use your existing icon size.

        // Texture for the auto-shelter checkbox background.
        private static readonly Texture2D AutoShelterBackgroundTex = ContentFinder<Texture2D>.Get("UI/Designators/BuildRoofArea", false);

        // =====================================================
        // Editable Offsets for Interactive Elements
        // =====================================================
        public static Vector2 SliderOffset = new Vector2(-78f, -5f); // Relative to gizmo’s center.
        public static Vector2 OilAllowedCheckboxOffset = new Vector2(-10f, 10f); // Relative to oil bar’s top-left.
        public static Vector2 AutoShelterCheckboxOffset = new Vector2(10f, 10f);  // Relative to oil bar’s top-left.
        public static Vector2 NumericTextOffset = new Vector2(50f, 40f);          // Position offset for numeric text.
        public static float NumericTextWidth = 100f;                             // Fixed width for numeric text.

        // =====================================================
        // New settings for vertical slider customization.
        // =====================================================
        public static float HorizontalSliderLength = 65f; // Vertical length.
        public static float HorizontalSliderHeight = 20f; // Thickness (width).

        // =====================================================
        // New: Editable properties for the fillbar.
        // =====================================================
        // These properties let you position and size the fillbar independently.
        public static float OilFillBarXOffset = 20f;      // Horizontal fine‑tuning; the fillbar is otherwise centered on the overlay.
        public static float OilFillBarYOffset = 0f;         // Vertical fine‑tuning.
        public static float OilFillBarWidth = 27f;          // Unscaled fillbar width (adjust as needed).
        public static float OilFillBarHeight = 27f;         // Unscaled fillbar height.

        public GeneGizmo_NeutroamineOil(
            Gene_Resource gene,
            List<IGeneResourceDrain> drainGenes,
            Color barColor,
            Color barHighlightColor)
            : base(gene, drainGenes ?? new List<IGeneResourceDrain>(), barColor, barHighlightColor)
        {
            // Always start with the target value set to 50.
            gene.targetValue = 50f;
        }

        protected override IEnumerable<float> GetBarThresholds()
        {
            if (drainGenes == null)
                yield break;
            if (gene == null)
                yield break;
            float geneMax = gene.MaxForDisplay;
            if (Mathf.Approximately(geneMax, 0f))
                yield break;
            foreach (IGeneResourceDrain drain in drainGenes)
            {
                if (drain != null)
                    yield return Mathf.Clamp01(Mathf.Abs(drain.ResourceLossPerDay) / geneMax);
            }
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            // Only display the gizmo for colonist/prisoner pawns.
            if (gene.pawn == null || !(gene.pawn.IsColonist || gene.pawn.IsPrisonerOfColony))
                return new GizmoResult(GizmoState.Clear);

            GizmoResult result = new GizmoResult(GizmoState.Clear);

            // ----- 1. Draw the Overall Oil Bar (Gizmo Area) -----
            Rect barRect = new Rect(
                topLeft.x + OilBarXOffset * OilBarScale,
                topLeft.y + OilBarYOffset * OilBarScale,
                OilBarWidth * OilBarScale,
                OilBarHeight * OilBarScale
            );

            float oilPercent = Mathf.Clamp01(gene.Value / gene.Max);
            Color fillColor = new Color(0.25f, 0.25f, 0.25f, 1f);

            if (OilBarTex != null)
                OilBarTex.filterMode = FilterMode.Bilinear;
            if (OilOverlayTex != null)
                OilOverlayTex.filterMode = FilterMode.Bilinear;

            // Default tooltip area.
            Rect textureTooltipArea = barRect;

            if (OilBarTex != null && OilOverlayTex != null)
            {
                // Compute the overlay texture's (icon) rectangle. It is centered in barRect
                // using the overlay's native aspect ratio.
                float aspect = (float)OilOverlayTex.width / OilOverlayTex.height;
                float texHeight = barRect.height;
                float texWidth = texHeight * aspect;
                float textureX = barRect.x + (barRect.width - texWidth) / 2f;
                Rect texRect = new Rect(textureX, barRect.y, texWidth, texHeight);
                textureTooltipArea = texRect;

                // Compute the fillbar rectangle to be centered within the overlay's clear center.
                float fillBarScaledWidth = OilFillBarWidth * OilBarScale;
                float fillBarScaledHeight = OilFillBarHeight * OilBarScale;
                float fillBarX = texRect.x + (texRect.width - fillBarScaledWidth) / 2f + OilFillBarXOffset;
                float fillBarY = texRect.y + (texRect.height - fillBarScaledHeight) / 2f + OilFillBarYOffset;
                Rect fillBarRect = new Rect(fillBarX, fillBarY, fillBarScaledWidth, fillBarScaledHeight);

                // Draw the fill portion of the fillbar from bottom up.
                Rect fillDestRect = new Rect(
                    fillBarRect.x,
                    fillBarRect.y + fillBarRect.height * (1 - oilPercent),
                    fillBarRect.width,
                    fillBarRect.height * oilPercent
                );
                Rect fillSourceRect = new Rect(0f, 0f, 1f, oilPercent);
                Material transparentMat = new Material(ShaderDatabase.Transparent);
                Graphics.DrawTexture(fillDestRect, OilBarTex, fillSourceRect, 0, 0, 0, 0, fillColor, transparentMat);

                // Draw the overlay icon on top (fully opaque).
                Color savedColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 1f);
                GUI.DrawTexture(texRect, OilOverlayTex);
                GUI.color = savedColor;

                // Highlight only the overlay texture when hovered.
                if (Mouse.IsOver(texRect))
                {
                    Widgets.DrawHighlight(texRect);
                }
            }
            else
            {
                // Fallback if textures are missing.
                Widgets.DrawBoxSolid(barRect, new Color(0.1f, 0.1f, 0.1f, 0.8f));
                Rect fallbackFill = barRect.ContractedBy(2f);
                fallbackFill.width *= oilPercent;
                Widgets.DrawBoxSolid(fallbackFill, fillColor);
            }

            // ----- 2. Draw the Numeric Scale Overlay -----
            int currentPercentage = Mathf.RoundToInt(oilPercent * 100f);
            string scaleText = $"{currentPercentage}/100";
            Rect scaleTextRect = new Rect(
                barRect.x + NumericTextOffset.x,
                barRect.y + NumericTextOffset.y,
                NumericTextWidth,
                barRect.height
            );
            GameFont savedFont = Text.Font;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.Label(scaleTextRect, scaleText);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = savedFont;

            // ----- 3. Draw the Vertical Slider (Inverted) -----
            float sliderWidth = HorizontalSliderHeight; // slider thickness.
            float sliderHeight = HorizontalSliderLength;  // slider vertical length.
            float baseSliderX = barRect.center.x - sliderWidth * 0.5f;
            float baseSliderY = barRect.center.y - sliderHeight * 0.5f;
            Rect sliderRect = new Rect(
                baseSliderX + SliderOffset.x,
                baseSliderY + SliderOffset.y,
                sliderWidth,
                sliderHeight
            );
            float newTargetValue = DrawVerticalSlider(sliderRect, gene.targetValue, 0f, gene.MaxForDisplay);
            if (!Mathf.Approximately(newTargetValue, gene.targetValue))
            {
                gene.targetValue = newTargetValue;
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
            }
            TooltipHandler.TipRegion(sliderRect, "MD.OilTargetValue".Translate(gene.targetValue.ToString("F0")));
            // =========================== I stopped translating here! ===========================


            // ----- 4. Draw the Checkboxes (editable positions) -----
            int checkboxSize = 24;
            Rect oilAllowedRect = new Rect(
                barRect.x + OilAllowedCheckboxOffset.x,
                barRect.y + OilAllowedCheckboxOffset.y,
                checkboxSize,
                checkboxSize
            );
            Rect autoShelterRect = new Rect(
                barRect.x + AutoShelterCheckboxOffset.x,
                barRect.y + AutoShelterCheckboxOffset.y,
                checkboxSize,
                checkboxSize
            );

            Gene_NeutroamineOil geneInstance = gene as Gene_NeutroamineOil;
            if (geneInstance != null)
            {
                Texture2D oilTex = ContentFinder<Texture2D>.Get(geneInstance.texPath, false) ?? BaseContent.BadTex;
                if (Widgets.ButtonImage(oilAllowedRect, oilTex))
                {
                    geneInstance.neutroamineAllowed = !geneInstance.neutroamineAllowed;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }
                Texture2D oilAllowedOverlay = geneInstance.neutroamineAllowed ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
                Rect oilAllowedOverlayRect = new Rect(
                    oilAllowedRect.x,
                    oilAllowedRect.y,
                    oilAllowedRect.width * 0.6f,
                    oilAllowedRect.height * 0.6f
                );
                GUI.DrawTexture(oilAllowedOverlayRect, oilAllowedOverlay);
                if (Mouse.IsOver(oilAllowedRect))
                    TooltipHandler.TipRegion(oilAllowedRect, geneInstance.neutroamineAllowed ? "Neutroamine is allowed." : "Neutroamine is disabled.");

                if (AutoShelterBackgroundTex != null)
                    GUI.DrawTexture(autoShelterRect, AutoShelterBackgroundTex);
                if (Widgets.ButtonInvisible(autoShelterRect))
                {
                    geneInstance.autoShelter = !geneInstance.autoShelter;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }
                if (Mouse.IsOver(autoShelterRect))
                    TooltipHandler.TipRegion(autoShelterRect, geneInstance.autoShelter ? "Automatic sheltering is enabled." : "Automatic sheltering is disabled.");
                Rect autoShelterCheckRect = new Rect(
                    autoShelterRect.x,
                    autoShelterRect.y,
                    autoShelterRect.width * 0.6f,
                    autoShelterRect.height * 0.6f
                );
                Texture2D checkmarkTex = geneInstance.autoShelter ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
                GUI.DrawTexture(autoShelterCheckRect, checkmarkTex);
            }

            // ----- 5. Register the Tooltip Region Over the Oil Icon -----
            TooltipHandler.TipRegion(textureTooltipArea, GetTooltip());

            // ----- NEW: Draw Efficiency Bonus Icon if the bonus is active -----
            if (geneInstance != null)
            {
                // Using reflection to get the private efficiency bonus fields.
                FieldInfo bonusTicksField = geneInstance.GetType().GetField("efficiencyBonusTicksRemaining", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo bonusMultField = geneInstance.GetType().GetField("efficiencyBonusMultiplier", BindingFlags.NonPublic | BindingFlags.Instance);
                if (bonusTicksField != null && bonusMultField != null)
                {
                    int bonusTicks = (int)bonusTicksField.GetValue(geneInstance);
                    if (bonusTicks > 0)
                    {
                        // Use the existing settings for icon position offset and size.
                        Rect bonusIconRect = new Rect(
                            barRect.xMax - EfficiencyBonusIconSize + EfficiencyBonusIconOffset.x,
                            barRect.y - EfficiencyBonusIconSize / 2 + EfficiencyBonusIconOffset.y,
                            EfficiencyBonusIconSize,
                            EfficiencyBonusIconSize
                        );
                        // Apply a subtle vertical up-and-down animation with a higher frequency for smoother motion.
                        float verticalOffset = Mathf.Sin(Time.realtimeSinceStartup * 2f) * 2f;
                        Rect animatedBonusIconRect = new Rect(
                            bonusIconRect.x,
                            bonusIconRect.y + verticalOffset,
                            bonusIconRect.width,
                            bonusIconRect.height
                        );
                        GUI.DrawTexture(animatedBonusIconRect, EfficiencyBonusIcon);
                        float bonusMultiplier = (float)bonusMultField.GetValue(geneInstance);
                        TooltipHandler.TipRegion(animatedBonusIconRect, $"Efficiency Bonus Active ({bonusMultiplier}x)");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Draws an inverted vertical slider.
        /// Rotates the GUI context, using a horizontal slider with reversed input/output so that the top corresponds to max.
        /// </summary>
        /// <param name="rect">Rectangle for the slider (in unrotated coordinates).</param>
        /// <param name="value">Current slider value.</param>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <returns>The new value after interaction.</returns>
        private float DrawVerticalSlider(Rect rect, float value, float min, float max)
        {
            // Invert the input value so that the top represents the max.
            float reversedInput = max + min - value;

            Matrix4x4 savedMatrix = GUI.matrix;
            // Rotate the GUI context around the slider's center by 90° clockwise.
            GUIUtility.RotateAroundPivot(90, rect.center);
            // Swap the rectangle's width and height.
            Rect newRect = new Rect(rect.x, rect.y, rect.height, rect.width);
            // Draw the horizontal slider with reversed input.
            float horizontalValue = Widgets.HorizontalSlider(newRect, reversedInput, min, max, false);
            GUI.matrix = savedMatrix;
            // Invert slider output to recover the proper value.
            float newValue = max + min - horizontalValue;
            float step = (max - min) * 0.1f;
            newValue = Mathf.Round(newValue / step) * step;
            return newValue;
        }

        protected override string GetTooltip()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendFormat("<color=orange>{0}</color>: {1} / {2}\n",
                gene.ResourceLabel.CapitalizeFirst(), gene.ValueForDisplay, gene.MaxForDisplay);

            if (gene.pawn != null && (gene.pawn.IsColonistPlayerControlled || gene.pawn.IsPrisonerOfColony))
            {
                Gene_NeutroamineOil geneInstance = gene as Gene_NeutroamineOil;
                if (geneInstance != null)
                {
                    if (geneInstance.TargetValue <= 0f)
                        sb.Append("Never Consume Neutroamine Oil");
                    else
                        sb.Append("Consume Neutroamine Oil Below: " + gene.PostProcessValue(geneInstance.TargetValue));
                }
                else
                {
                    if (gene.targetValue <= 0f)
                        sb.Append("Never Consume Neutroamine Oil");
                    else
                        sb.Append("Consume Neutroamine Oil Below: " + gene.PostProcessValue(geneInstance.TargetValue));
                }
            }

            sb.AppendLine("\n\n<i>Neutroamine Oil</i> is a specialized biofluid essential for cooling internal heat. " +
                "When present, it actively reduces heat buildup in the drone’s systems, helping maintain optimal performance.");
            sb.AppendLine();
            sb.AppendLine("<b>Acquisition Methods:</b>");
            sb.AppendLine("<color=lightblue>• Consuming Neutroamine:</color> " +
                "The primary and most reliable method. When ingested, Neutroamine is rapidly converted by the drone’s internal nanoforge " +
                "into a high-performance coolant. However, it does not provide an efficiency bonus.");
            sb.AppendLine("<color=lightblue>• Consuming corpses of androids/drones:</color> " +
                "An alternative method that grants an efficiency bonus, temporarily boosting heat-reducing efficiency. " +
                "Note that this method yields less oil and takes longer to process compared to consuming neutroamine.");
            sb.AppendLine();
            sb.AppendLine("Maintain your Neutroamine Oil levels for effective cooling and system stability.");

            if (!drainGenes.NullOrEmpty<IGeneResourceDrain>())
            {
                float totalDrain = 0f;
                foreach (IGeneResourceDrain drain in drainGenes)
                {
                    if (drain != null && drain.CanOffset)
                        totalDrain += drain.ResourceLossPerDay;
                }
                if (!Mathf.Approximately(totalDrain, 0f))
                {
                    string drainRateStr = (totalDrain < 0f) ? "RegenerationRate".Translate() : "DrainRate".Translate();
                    sb.AppendFormat("\n\n{0}: {1}", drainRateStr, Mathf.Abs(gene.PostProcessValue(totalDrain)));
                }
            }

            if (!gene.def.resourceDescription.NullOrEmpty())
                sb.Append("\n\n" + gene.def.resourceDescription.Formatted(gene.pawn.Named("PAWN")).Resolve());

            return sb.ToString();
        }
    }
}

























