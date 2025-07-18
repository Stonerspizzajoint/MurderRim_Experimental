using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WorkerDronesMod
{
    /// <summary>
    /// Defines configurable parameters for Disassembly Drone heat and oil mechanics.
    /// </summary>
    public class SolverGeneExtension : DefModExtension
    {
        // --- Heat options grouped ---
        public HeatOptions heatOptions = new HeatOptions();

        // --- Oil options ---
        public OilOptions oilOptions = new OilOptions();

        // --- Regen options
        public RegenOptions regenOptions = new RegenOptions();

        /// <summary>
        /// Factor controlling the rate of passive ambient cooling per tick.
        /// </summary>
        public float ambientCoolingFactor = 0.0005f;

        /// <summary>
        /// Maximum amount of heat that can be removed by ambient cooling in a single tick.
        /// </summary>
        public float maxAmbientCoolPerTick = 0.01f;

        /// <summary>
        /// Duration (in ticks) that a "solar sunscreen" buff protects the drone from sunlight heat.
        /// </summary>
        public float solarSunscreenDurationTicks = 60000;

        ///<summary>
        ///Determines if This Version of the solver is nerfed or not, meaning if it can level up and gain Solver Abilities.
        ///</summary>
        public bool isNerfedSolver = true;

        public class RegenOptions
        {
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
            public float baseSeverityPerTick = 0.015f;
            public float baseHeatPerCoverage = 0.28f;
            public float severityHeatFactor = 0.1f;

            public void ExposeData()
            {
                Scribe_Values.Look(ref regenSpeedMultiplier, "regenSpeedMultiplier");
                Scribe_Values.Look(ref minHealingFactor, "minHealingFactor");
                Scribe_Values.Look(ref woundHealingWarmupTicks, "woundHealingWarmupTicks");
                Scribe_Values.Look(ref additionalWoundDamageWarmupTicks, "additionalWoundDamageWarmupTicks");
                Scribe_Values.Look(ref missingLimbWarmupTicks, "missingLimbWarmupTicks");
                Scribe_Values.Look(ref additionalDamageLimbDelayTicks, "additionalDamageLimbDelayTicks");
                Scribe_Values.Look(ref baseSeverityPerTick, "baseSeverityPerTick");
                Scribe_Values.Look(ref baseHeatPerCoverage, "baseHeatPerCoverage");
                Scribe_Values.Look(ref severityHeatFactor, "severityHeatFactor");
            }

        }

        public class HeatOptions
        {
            /// <summary>
            /// Amount of heat gained per tick when the drone is exposed to a sun lamp.
            /// </summary>
            public float heatGainPerTickSun = 0.01f;

            /// <summary>
            /// Heat threshold below which the drone may suffer performance reduction (e.g., stiffness).
            /// </summary>
            public float minimumSafeHeat = 0.0f;

            /// <summary>
            /// Amount of heat increase corresponding to one severity point of an overheating hediff.
            /// </summary>
            public float heatPerSeverity = 0.1f;

            /// <summary>
            /// What the sky light level must be for it to be dangerous.
            /// </summary>
            public float dangrousSkyGlow = 0.6f;

            /// <summary>
            /// 
            /// </summary>
            public float burnDamageAmmount = 2.0f;

            /// <summary>
            /// The baseline temperature (in Celsius) for ambient cooling. Default is 21.
            /// </summary>
            public float ambientCoolingBaseline = 21f;

            public float headCoverSunlightFactor = 0.5f; // 0.5 = 50% sunlight heat if head is covered

            /// <summary>
            /// Multiplier applied to all heat gained from abilities. Default is 1.0 (no change).
            /// </summary>
            public float globalDefaultHeatMultiplier = 1.0f;

            public void ExposeData()
            {
                Scribe_Values.Look(ref heatGainPerTickSun, "heatGainPerTickSun");
                Scribe_Values.Look(ref minimumSafeHeat, "minimumSafeHeat");
                Scribe_Values.Look(ref heatPerSeverity, "heatPerSeverity");
                Scribe_Values.Look(ref dangrousSkyGlow, "dangrousSkyGlow");
                Scribe_Values.Look(ref burnDamageAmmount, "burnDamageAmmount");
                Scribe_Values.Look(ref ambientCoolingBaseline, "ambientCoolingBaseline");
                Scribe_Values.Look(ref headCoverSunlightFactor, "headCoverSunlightFactor");
                Scribe_Values.Look(ref globalDefaultHeatMultiplier, "globalDefaultHeatMultiplier");
            }
        }
        public class OilOptions
        {
            /// <summary>
            /// Oil consumed for each unit of heat to perform active cooling.
            /// </summary>
            public float oilUsePerHeatUnit = 0.1f;

            /// <summary>
            /// Amount of heat removed per unit of oil used.
            /// </summary>
            public float heatPerOil = 4f;

            /// <summary>
            /// Multiplier for oil craving speed (higher = craves oil more frequently). Default is 1.0.
            /// </summary>
            public float oilCravingSpeed = 1.0f;


            public void ExposeData()
            {
                Scribe_Values.Look(ref oilUsePerHeatUnit, "oilUsePerHeatUnit");
                Scribe_Values.Look(ref heatPerOil, "heatPerOil");
                Scribe_Values.Look(ref oilCravingSpeed, "oilCravingSpeed");
            }
        }

    }
}


