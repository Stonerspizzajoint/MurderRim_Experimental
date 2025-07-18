using Verse;
using RimWorld;
using System.Linq;
using System;
using System.Collections.Generic;


namespace WorkerDronesMod
{
    public static class GeneDefHelper
    {
        /// <summary>
        /// Returns all non-null GeneDefs for a base/alt pair from DefOf (e.g., MD_DefOf.MD_DroneBody and MD_DefOf.VREA_MD_DroneBody).
        /// </summary>
        public static IEnumerable<GeneDef> GetGeneDefAndAlternative(GeneDef baseDef, GeneDef altDef)
        {
            if (baseDef != null)
                yield return baseDef;
            if (altDef != null && altDef != baseDef)
                yield return altDef;
        }

        /// <summary>
        /// Returns true if the pawn has any of the provided gene defs.
        /// </summary>
        public static bool PawnHasAnyGene(Pawn pawn, params GeneDef[] geneDefs)
        {
            if (pawn?.genes == null) return false;
            return pawn.genes.GenesListForReading.Any(g => geneDefs.Contains(g.def));
        }
    }
}

