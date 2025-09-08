using System.Reflection;
using Verse;
using FacialAnimation;

namespace WorkerDronesMod.FacialAnimationCompat
{
    public static class FacialAnimationGeneUtil
    {
        public static void SafeReload(object comp)
        {
            if (comp == null) return;

            // Get the pawn field
            var pawnField = comp.GetType().GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var pawnVal = pawnField?.GetValue(comp) as Pawn;
            if (pawnVal == null || pawnVal.DestroyedOrNull()) return;

            // Get the FaceType property
            var faceTypeProp = comp.GetType().GetProperty("FaceType");
            if (faceTypeProp != null && faceTypeProp.GetValue(comp) == null) return;

            // Get and invoke ReloadIfNeed
            var reload = comp.GetType().GetMethod("ReloadIfNeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            reload?.Invoke(comp, null);
        }
    }
}