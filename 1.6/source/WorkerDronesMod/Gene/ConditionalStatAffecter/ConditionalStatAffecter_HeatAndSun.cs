using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class ConditionalStatAffecter_HeatAndSun : ConditionalStatAffecter
    {
        public override bool Applies(StatRequest req)
        {
            // Only applies to pawns
            Pawn pawn = req.Thing as Pawn;
            if (pawn == null)
                return false;

            // If sufficiently covered, do not apply the stat effect
            if (SolarUtil.IsSufficientlyCovered(pawn))
                return false;

            // Try to get the gene (assumes only one per pawn)
            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene == null)
                return false;

            var ext = gene.def.GetModExtension<SolverGeneExtension>();
            if (ext == null)
                return false;

            // Apply if in true sunlight or overheating
            return SolarUtil.InTrueSunlight(pawn) || HeatUtil.IsOverheating(gene.Heat, gene.InitialResourceMax);
        }

        public override string Label => "Heat & Sun";
    }
}
