using System;
using Verse;

namespace WorkerDronesMod
{
    /// <summary>
    /// This mod extension lets you override the default regeneration multipliers for wounds and armor.
    /// In XML you can attach this extension to a gene def (or other defs) to change the regeneration behavior.
    /// </summary>
    public class SolverRegenerationModExtension : DefModExtension
    {

        // This property groups all heat-related settings.
        public HeatControlData HeatControl;

        /// <summary>
        /// A multiplier that speeds up wound and armor regeneration.
        /// Increase this value to heal faster. Default is 10.0f.
        /// </summary>
        public float regenSpeedMultiplier = 10f;

        /// <summary>
        /// The minimum healing factor to ensure healing never slows below this fraction.
        /// Default is 0.2 (20% effectiveness).
        /// </summary>
        public float minHealingFactor = 0.2f;

        /// <summary>
        /// The number of ticks to wait (warmup) before wounds begin healing.
        /// Editable in XML. Default is 60 ticks.
        /// </summary>
        public int woundHealingWarmupTicks = 60;

        /// <summary>
        // Delay to wait if new injuries are added.
        ///<summary>
        public int additionalWoundDamageWarmupTicks = 30;

        public int missingLimbWarmupTicks = 100;
        public int additionalDamageLimbDelayTicks = 50;
    }

    [Serializable]
    public class HeatControlData
    {
        /// <summary>
        /// The amount of neutroamine oil consumed per tick when active cooling is applied.
        /// In other words, this value determines how much oil is "spent" to reduce internal heat during cooling.
        /// Default is 0.0072f.
        /// </summary>
        public float oilCoolingCost = 0.0072f;

        /// <summary>
        /// The multiplier that scales the heat reduction achieved by using the oil.
        /// The actual heat removed is calculated by multiplying the oil used by this factor.
        /// A higher value results in more efficient cooling.
        /// Default is 6.25f.
        /// </summary>
        public float activeCoolingMultiplier = 6.25f;

        /// <summary>
        /// Converts injury severity into additional heat.
        /// When injuries are healed, extra heat is generated based on the severity of injuries multiplied by this factor.
        /// Default is 0.003f.
        /// </summary>
        public float heatPerSeverityPoint = 0.003f;

        /// <summary>
        /// The rate at which internal heat increases due to sunlight exposure, per tick.
        /// A higher value causes the pawn to accumulate heat faster when exposed to direct sunlight.
        /// Default is 0.1f.
        /// </summary>
        public float sunHeatGainRate = 0.1f;

        /// <summary>
        /// The interval (in ticks) at which a warning is issued to the player when the pawn is exposed to sunlight.
        /// Once this many ticks pass in sun exposure, a warning is displayed.
        /// Default is 1000.
        /// </summary>
        public int sunWarningInterval = 1000;

        /// <summary>
        /// The fraction of maximum heat above which the pawn is considered at risk of overheating.
        /// For example, a value of 0.6f means that once the pawn’s heat exceeds 60% of its maximum,
        /// the system starts tracking exposure for potential overheating.
        /// Default is 0.6f.
        /// </summary>
        public float overheatingThresholdPercent = 0.6f;

        /// <summary>
        /// The number of continuous ticks the pawn must remain over the overheating threshold
        /// before an overheating effect is triggered.
        /// Default is 600.
        /// </summary>
        public int overheatingExposureThreshold = 600;

        /// <summary>
        /// When the neutroamine oil resource is depleted the cooling effect is significantly reduced,
        /// causing heat to build up more quickly.
        /// This multiplier increases the rate of heat accumulation in that state.
        /// Default is 2.0f.
        /// </summary>
        public float oilEmptyHeatMultiplier = 2.0f;
    }
}
