using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;
using VREAndroids;

namespace WorkerDronesMod
{
    [StaticConstructorOnStartup]
    public static class WorkerDronesMod_DebugActions
    {
        [DebugAction("WorkerDronesMod", "Check AndroidCatagory Genes", actionType = DebugActionType.Action)]
        public static void CheckAndroidGenesDebugAction()
        {
            AndroidGeneCategoryDebugUtil.LogCustomCategoryGenesInList(Utils.allAndroidGenes);
        }
    }

    /// <summary>
    /// Utility for debugging custom android gene category inclusion.
    /// </summary>
    public static class AndroidGeneCategoryDebugUtil
    {

        /// <summary>
        /// Logs all genes in the custom AndroidGeneCategoryDef and whether they are present in the target list.
        /// </summary>
        /// <param name="targetList">The list to check against (e.g., Utils.allAndroidGenes).</param>
        public static void LogCustomCategoryGenesInList(System.Collections.Generic.IEnumerable<GeneDef> targetList)
        {
            var customCategoryGenes = DefDatabase<GeneDef>.AllDefsListForReading
                .Where(g => g.displayCategory is AndroidGeneCategoryDef)
                .ToList();

            Log.Message($"[WorkerDronesMod] --- Checking custom category genes against target list ({targetList.Count()} entries) ---");
            foreach (var gene in customCategoryGenes)
            {
                bool inList = targetList.Any(g2 => g2.defName == gene.defName);
                Log.Message($"[WorkerDronesMod] Custom category gene '{gene.defName}' is{(inList ? "" : " NOT")} in target list.");
            }
            Log.Message($"[WorkerDronesMod] --- End custom category gene check ---");
        }
    }
}

