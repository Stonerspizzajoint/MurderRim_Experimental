using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace WorkerDronesMod
{
    /// <summary>
    /// Custom resource gizmo for the BasicSolver gene showing Oil (dark gray) and Heat (red) with overlaid text.
    /// Includes temperature fluctuations that grow with heat level to mimic a real machine.
    /// Can fluctuate below the minimum base temperature.
    /// </summary>
    [StaticConstructorOnStartup]
    public class GeneGizmo_BasicSolver : GeneGizmo_Resource
    {
        private readonly Gene_BasicSolver basicSolver;
        private float lastHeat = -1f;
        private static readonly Texture2D BurningIcon = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Burning");
        private float smoothedHeatDelta = 0f;
        private int lastGameTick = -1;
        private float lastFluct = 0f;


        public GeneGizmo_BasicSolver(Gene_Resource gene, List<IGeneResourceDrain> drainGenes, Color barColor, Color barHighlightColor)
            : base(gene, drainGenes, barColor, barHighlightColor)
        {
            basicSolver = gene as Gene_BasicSolver;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            const float labelBoxHeight = 24f; // Should match the value used for startY
            const float verticalOffset = -20f; // Raise the gizmo up by 8 pixels (adjust as needed)
            Rect mainRect = new Rect(topLeft.x, topLeft.y + verticalOffset, 140f, 75f + labelBoxHeight);
            GUI.BeginGroup(mainRect);

            // Base resource bar (from GeneGizmo_Resource)  
            base.GizmoOnGUI(topLeft, maxWidth, parms);

            // Checkbox for restricting to roofed areas
            const float checkboxSize = 24f;
            Rect checkboxRect = new Rect(4f, 4f, checkboxSize, checkboxSize);

            // Draw the base texture
            Texture2D checkboxTex = ContentFinder<Texture2D>.Get("UI/Designators/BuildRoofArea");
            GUI.DrawTexture(checkboxRect, checkboxTex);

            // Draw overlay: X if false, checkmark if true
            if (basicSolver.RestrictToRoofedAreas)
            {
                // Draw checkmark (use vanilla checkmark)
                Texture2D checkmarkTex = Widgets.CheckboxOnTex;
                GUI.DrawTexture(checkboxRect, checkmarkTex);
            }
            else
            {
                // Draw X (use vanilla X)
                Texture2D xTex = Widgets.CheckboxOffTex;
                GUI.DrawTexture(checkboxRect, xTex);
            }

            // Handle click
            if (Widgets.ButtonInvisible(checkboxRect))
            {
                basicSolver.RestrictToRoofedAreas = !basicSolver.RestrictToRoofedAreas;
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }

            // Tooltip using translation key
            TooltipHandler.TipRegion(checkboxRect, "MD.ForceRoofArea".Translate());

            // Calculate vertical positions to center bars  
            const float barHeight = 18f;
            const float spacing = 4f;
            float startY = (mainRect.height - (barHeight * 2 + spacing)) / 2f + labelBoxHeight;

            // Oil Bar with overlaid text  
            Rect oilBarRect = new Rect(0, startY, mainRect.width, barHeight);
            float oilRatio = Mathf.Clamp01(basicSolver.Oil / basicSolver.InitialResourceMax);

            // Flashing effect when oil is below a threshold  
            Color oilColor = Color.gray;
            if (oilRatio < 0.2f) // Threshold for low oil  
            {
                float flash = Mathf.Abs(Mathf.Sin(Time.realtimeSinceStartup * 4f));
                oilColor = Color.Lerp(oilColor, Color.red, flash);
            }

            Widgets.FillableBar(oilBarRect, oilRatio, SolidColorMaterials.NewSolidColorTexture(oilColor));
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(oilBarRect, $"{"MD.OilLabel".Translate()}: {Mathf.RoundToInt(basicSolver.Oil)}/{basicSolver.InitialResourceMax}");
            Text.Anchor = TextAnchor.UpperLeft;

            // Compute actual heat ratio  
            float heatRatio = basicSolver.Heat / basicSolver.InitialResourceMax;

            // Scale fluctuation instability by heat ratio (max at 100% heat)  
            float instability = Mathf.Clamp01(heatRatio);
            const float maxFluctAmplitude = 0.1f; // ±10% at full heat  
            float fluct;
            if (!Find.TickManager.Paused)
            {
                fluct = (Mathf.PerlinNoise(Time.realtimeSinceStartup * 0.5f, 0f) - 0.5f) * maxFluctAmplitude * instability;
                lastFluct = fluct;
            }
            else
            {
                fluct = lastFluct;
            }
            float displayRatio = heatRatio + fluct;


            // Determine bar color based on fluctuated ratio  
            Color heatColor = Color.Lerp(Color.blue, Color.red, Mathf.Clamp01(displayRatio));
            if (displayRatio > 1.0f)
            {
                float flash = Mathf.Abs(Mathf.Sin(Time.realtimeSinceStartup * 4f));
                heatColor = Color.Lerp(heatColor, Color.white, flash * 0.5f);
            }

            // Determine text to display based on heat level  
            string heatText;
            if (heatRatio >= 1.1f)
            {
                heatText = "MD.HeatCritical".Translate();
            }
            else if (heatRatio <= 0.05f)
            {
                heatText = "MD.HeatSafe".Translate();
            }
            else
            {
                float tempF = Mathf.LerpUnclamped(70f, 249f, displayRatio);
                float tempC = GenTemperature.ConvertTemperatureOffset(tempF, TemperatureDisplayMode.Fahrenheit, TemperatureDisplayMode.Celsius);
                heatText = Prefs.TemperatureMode == TemperatureDisplayMode.Celsius
                    ? $"{Mathf.RoundToInt(tempC)}°C"
                    : $"{Mathf.RoundToInt(tempF)}°F";
            }

            // Heat Bar with overlaid fluctuated fill and text  
            Rect heatBarRect = new Rect(0, startY + barHeight + spacing, mainRect.width, barHeight);
            Widgets.FillableBar(heatBarRect, Mathf.Clamp01(Mathf.Min(displayRatio, 2.0f)),
                                 SolidColorMaterials.NewSolidColorTexture(heatColor));
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(heatBarRect, heatText);
            Text.Anchor = TextAnchor.UpperLeft;

            // Draw draggable threshold marker (downward triangle) on oil bar
            if (basicSolver.pawn.IsColonistPlayerControlled)
            {
                // Clamp markerX to always be within the bar's visible area
                float markerX = Mathf.Clamp(
                    oilBarRect.x + oilBarRect.width * basicSolver.OilRefuelThreshold,
                    oilBarRect.x,
                    oilBarRect.x + oilBarRect.width - 1f
                );

                // Draw marker (downward triangle)
                Vector2 p0 = new Vector2(markerX - 6f, oilBarRect.y - 6f);
                Vector2 p1 = new Vector2(markerX + 6f, oilBarRect.y - 6f);
                Vector2 p2 = new Vector2(markerX, oilBarRect.y + 2f);

                Color prevColor = GUI.color;
                GUI.color = Color.yellow;
                DrawSolidTriangle(p0, p1, p2);
                GUI.color = prevColor;

                // Draw threshold value in a black box centered above the oil bar
                string thresholdLabel = basicSolver.OilRefuelThreshold <= 0f
                    ? "MD.NeutroamineConsumptionDisabled".Translate().ToString()
                    : $"{Mathf.RoundToInt(basicSolver.OilRefuelThreshold * 100f)}%";
                Vector2 labelSize = Text.CalcSize(thresholdLabel);
                float boxPaddingX = 8f;
                float boxPaddingY = 2f;
                Rect labelBox = new Rect(
                    oilBarRect.x + (oilBarRect.width - labelSize.x) / 2f - boxPaddingX,
                    oilBarRect.y - labelSize.y - 10f - boxPaddingY,
                    labelSize.x + boxPaddingX * 2f,
                    labelSize.y + boxPaddingY * 2f
                );

                // Draw black background box
                GUI.color = Color.black;
                GUI.DrawTexture(labelBox, BaseContent.WhiteTex);
                GUI.color = Color.white;

                // Draw label text centered in the box
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(labelBox, thresholdLabel);
                Text.Anchor = TextAnchor.UpperLeft;


                // Make the drag area exactly the oil bar (with a little extra vertical padding)
                Rect dragRect = new Rect(oilBarRect.x, oilBarRect.y - 10f, oilBarRect.width, oilBarRect.height + 18f);

                // Handle click or drag anywhere on the oil bar
                if (Mouse.IsOver(dragRect) && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && Event.current.button == 0)
                {
                    float mouseX = Mathf.Clamp(Event.current.mousePosition.x, oilBarRect.x, oilBarRect.x + oilBarRect.width - 1f);
                    float mouseBarRatio = (mouseX - oilBarRect.x) / (oilBarRect.width - 1f);
                    float snapped = Mathf.Round(mouseBarRatio * 20f) / 20f;
                    snapped = Mathf.Clamp(snapped, 0f, 1.0f);
                    if (!Mathf.Approximately(snapped, basicSolver.OilRefuelThreshold))
                    {
                        basicSolver.OilRefuelThreshold = snapped;
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    }
                    Event.current.Use();
                }

                TooltipHandler.TipRegion(dragRect, "Set the oil level at which this pawn will refuel.");
            }

            // Only update smoothing once per game tick
            int currentTick = Find.TickManager.TicksGame;
            float heatDelta = 0f;
            if (lastHeat >= 0f)
                heatDelta = basicSolver.Heat - lastHeat;

            if (currentTick != lastGameTick)
            {
                // Smooth the delta (exponential moving average)
                smoothedHeatDelta = Mathf.Lerp(smoothedHeatDelta, heatDelta, 0.5f); // 0.5f = smoothing factor, tweak as needed
                lastHeat = basicSolver.Heat;
                lastGameTick = currentTick;
            }

            // --- Burning icon for rising heat ---
            bool isOverheating = heatRatio >= 1.1f;
            float iconSize = Mathf.Clamp(20f + smoothedHeatDelta * 60f, 20f, 48f);
            if (isOverheating)
            {
                iconSize = 48f;
            }
            if (smoothedHeatDelta > 0.01f || isOverheating)
            {
                // Calculate icon center as before
                float iconCenterX = heatBarRect.xMax - barHeight / 2f;
                float iconCenterY = heatBarRect.y + heatBarRect.height / 2f;
                float iconX = iconCenterX - iconSize / 2f;
                float iconY = iconCenterY - iconSize / 2f;

                // Clamp so the icon stays fully inside the gizmo group
                iconX = Mathf.Min(iconX, mainRect.width - iconSize - 1f);
                iconY = Mathf.Min(iconY, mainRect.height - iconSize - 1f);
                iconX = Mathf.Max(iconX, 0f);
                iconY = Mathf.Max(iconY, 0f);

                Rect iconRect = new Rect(iconX, iconY, iconSize, iconSize);

                Color iconColor = Color.white;
                if (isOverheating)
                {
                    float flash = Mathf.Abs(Mathf.Sin(Time.realtimeSinceStartup * 4f));
                    iconColor = Color.Lerp(Color.white, Color.red, flash * 0.5f);
                }
                GUI.color = iconColor;
                GUI.DrawTexture(iconRect, BurningIcon);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(iconRect, "MD.HeatRisingTooltip".Translate());
            }


            GUI.EndGroup();
            return new GizmoResult(GizmoState.Clear);
        }

        // Draws a filled triangle using GL immediate mode (works in OnGUI)
        private static void DrawSolidTriangle(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            SolidWhiteMaterial.SetPass(0);
            GL.Begin(GL.TRIANGLES);
            GL.Color(GUI.color);
            GL.Vertex3(p0.x, p0.y, 0f);
            GL.Vertex3(p1.x, p1.y, 0f);
            GL.Vertex3(p2.x, p2.y, 0f);
            GL.End();
        }

        protected override string GetTooltip()
        {
            float heatRatio = basicSolver.Heat / basicSolver.InitialResourceMax;
            float tempF = Mathf.LerpUnclamped(70f, 249f, heatRatio);
            float tempC = GenTemperature.ConvertTemperatureOffset(tempF, TemperatureDisplayMode.Fahrenheit, TemperatureDisplayMode.Celsius);
            string tempStr = Prefs.TemperatureMode == TemperatureDisplayMode.Celsius
                         ? $"{Mathf.RoundToInt(tempC)}°C"
                         : $"{Mathf.RoundToInt(tempF)}°F";

            return $"{"MD.OilLabel".Translate()}: {Mathf.RoundToInt(basicSolver.Oil)}/{basicSolver.InitialResourceMax}\n" +
                   $"{"MD.HeatLabel".Translate()}: {tempStr}";
        }

        private static readonly Material SolidWhiteMaterial = new Material(ShaderDatabase.MetaOverlay);

    }
}







