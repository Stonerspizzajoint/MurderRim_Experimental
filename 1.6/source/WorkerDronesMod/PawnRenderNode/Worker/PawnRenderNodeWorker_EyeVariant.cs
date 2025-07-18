using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    // Assumes that PawnRenderNodeWorker is defined elsewhere in your project.
    public class PawnRenderNodeWorker_EyeVariant : PawnRenderNodeWorker
    {
        // Custom properties for this worker.
        // Make sure these properties are set during initialization.
        public PawnRenderNodeProperties_EyeVariant Props { get; set; }

        // Flag indicating whether the pawn is drafted.
        public bool IsDrafted { get; set; }

        // Returns the list of texture paths based on the drafted state.
        public List<string> CurrentTexPaths
        {
            get
            {
                if (Props == null)
                {
                    Log.Error("Properties not set for PawnRenderNodeWorker_EyeVariant.");
                    return new List<string>();
                }
                return IsDrafted ? Props.draftedTexPaths : Props.normalTexPaths;
            }
        }

        // Custom render method for the eye variant.
        // This method encapsulates the complete logic for selecting and drawing the texture.
        public void RenderEyeVariant()
        {
            if (Props == null)
            {
                Log.Error("Properties not set for PawnRenderNodeWorker_EyeVariant.");
                return;
            }

            List<string> texPaths = CurrentTexPaths;
            if (texPaths == null || texPaths.Count == 0)
            {
                Log.Warning("No texture paths available for PawnRenderNodeWorker_EyeVariant.");
                return;
            }

            // For demonstration, we choose the first texture path.
            string texturePath = texPaths[0];

            // Load the texture using Verse's ContentFinder.
            Texture2D texture = ContentFinder<Texture2D>.Get(texturePath, true);
            if (texture == null)
            {
                Log.Error($"Failed to load texture at path: {texturePath}");
                return;
            }

            // Render the texture using a helper method.
            DrawTexture(texture);
        }

        // Helper method to draw the texture.
        // Replace this implementation with your actual rendering logic.
        private void DrawTexture(Texture2D texture)
        {
            if (texture == null)
            {
                Log.Error("Attempted to draw a null texture.");
                return;
            }

            // Example drawing logic using Unity's Graphics API.
            // Define a rectangle for drawing. Adjust as needed.
            Rect drawRect = new Rect(0, 0, texture.width, texture.height);
            Graphics.DrawTexture(drawRect, texture);
        }
    }
}



















