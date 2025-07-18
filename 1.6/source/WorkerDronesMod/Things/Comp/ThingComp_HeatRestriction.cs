using Verse;

namespace WorkerDronesMod
{
    // ThingComp that prevents weapon usage if the user's heat is too high.
    public class ThingComp_HeatRestriction : ThingComp
    {
        public CompProperties_HeatRestriction Props => (CompProperties_HeatRestriction)props;

        /// <summary>
        /// Gets a value indicating whether the pawn's current heat is above the allowed threshold.
        /// </summary>
        public bool IsHeatTooHigh(Pawn shooter)
        {
            if (shooter == null)
                return false;

            // Prefer Gene_BasicSolver if present
            var basicSolverGene = shooter.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (basicSolverGene != null)
            {
                float currentHeatFraction = basicSolverGene.Heat / basicSolverGene.InitialResourceMax;
                return currentHeatFraction > Props.maxAllowedHeat;
            }

            // If no heat data exists, consider it not too high.
            return false;
        }

        /// <summary>
        /// Checks whether the shooter is allowed to use the weapon.
        /// </summary>
        public bool CanShoot(Pawn shooter)
        {
            // If heat is too high, return false.
            return !IsHeatTooHigh(shooter);
        }
    }
}
