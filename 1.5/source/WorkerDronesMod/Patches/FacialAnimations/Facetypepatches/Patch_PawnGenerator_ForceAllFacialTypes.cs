using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using FacialAnimation;
using WorkerDronesMod;

namespace WorkerDronesMod.Patches.FacialAnimations
{
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
    static class Patch_PawnGenerator_ForceAllFacialTypes
    {
        static void Postfix(Pawn __result)
        {
            var pawn = __result;
            var genes = pawn.genes?.GenesListForReading;
            if (genes == null) return;

            var ext = genes
                .Select(g => g.def.GetModExtension<GeneForcedFacetypesExtension>())
                .FirstOrDefault(e => e != null);
            if (ext == null) return;

            // Helper to set a property if available
            void TryProp<TVal>(object comp, string propName, TVal val)
            {
                if (val == null) return;
                var pi = comp.GetType().GetProperty(propName,
                    BindingFlags.Instance | BindingFlags.Public);
                if (pi != null && pi.CanWrite && pi.PropertyType.IsAssignableFrom(typeof(TVal)))
                    pi.SetValue(comp, val);
            }

            foreach (var comp in pawn.AllComps.Where(c => c != null))
            {
                // HEAD
                if (ext.forcedHeadTypes?.Any() == true)
                {
                    var pick = ext.forcedHeadTypes
                                  .RandomElementByWeight(h => h.probability);
                    TryProp(comp, "FaceType", pick);
                }

                // BROW
                if (ext.forcedBrowTypes?.Any() == true)
                {
                    var pick = ext.forcedBrowTypes
                                  .RandomElementByWeight(b => b.probability);
                    TryProp(comp, "FaceType", pick);
                }

                // MOUTH
                if (ext.forcedMouthTypes?.Any() == true)
                {
                    var pick = ext.forcedMouthTypes
                                  .RandomElementByWeight(m => m.probability);
                    TryProp(comp, "FaceType", pick);
                }

                // EYE
                if (ext.forcedEyeTypes?.Any() == true)
                {
                    var pick = ext.forcedEyeTypes
                                  .RandomElementByWeight(e => e.probability);
                    TryProp(comp, "FaceType", pick);
                }

                // LID
                if (ext.forcedLidTypes?.Any() == true)
                {
                    var pick = ext.forcedLidTypes
                                  .RandomElementByWeight(l => l.probability);
                    TryProp(comp, "FaceType", pick);
                }

                // mark dirty + reinit so no defaults override
                comp.GetType().GetMethod("SetDirty", BindingFlags.Instance | BindingFlags.Public)
                    ?.Invoke(comp, null);
                comp.GetType().GetMethod("InitializeIfNeed", BindingFlags.Instance | BindingFlags.Public)
                    ?.Invoke(comp, null);
            }
        }
    }
}










