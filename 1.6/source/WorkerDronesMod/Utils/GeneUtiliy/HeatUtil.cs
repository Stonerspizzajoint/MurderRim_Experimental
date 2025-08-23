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
        private static List<BodyPartRecord> tempParts = new List<BodyPartRecord>();
        private static List<BodyPartRecord> tempSelectedParts = new List<BodyPartRecord>();

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

            // --- Only apply damage if in sunlight OR has injury/missing part ---
            bool inSunlight = SolarUtil.InTrueSunlight(pawn);
            bool hasInjuryOrMissingPart = false;

            // Check for missing part or injury
            var hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                var h = hediffs[i];
                if (h is Hediff_MissingPart || h is Hediff_Injury)
                {
                    hasInjuryOrMissingPart = true;
                    break;
                }
            }

            if ((inSunlight || hasInjuryOrMissingPart) && currentTick - lastTick >= DamageIntervalTicks)
            {
                lastDamageTick[gene] = currentTick;

                float damageAmount = (gene.Oil <= 0f) ? 30f : (gene.ext != null ? gene.ext.heatOptions.burnDamageAmmount : 2.0f);

                // Use static list to avoid allocations
                tempParts.Clear();
                tempParts.AddRange(pawn.health.hediffSet.GetNotMissingParts());

                int numParts = Rand.RangeInclusive(1, Math.Min(5, tempParts.Count));
                tempSelectedParts.Clear();
                for (int i = 0; i < numParts && tempParts.Count > 0; i++)
                {
                    int idx = Rand.Range(0, tempParts.Count);
                    tempSelectedParts.Add(tempParts[idx]);
                    tempParts.RemoveAt(idx);
                }

                foreach (var part in tempSelectedParts)
                {
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, damageAmount, 0, -1, null, part);
                    pawn.TakeDamage(dinfo);
                }

                // Chance to catch fire (e.g., 2% per interval)
                if (Rand.Value < 0.02f && !pawn.IsBurning())
                {
                    FireUtility.TryAttachFire(pawn, 0.15f, pawn);
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
        /// Adds heat to a pawn's Gene_BasicSolver, applying the HeatGainMultiplier stat.
        /// </summary>
        /// <param name="pawn">The pawn to add heat to.</param>
        /// <param name="baseHeat">The base heat to add.</param>
        /// <param name="ext">The SolverGeneExtension (optional, will auto-detect if null).</param>
        /// <param name="heatCanBeMultiplied">[Obsolete] Ignored, multiplier is always applied via stat.</param>
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

            // Always apply HeatGainMultiplier stat using MD_DefOf
            float heatGainMultiplier = pawn.GetStatValue(MD_DefOf.MD_HeatGainMultiplier, true);
            finalHeat *= heatGainMultiplier;

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

            // Apply SolarHeatMultiplier stat
            float solarHeatMultiplier = pawn.GetStatValue(MD_DefOf.MD_SolarHeatMultiplier, true);
            gain *= solarHeatMultiplier;

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
