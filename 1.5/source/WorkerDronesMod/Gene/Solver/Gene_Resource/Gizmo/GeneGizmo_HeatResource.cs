using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WorkerDronesMod
{
    [StaticConstructorOnStartup]
    public class GeneGizmo_HeatResource : GeneGizmo_Resource
    {
        // =====================================================
        // Global positional settings for the whole gizmo:
        // =====================================================
        // Change this value to reposition the entire gizmo.
        public static Vector2 GlobalOffset = new Vector2(-20f, 0f);

        // =====================================================
        // Layout Settings: Independent Adjustments
        // =====================================================

        // ----- Bar Settings -----
        // The bar's position is computed relative to GlobalOffset.
        public static float XOffset = -5f;          // Bar's X offset (will be multiplied by Scale)
        public static float BarYOffset = -5f;        // Bar's Y offset (will be multiplied by Scale)
        public static float Scale = 2.5f;            // Scale factor for the bar
        private const float BarWidth = 40f;         // Unscaled bar width
        private const float BarHeight = 40f;        // Unscaled bar height

        // ----- Temperature Text Settings -----
        // The temperature text is positioned independently relative to GlobalOffset.
        public static float TempTextXOffset = -25f;  // Temperature text's absolute X offset
        public static float TempTextYOffset = 30f;   // Temperature text's absolute Y offset
        private const float TemperatureMin = 30f;
        private const float TemperatureMax = 100f;

        // ----- Spike Icon Settings -----
        // The spike icon is positioned independently relative to GlobalOffset.
        public static float SpikeXOffset = 50f;      // Spike icon's absolute X offset
        public static float SpikeYOffset = 50f;      // Spike icon's absolute Y offset
        public static float SpikeIconBaseSize = 35f;   // Base size for the spike icon
        // New setting: the minimum multiplier for the spike icon size at low heat.
        public static float SpikeIconMinMultiplier = 0.8f;

        // ----- Checkbox Settings -----
        // The checkbox is positioned completely independently.
        public static float CheckboxXOffset = 5f;      // Checkbox's absolute X offset
        public static float CheckboxYOffset = 8f;      // Checkbox's absolute Y offset
        public static float CheckboxSize = 30f;        // Adjustable checkbox size

        // ----- Heat Generation Thresholds & Flashing -----
        private const float lowHeatThreshold = 0.05f;
        private const float highHeatThreshold = 0.2f;
        private const float FlashSpeed = 2f;  // cycles per second when overheating

        // =====================================================
        // Gizmo Width Override
        // =====================================================
        // Override the Width property so the container reserves less horizontal space.
        protected override float Width
        {
            get { return BarWidth * Scale; }
        }

        // =====================================================
        // Constructor
        // =====================================================
        public GeneGizmo_HeatResource(Gene_Resource gene, List<IGeneResourceDrain> drainGenes,
            Color barColor, Color barHighlightColor)
            : base(gene, drainGenes ?? new List<IGeneResourceDrain>(),
                  barColor, barHighlightColor)
        {
        }

        public override bool Visible => true;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            // Create a base position for all elements by applying GlobalOffset.
            Vector2 basePos = topLeft + GlobalOffset;

            // -------- 1. Calculate Heat Percentage & Colors --------
            float heatPercent = gene.Value / gene.Max;
            Color blueColor = new Color(0.4f, 0.6f, 1f);
            Color redColor = new Color(1f, 0f, 0f);
            Color baseColor = Color.Lerp(blueColor, redColor, heatPercent);

            bool isOverheating = false;
            var neutroGene = gene.pawn?.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
            if (neutroGene != null && neutroGene.IsOverheating())
                isOverheating = true;

            Color fillColor = baseColor;
            if (isOverheating)
            {
                float t = Mathf.PingPong(Time.realtimeSinceStartup * FlashSpeed, 1f);
                // Create darkened (70% brightness) and brightened (120% brightness) versions of baseColor.
                Color darkColor = new Color(
                    Mathf.Clamp01(baseColor.r * 0.7f),
                    Mathf.Clamp01(baseColor.g * 0.7f),
                    Mathf.Clamp01(baseColor.b * 0.7f),
                    baseColor.a);
                Color brightColor = new Color(
                    Mathf.Clamp01(baseColor.r * 1.2f),
                    Mathf.Clamp01(baseColor.g * 1.2f),
                    Mathf.Clamp01(baseColor.b * 1.2f),
                    baseColor.a);
                // Interpolate the fill color between dark and bright versions.
                fillColor = Color.Lerp(darkColor, brightColor, t);
            }
            fillColor = new Color(fillColor.r, fillColor.g, fillColor.b, 1f);  // Force full opacity

            // -------- 2. Draw the Bar and Fill --------
            Rect barRect = new Rect(
                basePos.x + XOffset * Scale,
                basePos.y + BarYOffset * Scale,
                BarWidth * Scale,
                BarHeight * Scale
            );

            Texture2D barTex = ContentFinder<Texture2D>.Get("UI/Icons/Gizmo/HeatBar/Bar/HeatGizmoBar", false);
            if (barTex != null)
            {
                barTex.filterMode = FilterMode.Bilinear;
            }
            Texture2D overlayTex = ContentFinder<Texture2D>.Get("UI/Icons/Gizmo/HeatBar/HeatGizmoIcon", false);
            if (overlayTex != null)
            {
                overlayTex.filterMode = FilterMode.Bilinear;
            }

            // We'll store the overlay texture's draw rectangle here.
            Rect overlayRect = Rect.zero;
            if (barTex != null && overlayTex != null)
            {
                float aspect = (float)overlayTex.width / overlayTex.height;
                float texHeight = barRect.height;
                float texWidth = texHeight * aspect;
                float textureX = barRect.x + (barRect.width - texWidth) / 2f;
                overlayRect = new Rect(textureX, barRect.y, texWidth, texHeight);

                Rect fillDestRect = new Rect(
                    overlayRect.x,
                    overlayRect.y + overlayRect.height * (1 - heatPercent),
                    overlayRect.width,
                    overlayRect.height * heatPercent
                );
                Rect fillSourceRect = new Rect(0f, 0f, 1f, heatPercent);
                Material transparentMat = new Material(ShaderDatabase.Transparent);
                Graphics.DrawTexture(fillDestRect, barTex, fillSourceRect, 0, 0, 0, 0, fillColor, transparentMat);

                // Draw overlay texture with full opacity.
                Color prevColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 1f);
                GUI.DrawTexture(overlayRect, overlayTex);
                GUI.color = prevColor;
            }
            else
            {
                Widgets.DrawBoxSolid(barRect, new Color(0.1f, 0.1f, 0.1f, 0.8f));
                Rect fallbackFill = barRect.ContractedBy(2f);
                fallbackFill.width *= heatPercent;
                Widgets.DrawBoxSolid(fallbackFill, fillColor);
            }

            // If the mouse is over the region (overlay if available or barRect otherwise),
            // draw a highlight effect.
            Rect highlightRect = overlayTex != null ? overlayRect : barRect;
            if (Mouse.IsOver(highlightRect))
            {
                Widgets.DrawHighlight(highlightRect);
            }

            // -------- 3. Draw Temperature Text --------
            float currentTemp = Mathf.Lerp(TemperatureMin, TemperatureMax, heatPercent);
            string tempText = Prefs.TemperatureMode == TemperatureDisplayMode.Fahrenheit
                ? $"{GenTemperature.CelsiusTo(currentTemp, TemperatureDisplayMode.Fahrenheit):0}°F"
                : $"{currentTemp:0}°C";
            Rect tempRect = new Rect(
                basePos.x + TempTextXOffset,
                basePos.y + TempTextYOffset,
                BarWidth * Scale,
                20f
            );
            GameFont prevFont = Text.Font;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            GUI.color = fillColor;
            Widgets.Label(tempRect, tempText);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = prevFont;

            // -------- 4. Draw Spike Icon --------
            float heatGen = EstimateCurrentHeatGeneration();
            if (heatGen >= lowHeatThreshold)
            {
                Texture2D spikeTex = heatGen >= highHeatThreshold
                    ? ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Burning", false)
                    : ContentFinder<Texture2D>.Get("UI/Icons/PassionMinor", false);
                if (spikeTex != null)
                {
                    spikeTex.filterMode = FilterMode.Bilinear;
                    float spikeLerp = Mathf.InverseLerp(lowHeatThreshold, highHeatThreshold, heatGen);
                    // Adjust multiplier: at low heat it will be SpikeIconMinMultiplier, at high heat 1.
                    spikeLerp = Mathf.Lerp(SpikeIconMinMultiplier, 1f, spikeLerp);
                    float iconSize = SpikeIconBaseSize * spikeLerp;
                    // Draw spike icon using its absolute offsets.
                    Rect spikeRect = new Rect(
                        basePos.x + SpikeXOffset,
                        basePos.y + SpikeYOffset,
                        iconSize,
                        iconSize
                    );
                    GUI.DrawTexture(spikeRect, spikeTex);
                }
            }

            // -------- 5. Draw Overheating Protection Checkbox --------
            if (gene.pawn != null && gene.pawn.IsColonistPlayerControlled)
            {
                float checkboxSize = CheckboxSize;  // Using the adjustable CheckboxSize variable.
                Rect checkRect = new Rect(
                    basePos.x + CheckboxXOffset,
                    basePos.y + CheckboxYOffset,
                    checkboxSize,
                    checkboxSize
                );
                Texture2D fireBurstTex = ContentFinder<Texture2D>.Get("UI/Icons/Genes/Gene_FireResistant", false);
                if (fireBurstTex != null)
                {
                    fireBurstTex.filterMode = FilterMode.Bilinear;
                    GUI.DrawTexture(checkRect, fireBurstTex);
                }
                Rect checkmarkRect = new Rect(
                    checkRect.x,
                    checkRect.y,
                    checkRect.width * 0.6f,
                    checkRect.height * 0.6f
                );
                Texture2D checkmarkTex = WorkerDronesModSettings.OverheatingProtectionEnabled
                    ? Widgets.CheckboxOnTex
                    : Widgets.CheckboxOffTex;
                GUI.DrawTexture(checkmarkRect, checkmarkTex);

                if (Widgets.ButtonInvisible(checkRect))
                {
                    WorkerDronesModSettings.OverheatingProtectionEnabled = !WorkerDronesModSettings.OverheatingProtectionEnabled;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }
                TooltipHandler.TipRegion(checkRect, "MD.OverheatingProtection".Translate());
            }

            // -------- 6. Create the Main Tooltip --------
            // Now, register the tooltip for the overlay texture region if available.
            if (overlayTex != null)
            {
                TooltipHandler.TipRegion(overlayRect, GetTooltip());
            }
            else
            {
                // Fallback: use the barRect (or any other preferred region)
                Rect fallbackTooltipRect = new Rect(barRect.x, barRect.y, barRect.width, barRect.height + 20f);
                TooltipHandler.TipRegion(fallbackTooltipRect, GetTooltip());
            }

            return new GizmoResult(GizmoState.Clear);
        }

        // =====================================================
        // Estimate Heat Generation
        // =====================================================
        private float EstimateCurrentHeatGeneration()
        {
            float heatGeneration = 0f;
            var neutroGene = gene.pawn?.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
            if (neutroGene == null || neutroGene.pawn == null)
                return heatGeneration;

            Pawn pawn = neutroGene.pawn;
            float multiplier = (neutroGene.Value <= 0f) ? 2.0f : 1f;

            // Injuries contribute heat.
            var injuries = pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>();
            heatGeneration += injuries.Sum(i => i.Severity) * 0.002f * multiplier;

            // Sunlight exposure contributes heat.
            if (pawn.Map != null && !pawn.Position.Roofed(pawn.Map) && pawn.Map.skyManager.CurSkyGlow > 0.5f)
                heatGeneration += 0.1f * multiplier;

            // Limb regeneration contributes heat.
            heatGeneration += RoboticLimbRegenerator.CalculateHeatForPawn(pawn) * multiplier;

            return heatGeneration;
        }

        // =====================================================
        // Tooltip Text for the Gizmo
        // =====================================================
        protected override string GetTooltip()
        {
            var sb = new System.Text.StringBuilder();
            float heatGen = EstimateCurrentHeatGeneration();

            // Pre-format numerical values
            string formattedHeatGen = heatGen.ToString("0.000");
            string formattedLow = lowHeatThreshold.ToString("0.00");
            string formattedHigh = highHeatThreshold.ToString("0.00");

            // Use translation keys with pre-formatted parameters.
            sb.AppendLine("MD.CurrentHeatGeneration".Translate(formattedHeatGen));
            sb.AppendLine("MD.CoolingSystemExplanation".Translate());
            sb.AppendLine();
            sb.AppendLine("MD.HeatSources".Translate());
            sb.AppendLine("MD.HeatSource.Injuries".Translate());
            sb.AppendLine("MD.HeatSource.Sunlight".Translate());
            sb.AppendLine("MD.HeatSource.Regeneration".Translate());
            sb.AppendLine();
            sb.AppendLine("MD.Thresholds".Translate());
            sb.AppendLine("MD.Threshold.Low".Translate(formattedLow));
            sb.AppendLine("MD.Threshold.Medium".Translate(formattedLow, formattedHigh));
            sb.AppendLine("MD.Threshold.High".Translate(formattedHigh));

            return sb.ToString();
        }

        // =====================================================
        // Visual Bar Warning Thresholds
        // =====================================================
        protected override IEnumerable<float> GetBarThresholds()
        {
            yield return 0.7f;  // Yellow warning threshold.
            yield return 0.9f;  // Red danger threshold.
        }
    }
}
























































































