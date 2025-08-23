using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Utils), "get_AndroidGenesGenesInOrder")]
    public static class Patch_AndroidGenesGenesInOrder_CustomCategories
    {
        private static List<GeneDef> cachedResult;
        private static int lastGeneCount = -1;

        [HarmonyPostfix]
        public static void Postfix(ref List<GeneDef> __result)
        {
            // Gather all categories of type WorkerDronesMod.AndroidGeneCategoryDef
            var androidOnlyCategories = new HashSet<GeneCategoryDef>();
            foreach (var cat in DefDatabase<GeneCategoryDef>.AllDefsListForReading)
            {
                if (cat.GetType() == typeof(AndroidGeneCategoryDef))
                    androidOnlyCategories.Add(cat);
            }

            // Only rebuild if the gene count has changed
            int currentGeneCount = __result.Count;
            if (cachedResult == null || lastGeneCount != currentGeneCount)
            {
                lastGeneCount = currentGeneCount;

                // Add genes in those categories if not already present
                foreach (var geneDef in DefDatabase<GeneDef>.AllDefsListForReading)
                {
                    if (geneDef.displayCategory != null &&
                        androidOnlyCategories.Contains(geneDef.displayCategory) &&
                        geneDef.endogeneCategory != EndogeneCategory.Melanin &&
                        !__result.Contains(geneDef))
                    {
                        __result.Add(geneDef);
                    }
                }

                // Sort by VREA's logic
                __result.SortBy(
                    (GeneDef x) => 0f - x.displayCategory.displayPriorityInXenotype,
                    (GeneDef x) => x.displayCategory.label,
                    (GeneDef x) => x.displayOrderInCategory
                );

                // Cache the result
                cachedResult = new List<GeneDef>(__result);
            }
            else
            {
                // Use cached result to prevent flashing
                __result.Clear();
                __result.AddRange(cachedResult);
            }
        }
    }
}
