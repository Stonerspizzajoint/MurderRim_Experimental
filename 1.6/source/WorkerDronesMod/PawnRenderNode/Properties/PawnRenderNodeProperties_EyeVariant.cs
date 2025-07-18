using System.Collections.Generic;
using Verse;

namespace WorkerDronesMod
{
    // Inherits from the vanilla PawnRenderNodeProperties_Eye.
    public class PawnRenderNodeProperties_EyeVariant : PawnRenderNodeProperties_Eye
    {
        // Texture paths when the pawn is drafted.
        public List<string> draftedTexPaths = new List<string>();
        // Texture paths when the pawn is not drafted (normal).
        public List<string> normalTexPaths = new List<string>();

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            // If texPaths is null or empty, default to normalTexPaths.
            if (texPaths == null || texPaths.Count == 0)
            {
                texPaths = normalTexPaths;
            }
        }
    }
}






