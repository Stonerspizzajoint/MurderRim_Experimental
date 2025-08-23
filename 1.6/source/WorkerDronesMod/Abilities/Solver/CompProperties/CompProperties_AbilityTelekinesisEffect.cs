using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class CompProperties_AbilityTelekinesisEffect : CompProperties_AbilityEffect
    {
        public string SpinningIconPath;
        public string SpinningIconShader = "Map/TransparentPostLight";
        public float SpinIconSize = 1f; // Default size (1x1 plane)
        public bool SpinIconSkinColor = true; // Default: skin color tint
        public bool DynamicSizeOffset = true; // If true, icon size matches held item's size

        public CompProperties_AbilityTelekinesisEffect()
        {
            compClass = typeof(Comp_AbilityTelekinesisEffect);
        }
    }
}
