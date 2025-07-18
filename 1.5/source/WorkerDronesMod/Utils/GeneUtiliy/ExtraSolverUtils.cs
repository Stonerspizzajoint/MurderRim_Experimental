using Verse;
using RimWorld;
using System.Security.Cryptography;

namespace WorkerDronesMod
{
    public static class ExtraSolverUtils
    {
        public static void HandleAutoSheltering(Gene_BasicSolver gene)
        {
            TrackLastNonShelterArea(gene);
            Pawn pawn = gene.pawn;
            if (!gene.RestrictToRoofedAreas)
            {
                ReleaseFromShelterIfRestricted(gene);
                return;
            }

            if (!pawn.IsColonistPlayerControlled || pawn.Map == null || pawn.Drafted)
                return;

            Area shelterArea = GetShelterArea(pawn);

            // Use the custom outside safety check
            var ext = gene.def.GetModExtension<SolverGeneExtension>();
            if (!SolarUtil.IsOutsideSafe(pawn, ext))
                RestrictToShelter(gene, shelterArea);
            else
                ReleaseFromShelter(gene, shelterArea);
        }

        public static void TrackLastNonShelterArea(Gene_BasicSolver gene)
        {
            Pawn pawn = gene.pawn;
            if (pawn?.playerSettings == null)
                return;

            Area current = pawn.playerSettings.AreaRestrictionInPawnCurrentMap;
            if (current != null && current.Label != "Shaded Shelter")
            {
                // Only update if it's different from the last tracked area
                if (gene.lastNonShelterArea != current)
                    gene.lastNonShelterArea = current;
            }
        }

        private static Area GetShelterArea(Pawn pawn)
        {
            if (pawn?.Map == null)
                return null;

            foreach (Area area in pawn.Map.areaManager.AllAreas)
            {
                if (area != null && area.Label == "Shaded Shelter")
                    return area;
            }
            return null;
        }

        private static void RestrictToShelter(Gene_BasicSolver gene, Area shelterArea)
        {
            Pawn pawn = gene.pawn;
            if (pawn.playerSettings == null || shelterArea == null)
                return;

            // Store the last non-shelter area if switching to shelter
            if (pawn.playerSettings.AreaRestrictionInPawnCurrentMap != shelterArea)
            {
                if (pawn.playerSettings.AreaRestrictionInPawnCurrentMap != null &&
                    pawn.playerSettings.AreaRestrictionInPawnCurrentMap.Label != "Shaded Shelter")
                {
                    gene.lastNonShelterArea = pawn.playerSettings.AreaRestrictionInPawnCurrentMap;
                }
                pawn.playerSettings.AreaRestrictionInPawnCurrentMap = shelterArea;
            }
        }

        private static void ReleaseFromShelter(Gene_BasicSolver gene, Area shelterArea)
        {
            Pawn pawn = gene.pawn;
            if (pawn.playerSettings == null)
                return;

            if (pawn.playerSettings.AreaRestrictionInPawnCurrentMap == shelterArea)
            {
                // Restore the last non-shelter area if it exists and is not the shelter
                if (gene.lastNonShelterArea != null && gene.lastNonShelterArea != shelterArea)
                {
                    pawn.playerSettings.AreaRestrictionInPawnCurrentMap = gene.lastNonShelterArea;
                }
                else
                {
                    pawn.playerSettings.AreaRestrictionInPawnCurrentMap = null;
                }
            }
        }

        private static void ReleaseFromShelterIfRestricted(Gene_BasicSolver gene)
        {
            Pawn pawn = gene.pawn;
            if (pawn.playerSettings == null)
                return;

            Area shelterArea = GetShelterArea(pawn);
            if (pawn.playerSettings.AreaRestrictionInPawnCurrentMap == shelterArea)
            {
                if (gene.lastNonShelterArea != null && gene.lastNonShelterArea != shelterArea)
                {
                    pawn.playerSettings.AreaRestrictionInPawnCurrentMap = gene.lastNonShelterArea;
                }
                else
                {
                    pawn.playerSettings.AreaRestrictionInPawnCurrentMap = null;
                }
            }
        }
    }
}

