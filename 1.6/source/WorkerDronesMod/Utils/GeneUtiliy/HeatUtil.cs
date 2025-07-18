using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using VREAndroids;
using static System.Net.Mime.MediaTypeNames;

namespace WorkerDronesMod
{
    public static class HeatUtil
    {
        public static float CalculateAmbientDelta(Pawn pawn, float currentHeat, SolverGeneExtension ext)
        {
            if (currentHeat <= 0f) return 0f;
            float baseline = ext.heatOptions.ambientCoolingBaseline;
            float diff = baseline - pawn.AmbientTemperature;
            if (diff <= 0f) return 0f;
            float cool = Mathf.Min(diff * ext.ambientCoolingFactor, ext.maxAmbientCoolPerTick);
            return -cool;
        }


        // Track genes that have shown warning to avoid spamming messages
        private static readonly HashSet<Gene_BasicSolver> warnedGenes = new HashSet<Gene_BasicSolver>();
        private static readonly Dictionary<Gene_BasicSolver, int> lastDamageTick = new Dictionary<Gene_BasicSolver, int>();
        private static readonly Dictionary<Gene_BasicSolver, Mote> overheatingMotes = new Dictionary<Gene_BasicSolver, Mote>();
        private const int DamageIntervalTicks = 250; // every 250 ticks (~4 seconds)

        public static void HandleOverheating(Gene_BasicSolver gene, Pawn pawn)
        {
            if (pawn == null || gene == null || !IsOverheating(gene.Heat, gene.InitialResourceMax))
                return;

            int currentTick = Find.TickManager.TicksGame;
            int lastTick;
            lastDamageTick.TryGetValue(gene, out lastTick);

            // Maintain or create the attached overheating mote
            if (!overheatingMotes.TryGetValue(gene, out var mote) || mote == null || mote.Destroyed)
            {
                mote = MoteMaker.MakeAttachedOverlay(pawn, MD_DefOf.MD_Mote_DroneOverHeating, Vector3.zero, 1f, -1f);
                overheatingMotes[gene] = mote;
            }
            else
            {
                mote.Maintain();
            }

            if (currentTick - lastTick >= DamageIntervalTicks)
            {
                lastDamageTick[gene] = currentTick;

                // Always deal burn damage when overheating
                float damageAmount = gene.ext != null ? gene.ext.heatOptions.burnDamageAmmount : 2.0f;
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, damageAmount);
                pawn.TakeDamage(dinfo);

                // Chance to catch fire (e.g., 2% per interval)
                if (Rand.Value < 0.02f)
                {
                    if (!pawn.IsBurning())
                    {
                        FireUtility.TryAttachFire(pawn, 0.15f, pawn);
                    }
                }
            }

            if (!warnedGenes.Contains(gene))
            {
                warnedGenes.Add(gene);
            }
        }



        // Call this when heat cools below threshold to reset warning state
        public static void ClearOverheatWarning(Gene_BasicSolver gene)
        {
            warnedGenes.Remove(gene);

            // Clean up the overheating mote if it exists
            if (overheatingMotes.TryGetValue(gene, out var mote) && mote != null && !mote.Destroyed)
            {
                mote.Destroy();
            }
            overheatingMotes.Remove(gene);
        }

        /// <summary>
        /// Adds heat to a pawn's Gene_BasicSolver, applying the global multiplier if allowed.
        /// </summary>
        /// <param name="pawn">The pawn to add heat to.</param>
        /// <param name="baseHeat">The base heat to add.</param>
        /// <param name="ext">The SolverGeneExtension (optional, will auto-detect if null).</param>
        /// <param name="heatCanBeMultiplied">If true, applies the global multiplier.</param>
        /// <returns>The final heat value added (after multiplier).</returns>
        public static float AddHeat(Pawn pawn, float baseHeat, SolverGeneExtension ext = null, bool? heatCanBeMultiplied = null)
        {
            if (pawn == null || baseHeat == 0f)
                return 0f;

            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene == null)
                return 0f;

            if (ext == null)
                ext = gene.ext ?? gene.def.GetModExtension<SolverGeneExtension>();

            // Apply ambient multiplier (neutral in vacuum)
            float ambientMultiplier = HeatAmbientMultiplier(pawn, pawn.AmbientTemperature, ext);
            float finalHeat = baseHeat * ambientMultiplier;

