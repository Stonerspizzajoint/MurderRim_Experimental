using System.Collections.Generic;
using Verse;

namespace WorkerDronesMod
{
    public class GenePrerequisitesExtension : DefModExtension
    {
        /// <summary>
        /// A list of gene defNames that are considered prerequisites.
        /// If at least one of these genes is present on a pawn,
        /// then the gene with this extension is deemed usable.
        /// </summary>
        public List<string> prerequisiteGeneDefNames;
    }
    public static class GenePrerequisitesValidator
    {
        /// <summary>
        /// Returns true if the given gene is usable on the pawn,
        /// that is, if it has no prerequisites or if at least one prerequisite is met.
        /// </summary>
        public static bool IsGeneUsable(GeneDef gene, Pawn pawn)
        {
            // Get the mod extension, if any.
            var prereqExt = gene.GetModExtension<GenePrerequisitesExtension>();
            if (prereqExt == null || prereqExt.prerequisiteGeneDefNames == null || prereqExt.prerequisiteGeneDefNames.Count == 0)
            {
                // No prerequisites given.
                return true;
            }

            // Check if any of the prerequisite genes is present on the pawn.
            bool atLeastOnePresent = pawn.genes.GenesListForReading
                .Any(g => prereqExt.prerequisiteGeneDefNames.Contains(g.def.defName));

            return atLeastOnePresent;
        }
    }

}
