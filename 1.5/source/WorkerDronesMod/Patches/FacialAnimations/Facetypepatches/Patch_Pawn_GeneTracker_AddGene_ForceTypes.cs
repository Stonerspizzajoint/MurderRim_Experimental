using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using FacialAnimation;
using WorkerDronesMod;

namespace WorkerDronesMod.Patches.FacialAnimations
{
    [HarmonyPatch(typeof(Pawn_GeneTracker), "AddGene", new[] { typeof(Gene), typeof(bool) })]
    static class Patch_Pawn_GeneTracker_AddGene_ForceTypes
    {
        static void Postfix(Gene __result, Pawn_GeneTracker __instance)
        {
            if (__result == null) return;
            var ext = __result.def.GetModExtension<GeneForcedFacetypesExtension>();
            if (ext == null) return;

            var pawn = __instance.pawn;
            if (pawn == null) return;

            // pick by probability or null if empty
            FacialAnimation.HeadTypeDef head = ext.forcedHeadTypes != null && ext.forcedHeadTypes.Any()
                                   ? ext.forcedHeadTypes.RandomElementByWeight(h => h.probability)
                                   : null;

            BrowTypeDef brow = ext.forcedBrowTypes != null && ext.forcedBrowTypes.Any()
                                   ? ext.forcedBrowTypes.RandomElementByWeight(b => b.probability)
                                   : null;

            MouthTypeDef mouth = ext.forcedMouthTypes != null && ext.forcedMouthTypes.Any()
                                   ? ext.forcedMouthTypes.RandomElementByWeight(m => m.probability)
                                   : null;

            EyeballTypeDef eye = ext.forcedEyeTypes != null && ext.forcedEyeTypes.Any()
                                   ? ext.forcedEyeTypes.RandomElementByWeight(e => e.probability)
                                   : null;

            LidTypeDef lid = ext.forcedLidTypes != null && ext.forcedLidTypes.Any()
                                   ? ext.forcedLidTypes.RandomElementByWeight(l => l.probability)
                                   : null;

            void TrySet(object comp, Def val)
            {
                if (val == null) return;
                var pi = comp.GetType().GetProperty("FaceType",
                    BindingFlags.Instance | BindingFlags.Public);
                if (pi != null && pi.CanWrite && pi.PropertyType.IsAssignableFrom(val.GetType()))
                {
                    pi.SetValue(comp, val);
                    comp.GetType().GetMethod("SetDirty",
                        BindingFlags.Instance | BindingFlags.Public)
                        ?.Invoke(comp, null);
                    comp.GetType().GetMethod("InitializeIfNeed",
                        BindingFlags.Instance | BindingFlags.Public)
                        ?.Invoke(comp, null);
                }
            }

            foreach (var comp in pawn.AllComps.Where(c => c != null))
            {
                TrySet(comp, head);
                TrySet(comp, brow);
                TrySet(comp, mouth);
                TrySet(comp, eye);
                TrySet(comp, lid);
            }
        }
    }
}








