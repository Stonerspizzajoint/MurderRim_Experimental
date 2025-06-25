// WorkerDronesMod/FacialAnimationIntegration.cs
using Verse;

namespace WorkerDronesMod
{
    public static class FacialAnimationIntegration
    {
        public static readonly bool IsLoaded =
            ModLister.GetActiveModWithIdentifier("nals.facialanimation") != null;
    }
}