            // If not specified, default to IsSufficientlyCovered logic
            bool canMultiply = heatCanBeMultiplied ?? SolarUtil.IsSufficientlyCovered(pawn);

            if (canMultiply && ext != null)
                finalHeat *= ext.heatOptions.globalDefaultHeatMultiplier;

            gene.Heat += finalHeat;
            return finalHeat;
        }

        public static float CalculateAndApplySolarHeatGain(Pawn pawn, SolverGeneExtension ext)
        {
            if (pawn == null || ext == null)
                return 0f;

            float gain = 0f;

            // Prevent heat gain from sunlight if mod setting is disabled
            if (SolarUtil.InTrueSunlight(pawn) && WorkerDronesMod.settings.heatGainInSunlight)
            {
                float ambientMultiplier = HeatAmbientMultiplier(pawn, pawn.AmbientTemperature);
                float baseSunGain = ext.heatOptions.heatGainPerTickSun * ambientMultiplier;

                if (!(SolarUtil.IsSufficientlyCovered(pawn)))
                {
                    if ((SolarUtil.IsHeadCovered(pawn)))
                    {
                        gain += baseSunGain * ext.heatOptions.headCoverSunlightFactor;
                    }
                    else
                    {
                        gain += baseSunGain;
                    }
                }
                // If sufficiently covered, no sunlight heat gain (do nothing)
            }

            // Extra heat gain if ambient temp is above comfy max (always applies)
            float comfyMax = pawn.GetStatValue(StatDefOf.ComfyTemperatureMax, true);
            float ambient = pawn.AmbientTemperature;
            if (ambient > comfyMax)
            {
                float overTemp = ambient - comfyMax;
                float extremeAmbientHeat = overTemp * 0.02f * ext.heatOptions.heatGainPerTickSun;
                gain += extremeAmbientHeat;
            }

            // Apply the calculated heat using the universal method
            return HeatUtil.AddHeat(pawn, gain, ext);
        }

        /// <summary>
        /// Returns true if currentHeat is above the configured minimum; always false if heat ≤ 0.
        /// </summary>
        public static bool IsAboveMinimumHeat(float currentHeat, float minimumSafeHeat)
        {
            if (currentHeat <= 0f)
                return false;
            return currentHeat > minimumSafeHeat;
        }

        public static bool IsOverheating(float currentHeat, float InitialResourceMax)
        {
            return currentHeat >= InitialResourceMax * 1.1f;
        }


        public static bool IsHeatAboveThreshold(float currentHeat, float thresholdPercentage)
        {
            if (currentHeat <= 0f)
                return false;
            return currentHeat >= thresholdPercentage;
        }

        /// <summary>
        /// Returns a multiplier for heat or effect based on ambient temperature.
        /// At 21°C, returns 1.0. Below 21°C, returns less than 1.0. Above 21°C, returns greater than 1.0.
        /// The scaleFactor controls how much the multiplier changes per degree.
        /// </summary>
        public static float HeatAmbientMultiplier(Pawn pawn, float ambientTemp, SolverGeneExtension ext = null)
        {
            if (IsPawnInVacuum(pawn))
                return 1f; // No ambient effect in vacuum

            if (ext == null)
            {
                var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
                ext = gene?.ext ?? gene?.def.GetModExtension<SolverGeneExtension>();
            }
            float scaleFactor = ext?.heatOptions.ambientHeatScale ?? 0.01f;
            float multiplier = 1f + (ambientTemp - 21f) * scaleFactor;
            return Mathf.Max(multiplier, 0.1f);
        }



        public static bool IsPawnInVacuum(Pawn pawn)
        {
            if (pawn?.Map == null)
                return false;

            // Odyssey/space mod check: is this a vacuum map?
            if (!pawn.Map.Biome.inVacuum)
                return false;

            // Is the pawn's cell in vacuum (not in an airtight room)?
            float vacuum = pawn.Position.GetVacuum(pawn.Map);
            if (vacuum < 0.5f)
                return false;

            Room room = pawn.GetRoom();
            if (room != null && VacuumUtility.IsRoomAirtight(room))
                return false; // In an airtight room

            return true; // In vacuum
        }
    }
}
