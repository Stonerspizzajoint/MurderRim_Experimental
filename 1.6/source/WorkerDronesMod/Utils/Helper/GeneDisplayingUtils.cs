using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WorkerDronesMod
{
    /// <summary>
    /// Utility for gene display and validation logic.
    /// Use IsGeneAllowed for all gene selection/validation checks.
    /// </summary>
    public static class GeneDisplayingUtils
    {
        /// <summary>
        /// Returns true if the gene is allowed (its required research is finished, and it isn't blocked).
        /// </summary>
        public static bool IsGeneAllowed(GeneDef gene)
        {
            // 1) Research requirement
            var researchExt = gene.GetModExtension<GeneResearchExtension>();
            if (researchExt != null && researchExt.requiredResearch != null && !researchExt.requiredResearch.IsFinished)
                return false;

            // 2) Block-from-window or block-gene requirement
            var blockExt = gene.GetModExtension<BlockFromAndroidWindowExtension>();
            if (blockExt != null && (blockExt.blockFromAndroidWindow || blockExt.blockGene))
                return false;

            return true;
        }

        public static bool IsGeneAllowedForBase(GeneDef gene)
        {
            var blockExt = gene.GetModExtension<BlockFromAndroidWindowExtension>();
            if (blockExt != null && blockExt.blockGene)
                return false;
            return true;
        }


        /// <summary>
        /// Helper to retrieve the correct Enumerable.Where method for GeneDef.
        /// </summary>
        public static MethodInfo GetWhereMethod()
        {
            var candidate = typeof(Enumerable)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Where" &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[1].ParameterType.IsGenericType &&
                    m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>));
            if (candidate == null)
                throw new InvalidOperationException("Could not find Enumerable.Where<TSource>(IEnumerable<TSource>, Func<TSource,bool>)");
            return candidate.MakeGenericMethod(typeof(GeneDef));
        }

        /// <summary>
        /// Returns a list of blocked genes from a given list.
        /// </summary>
        public static List<GeneDef> GetBlockedGenes(IEnumerable<GeneDef> genes)
        {
            return genes.Where(g => !IsGeneAllowed(g)).ToList();
        }

        /// <summary>
        /// Returns a human-readable reason why a gene is blocked.
        /// </summary>
        public static string GetBlockReason(GeneDef gene)
        {
            var researchExt = gene.GetModExtension<GeneResearchExtension>();
            var blockExt = gene.GetModExtension<BlockFromAndroidWindowExtension>();
            List<string> reasons = new List<string>();
            if (researchExt != null && researchExt.requiredResearch != null && !researchExt.requiredResearch.IsFinished)
                reasons.Add($"Requires research: {researchExt.requiredResearch.LabelCap}");
            if (blockExt != null && blockExt.blockFromAndroidWindow)
                reasons.Add("Blocked from use in this window");
            if (blockExt != null && blockExt.blockGene)
                reasons.Add("Blocked from all windows");
            return string.Join("; ", reasons);
        }
    }
}
