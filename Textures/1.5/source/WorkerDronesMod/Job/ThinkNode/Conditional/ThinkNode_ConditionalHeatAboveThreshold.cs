using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    public class ThinkNode_ConditionalHeatAboveThreshold : ThinkNode_Conditional
    {
        /// <summary>
        /// The heat threshold value as a fraction (0-1). Default is 0.6 (i.e., 60%).
        /// This value is configurable via XML definitions or adjusted directly in code.
        /// </summary>
        public float heatThreshold = 0.6f;

        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn == null)
                return false;

            // Retrieve the pawn's heat gene.
            Gene_HeatBuildup heatGene = pawn.genes?.GetFirstGeneOfType<Gene_HeatBuildup>();
            if (heatGene == null || heatGene.InitialResourceMax <= 0f)
                return false;

            // Calculate the current heat as a fraction of the maximum.
            float heatPercent = heatGene.Value / heatGene.InitialResourceMax;

            // Return true if the heat exceeds the configurable threshold.
            return heatPercent > heatThreshold;
        }
    }
}
