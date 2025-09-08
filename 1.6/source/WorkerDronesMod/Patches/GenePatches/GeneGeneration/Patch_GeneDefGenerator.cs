using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(GeneDefGenerator), "ImpliedGeneDefs")]
    public static class Patch_GeneDefGenerator
    {
        [HarmonyPrefix]
        public static bool ImpliedGeneDefs_Prefix(ref IEnumerable<GeneDef> __result, bool hotReload = false)
        {
            if (!ModsConfig.BiotechActive)
            {
                __result = Enumerable.Empty<GeneDef>();
                return false;
            }

            var result = new List<GeneDef>();
            var getFromTemplate = AccessTools.Method(typeof(GeneDefGenerator), "GetFromTemplate");

            foreach (GeneTemplateDef g in DefDatabase<GeneTemplateDef>.AllDefs)
            {
                if (g.geneTemplateType == GeneTemplateDef.GeneTemplateType.Skill)
                {
                    foreach (SkillDef skillDef in DefDatabase<SkillDef>.AllDefs)
                    {
                        if (skillDef == MD_DefOf.SolverControl)
                            continue; // Skip SolverControl

                        var geneDef = (GeneDef)getFromTemplate.Invoke(null, new object[] { g, skillDef, (int)(skillDef.index * 1000), hotReload });
                        if (geneDef != null)
                            result.Add(geneDef);
                    }
                }
                else if (g.geneTemplateType == GeneTemplateDef.GeneTemplateType.Chemical)
                {
                    foreach (ChemicalDef chemicalDef in DefDatabase<ChemicalDef>.AllDefs)
                    {
                        if (chemicalDef.generateAddictionGenes)
                        {
                            var geneDef = (GeneDef)getFromTemplate.Invoke(null, new object[] { g, chemicalDef, (int)(chemicalDef.index * 1000), hotReload });
                            if (geneDef != null)
                                result.Add(geneDef);
                        }
                    }
                }
            }
            __result = result;
            return false; // Skip original method
        }
    }
}
