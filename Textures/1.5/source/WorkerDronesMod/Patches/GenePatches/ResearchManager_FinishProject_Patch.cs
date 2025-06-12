using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace WorkerDronesMod.Patches
{
    [StaticConstructorOnStartup]
    public static class GeneResearchManager
    {
        private static readonly Dictionary<ResearchProjectDef, List<GeneDef>> researchGeneMap;

        static GeneResearchManager()
        {
            researchGeneMap = new Dictionary<ResearchProjectDef, List<GeneDef>>();

            foreach (GeneDef gene in DefDatabase<GeneDef>.AllDefs)
            {
                var extension = gene.GetModExtension<GeneResearchExtension>();
                if (extension?.requiredResearch != null)
                {
                    if (!researchGeneMap.TryGetValue(extension.requiredResearch, out var list))
                    {
                        list = new List<GeneDef>();
                        researchGeneMap[extension.requiredResearch] = list;
                    }
                    list.Add(gene);
                }
            }
        }

        [HarmonyPatch(typeof(ResearchProjectDef), nameof(ResearchProjectDef.UnlockedDefs), MethodType.Getter)]
        public static class ResearchProjectDef_UnlockedDefs_Patch
        {
            public static void Postfix(ResearchProjectDef __instance, ref IEnumerable<Def> __result)
            {
                if (researchGeneMap.TryGetValue(__instance, out var genes))
                {
                    __result = (__result ?? Enumerable.Empty<Def>())
                        .Concat(genes.Cast<Def>())
                        .Distinct()
                        .ToList();
                }
            }
        }
    }
}

