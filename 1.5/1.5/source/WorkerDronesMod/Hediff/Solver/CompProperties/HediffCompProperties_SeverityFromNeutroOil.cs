using Verse;

namespace WorkerDronesMod
{
    /// <summary>
    /// XML-configurable properties for the SeverityFromNeutroOil hediff component.
    /// </summary>
    public class HediffCompProperties_SeverityFromNeutroOil : HediffCompProperties
    {
        /// <summary>
        /// Severity increase per hour when the Neutro Oil level is below 50%.
        /// </summary>
        public float severityPerHourBelow50 = 1.0f;

        /// <summary>
        /// Severity decrease per hour when the Neutro Oil level is above 50%.
        /// </summary>
        public float severityPerHourAbove50 = 0.5f;

        public HediffCompProperties_SeverityFromNeutroOil()
        {
            compClass = typeof(HediffComp_SeverityFromNeutroOil);
        }
    }
}

