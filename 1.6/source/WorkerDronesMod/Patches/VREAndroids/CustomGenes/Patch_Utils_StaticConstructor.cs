using HarmonyLib;
using Verse;
using System.Linq;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(VREAndroids.Utils), MethodType.StaticConstructor)]
    public static class Patch_Utils_StaticConstructor
    {
        public static void AddCustomGenes()
        {
            var androidOnlyCategories = DefDatabase<GeneCategoryDef>.AllDefsListForReading
                .Where(cat => cat.GetType() == typeof(AndroidGeneCategoryDef))
                .ToHashSet();

            var customCategoryGenes = DefDatabase<GeneDef>.AllDefsListForReading
                .Where(g => g.displayCategory != null
                            && androidOnlyCategories.Contains(g.displayCategory)
                            && g.endogeneCategory != EndogeneCategory.Melanin)
                .ToList();

            int before = VREAndroids.Utils.allAndroidGenes.Count;
            foreach (var geneDef in customCategoryGenes)
            {
                if (!VREAndroids.Utils.allAndroidGenes.Contains(geneDef))
                {
                    VREAndroids.Utils.allAndroidGenes.Add(geneDef);
                }
            }
            int after = VREAndroids.Utils.allAndroidGenes.Count;

            // Force rebuild of cachedGeneDefsInOrder
            var cachedGeneDefsInOrderField = typeof(VREAndroids.Utils).GetField("cachedGeneDefsInOrder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            cachedGeneDefsInOrderField?.SetValue(null, null);
        }
    }

    [HarmonyPatch(typeof(VREAndroids.Utils), "get_AndroidGenesGenesInOrder")]
    public static class Patch_Utils_AndroidGenesGenesInOrderGetter
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            Patch_Utils_StaticConstructor.AddCustomGenes();
        }
    }
}
