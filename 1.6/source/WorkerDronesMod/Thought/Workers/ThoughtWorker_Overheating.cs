using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class ThoughtWorker_Overheating : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            // Check for the gene
            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene == null)
                return ThoughtState.Inactive;

            // Activate if overheating
            return HeatUtil.IsOverheating(gene.Heat, gene.InitialResourceMax)
                ? ThoughtState.ActiveDefault
                : ThoughtState.Inactive;
        }
    }
}
