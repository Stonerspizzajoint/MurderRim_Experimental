using Verse;

namespace WorkerDronesMod
{
    public static class MD_Utils
    {
        // List of genes that mark a pawn as an android body
        public static readonly GeneDef[] AndroidBodyGenes = new[]
        {
        MD_DefOf.MD_DroneBody,
        MD_DefOf.MD_MurderDroneBody
        // Add more here as needed
    };

        public static bool IsDroneBody(Pawn pawn)
        {
            if (pawn?.genes == null) return false;
            foreach (var gene in AndroidBodyGenes)
            {
                if (pawn.genes.HasActiveGene(gene))
                    return true;
            }
            return false;
        }
    }
}
