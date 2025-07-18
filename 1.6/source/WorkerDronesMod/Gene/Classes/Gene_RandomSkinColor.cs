using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // <-- Add this line


namespace WorkerDronesMod
{
    /// <summary>
    /// Gene that forces a pawn's skin color to a random full-bright color or mix of colors (white, red, yellow, blue, green).
    /// </summary>
    public class Gene_RandomSkinColor : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            ApplyRandomSkinColor();
        }

        public override void PostMake()
        {
            base.PostMake();
            ApplyRandomSkinColor();
        }

        public override void PostRemove()
        {
            base.PostRemove();
            if (pawn != null && pawn.story != null)
            {
                pawn.story.skinColorOverride = Color.white;
                if (pawn.Drawer != null && pawn.Drawer.renderer != null)
                {
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
            }
        }

        private void ApplyRandomSkinColor()
        {
            if (pawn == null || pawn.story == null)
                return;

            // Use custom color palette
            List<Color> mixingColors = new List<Color>
            {
                new Color32(255, 255, 255, 255), // pale (white)
                new Color32(255, 0, 222, 255),
                new Color32(163, 102, 255, 255),
                new Color32(255, 0, 0, 255),
                new Color32(255, 147, 15, 255),
                new Color32(15, 119, 255, 255),
                new Color32(57, 244, 247, 255),
                new Color32(126, 255, 79, 255),
                new Color32(247, 219, 36, 255) // yellow
            };

            int mixCount = Rand.RangeInclusive(2, 3);
            List<Color> chosenColors = new List<Color>();

            // Ensure yellow is never the only color chosen and yellow never mixes with white
            do
            {
                chosenColors.Clear();
                for (int i = 0; i < mixCount; i++)
                {
                    chosenColors.Add(mixingColors[Rand.Range(0, mixingColors.Count)]);
                }
            }
            while (
                (chosenColors.Count > 1 && chosenColors.All(c => c == mixingColors[8])) || // Only yellow
                (chosenColors.Contains(mixingColors[8]) && chosenColors.Contains(mixingColors[0])) // Yellow and white together
            );

            // Blend the chosen colors
            Color mixedColor = chosenColors[0];
            for (int i = 1; i < chosenColors.Count; i++)
            {
                mixedColor = Color.Lerp(mixedColor, chosenColors[i], 0.5f);
            }

            // Ensure full brightness (normalize to max channel)
            float maxChannel = Mathf.Max(mixedColor.r, mixedColor.g, mixedColor.b);
            if (maxChannel > 0f)
            {
                mixedColor.r /= maxChannel;
                mixedColor.g /= maxChannel;
                mixedColor.b /= maxChannel;
            }

            pawn.story.skinColorOverride = mixedColor;

            // Force graphics update
            if (pawn.Drawer != null && pawn.Drawer.renderer != null)
            {
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }
    }
}

