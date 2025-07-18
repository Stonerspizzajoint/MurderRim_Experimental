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
}
