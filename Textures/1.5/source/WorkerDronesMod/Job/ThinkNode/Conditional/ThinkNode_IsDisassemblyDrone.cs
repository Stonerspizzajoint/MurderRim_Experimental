using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    // Inherit from ThinkNode_Conditional so this node can be used as a conditional in your AI trees.
    public class ThinkNode_IsDisassemblyDrone : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn == null || pawn.genes == null)
            {
                return false;
            }

            // List of required gene def names.
            string[] requiredGenes =
            {
                "MD_WeakenedSolver",
                "MD_HeatScale",
                "MD_MurderDroneBody"
            };

            // Check if the pawn has every gene in the list.
            foreach (string geneDefName in requiredGenes)
            {
                bool geneFound = false;
                foreach (var gene in pawn.genes.GenesListForReading)
                {
                    if (gene.def.defName == geneDefName)
                    {
                        geneFound = true;
                        break;
                    }
                }
                if (!geneFound)
                {
                    return false;
                }
            }
            return true;
        }
    }

}

