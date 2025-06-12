using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    public class Gene_NeutroamineOil : Gene_Resource
    {
        #region Debug Gizmos

        public override IEnumerable<Gizmo> GetGizmos()
        {
            // Yield any base gizmos first.
            foreach (var gizmo in base.GetGizmos())
                yield return gizmo;

            if (!DebugSettings.godMode)
                yield break;

            // Debug: Add 10 Oil
            yield return new Command_Action
            {
                defaultLabel = "DEBUG: Add 10 Oil",
                action = () =>
                {
                    Value = Mathf.Min(Value + 10f, InitialResourceMax);
                    Log.Message($"DEBUG: Added 10 oil to {pawn.LabelShort}. New Oil: {Value}");
                }
            };

            // Debug: Remove 10 Oil
            yield return new Command_Action
            {
                defaultLabel = "DEBUG: Remove 10 Oil",
                action = () =>
                {
                    Value = Mathf.Max(Value - 10f, 0f);
                    Log.Message($"DEBUG: Removed 10 oil from {pawn.LabelShort}. New Oil: {Value}");
                }
            };

            // Debug: Add 5 Heat
            yield return new Command_Action
            {
                defaultLabel = "DEBUG: Add 5 Heat",
                action = () =>
                {
                    if (linkedHeatGene != null)
                    {
                        linkedHeatGene.IncreaseHeat(5f);
                        Log.Message($"DEBUG: Added 5 heat to {pawn.LabelShort}. New Heat: {linkedHeatGene.Value}");
                    }
                    else
                    {
                        Log.Warning($"DEBUG: No linked heat gene found on {pawn.LabelShort}.");
                    }
                }
            };

            // Debug: Remove 5 Heat
            yield return new Command_Action
            {
                defaultLabel = "DEBUG: Remove 5 Heat",
                action = () =>
                {
                    if (linkedHeatGene != null)
                    {
                        linkedHeatGene.ReduceHeat(5f);
                        Log.Message($"DEBUG: Removed 5 heat from {pawn.LabelShort}. New Heat: {linkedHeatGene.Value}");
                    }
                    else
                    {
                        Log.Warning($"DEBUG: No linked heat gene found on {pawn.LabelShort}.");
                    }
                }
            };
        }

        #endregion

        #region Fields & Properties

        // Indicates whether the gene is active.
        public bool HasSolver { get; private set; } = false;

        public string texPath = "UI/Icons/Medical/Neutroamine_Icon";
        public bool neutroamineAllowed = true;

        public float TargetValue
        {
            get => targetValue;
            set => targetValue = value;
        }

        // Returns the ability’s own heat value (from the linked heat gene).
        public float OwnHeat => linkedHeatGene != null ? linkedHeatGene.Value : 0f;

        public Gene_HeatBuildup linkedHeatGene;
        public bool autoShelter = true;

        // Temporary efficiency bonus.
        private float efficiencyBonusMultiplier = 1f;
        private int efficiencyBonusTicksRemaining = 0;

        // Internal counters.
        private int sunWarningTickCounter = 0;
        private int overheatingExposureTicks = 0;

        #endregion

        #region Other Settings

        // This constant is used for wound healing tick amount.
        private float RegenTickAmount => 0.024f;

        // Delegate status checks to the utility.
        public bool IsOverheating() => SolverGeneUtility.IsOverheating(pawn);
        public bool IsNaniteAcidBuildupActive() => SolverGeneUtility.IsNaniteAcidBuildupActive(pawn);
        public bool IsACoreHeart => SolverGeneUtility.IsACoreHeart(pawn);
        public bool IsSafeFromSun
        {
            get { return SolverGeneUtility.IsSafeFromSun(pawn); }
        }

        #endregion

        #region ModExtension Settings (Heat-based)

        private HeatControlData HeatControl => def.GetModExtension<SolverRegenerationModExtension>()?.HeatControl;
        private float OilCoolingCost => HeatControl?.oilCoolingCost ?? 0.0072f;
        private float ActiveCoolingMultiplier => HeatControl?.activeCoolingMultiplier ?? 6.25f;
        private float HeatPerSeverityPoint => HeatControl?.heatPerSeverityPoint ?? 0.003f;
        private float SunHeatGainRate => HeatControl?.sunHeatGainRate ?? 0.1f;
        private int SunWarningInterval => HeatControl?.sunWarningInterval ?? 1000;
        private float OverheatingThresholdPercent => HeatControl?.overheatingThresholdPercent ?? 0.6f;
        private int OverheatingExposureThreshold => HeatControl?.overheatingExposureThreshold ?? 600;
        private float OilEmptyHeatMultiplier => HeatControl?.oilEmptyHeatMultiplier ?? 2.0f;

        #endregion

        #region Cached Definitions

        private static readonly ThingDef NeutroamineDef = MD_DefOf.Neutroamine;
        private static readonly JobDef JobRefuelWithNeutroamineDef = MD_DefOf.MD_Job_RefuelWithNeutroamine;
        private static readonly JobDef JobRefuelWithCorpseDef = MD_DefOf.MD_Job_RefuelWithCorpse;

        #endregion

        #region Main Update & Tick

        public override void Tick()
        {
            // (Optional:) If you want to stop all processes when paused, you could check here:
            // if (Find.TickManager.Paused) return;

            base.Tick();
            if (pawn == null || pawn.Map == null)
                return;

            // -------------------------------------------------------------------
            // Oil-level check.
            float oilFraction = Value / InitialResourceMax;
            if (oilFraction < 0.5f)
            {
                if (!pawn.health.hediffSet.HasHediff(MD_DefOf.MD_OilLoss, false))
                {
                    pawn.health.AddHediff(MD_DefOf.MD_OilLoss, null, null, null);
                    if (DebugSettings.godMode)
                        Log.Message($"[Gene_NeutroamineOil] Added MD_OilLoss to {pawn.LabelShort} (oil fraction: {oilFraction:F2}).");
                }
            }
            // -------------------------------------------------------------------

            // Apply bleeding oil consumption (this method now respects paused state).
            ApplyBleedingOilConsumption();

            // Retrieve the linked heat gene using the utility.
            linkedHeatGene = SolverGeneUtility.GetHeatGene(pawn);

            if (efficiencyBonusTicksRemaining > 0)
            {
                efficiencyBonusTicksRemaining--;
                if (efficiencyBonusTicksRemaining <= 0)
                    efficiencyBonusMultiplier = 1f;
            }

            HandleActiveCooling();
            HandleHealingAndRegeneration();
            HandleSunlightEffects();
            HandleOverheating();
            HandleAmbientHeatPush();
            HandleAutoSheltering();
            SolverGeneUtility.EnforceCriticalDamage(pawn);
            SolverGeneUtility.HasInterchangeableMelee(pawn);
            SolverGeneUtility.HasInterchangeableRanged(pawn);
        }

        #endregion

        #region Bleeding Oil Consumption

        /// <summary>
        /// Iterates over all injuries on the pawn and reduces the oil resource based
        /// on the total bleed rate. This method is assumed to be called once per tick.
        /// It now first checks if the game is paused, and if so, it returns without changing anything.
        /// </summary>
        private void ApplyBleedingOilConsumption()
        {
            // If the game is paused, don't drain oil.
            if (Find.TickManager.Paused)
                return;

            float totalBleedRate = 0f;
            foreach (Hediff_Injury injury in pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>())
            {
                // Skip injuries if the pawn is dead, the injury is tended, or if it is permanent.
                if (injury.pawn.Dead || injury.IsTended() || injury.IsPermanent())
                    continue;

                float rate = injury.Severity * injury.def.injuryProps.bleedRate;
                if (injury.Part != null)
                    rate *= injury.Part.def.bleedRate;

                totalBleedRate += rate;
            }

            // The consumption factor controls how much oil is consumed per unit bleed rate per tick.
            const float bleedOilConsumptionFactor = 0.002f;
            float oilConsumed = totalBleedRate * bleedOilConsumptionFactor;
            Value = Mathf.Max(Value - oilConsumed, 0f);
        }

        #endregion

        #region Cooling and Healing

        private void HandleActiveCooling()
        {
            // Check if we have enough oil and heat to operate.
            if (Value <= 0 || linkedHeatGene?.Value <= 0)
                return;

            float oilUsed = Mathf.Min(Value, OilCoolingCost);
            Value -= oilUsed;
            float heatRemoved = oilUsed * ActiveCoolingMultiplier * efficiencyBonusMultiplier;
            linkedHeatGene.ReduceHeat(heatRemoved);
        }

        #endregion

        #region Healing and Regeneration

        private void HandleHealingAndRegeneration()
        {
            // Preliminary checks.
            if (pawn.health.hediffSet.HasHediff(MD_DefOf.MD_DigitalLobotomy))
            {
                if (DebugSettings.godMode)
                    Log.Message($"[Gene_NeutroamineOil] Healing disabled: {pawn.LabelShort} is digitally lobotomized.");
                return;
            }

            // If oil is empty, do not perform healing.
            if (SolverGeneUtility.IsOilEmpty(pawn))
            {
                if (DebugSettings.godMode)
                    Log.Message($"[Gene_NeutroamineOil] Healing disabled: {pawn.LabelShort} has no oil.");
                return;
            }

            // Call wound healing only if there is at least one injury to heal.
            bool hasWounds = pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>().Any();
            if (hasWounds)
            {
                HandleWoundHealing();
            }

            // Call limb regeneration only if there's at least one missing limb (excluding stomach)
            // or if the reconstruction hediff is already applied.
            bool missingLimbExists = pawn.health.hediffSet.hediffs.OfType<Hediff_MissingPart>()
                .Any(h => h.Part != null && h.Part.def != MD_DefOf.Stomach);
            bool hasReconstruction = pawn.health.hediffSet.hediffs.Any(h => h.def == MD_DefOf.MD_RoboticReconstruction);

            if (missingLimbExists || hasReconstruction)
            {
                HandleLimbRegeneration();
            }
        }

        private void HandleWoundHealing()
        {
            // Get all current injuries.
            var allInjuries = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(injury => injury.Severity >= 0f)
                .ToList();

            if (!allInjuries.Any())
                return;

            // Retrieve the mod extension values for healing and delay parameters.
            var gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
            SolverRegenerationModExtension modExt = gene != null ? gene.def.GetModExtension<SolverRegenerationModExtension>() : null;

            // Use our newly renamed delay properties.
            int initialWoundDelay = modExt != null ? modExt.woundHealingWarmupTicks : 60;
            int additionalWoundDelay = modExt != null ? modExt.additionalWoundDamageWarmupTicks : 30;
            float regenSpeedMultiplier = modExt != null ? modExt.regenSpeedMultiplier : 10f;
            float minHealingFactor = modExt != null ? modExt.minHealingFactor : 0.2f;

            // Filter injuries: only include injuries for which the delay is complete.
            List<Hediff_Injury> readyInjuries = new List<Hediff_Injury>();
            foreach (Hediff_Injury injury in allInjuries)
            {
                if (SolverGeneUtility.IsInjuryReadyForHealing(pawn, injury, initialWoundDelay, additionalWoundDelay))
                {
                    readyInjuries.Add(injury);
                }
                else
                {
                    if (DebugSettings.godMode)
                        Log.Message($"[Gene_NeutroamineOil] Injury on {pawn.LabelShort} is still in delay period.");
                }
            }

            // If no injuries are ready, skip healing.
            if (!readyInjuries.Any())
                return;

            // Calculate healing factor based on whether the pawn is a CoreHeart.
            float healingFactor = (SolverGeneUtility.IsACoreHeart(pawn)) ? 0.25f : 1f;
            float effectiveRegen = RegenTickAmount * efficiencyBonusMultiplier * healingFactor;

            // Adjust effective regen if the pawn has healing affecting hediffs.
            if (SolverGeneUtility.HasHealingAffectingHediff(pawn))
            {
                // For example, reduce effective regeneration by 50%.
                effectiveRegen *= 0.5f;
                if (DebugSettings.godMode)
                    Log.Message($"[Gene_NeutroamineOil] Effective regen reduced due to overheating for {pawn.LabelShort}. New regen: {effectiveRegen}");
            }

            if (effectiveRegen > 0.0001f)
            {
                // Heal the injuries that are ready.
                RegenerationUtilities.RegenerateWounds(pawn, effectiveRegen, readyInjuries);

                float multiplier = (Value <= 0f) ? OilEmptyHeatMultiplier : 1f;
                float totalHeat = readyInjuries.Sum(i => i.Severity) * HeatPerSeverityPoint * multiplier;
                linkedHeatGene.IncreaseHeat(totalHeat);
            }
        }

        private void HandleLimbRegeneration()
        {
            // Check for critical missing organs. If a stomach is missing, kill the pawn.
            bool missingStomach = pawn.health.hediffSet.hediffs
                .Any(h => h is Hediff_MissingPart && h.Part != null && h.Part.def == MD_DefOf.Stomach);

            // Build a list of body parts that require regeneration.
            // This list will include:
            //   (a) missing parts (excluding stomach) and
            //   (b) parts that already have the reconstruction hediff present (and below full severity).
            List<BodyPartRecord> partsToRegenerate = new List<BodyPartRecord>();

            // (a) Add missing parts (except stomach).
            var missingParts = pawn.health.hediffSet.hediffs.OfType<Hediff_MissingPart>()
                .Where(h => h.Part != null && h.Part.def != MD_DefOf.Stomach)
                .Select(h => h.Part);
            partsToRegenerate.AddRange(missingParts);

            // (b) Add parts that already have the reconstruction hediff if their severity is below 1.
            HediffDef reconstructionDef = MD_DefOf.MD_RoboticReconstruction;
            var reconHediffs = pawn.health.hediffSet.hediffs
                .Where(h => h.def == reconstructionDef)
                .Where(h => h.Part != null && !pawn.health.hediffSet.PartIsMissing(h.Part) && h.Severity < 1f)
                .Select(h => h.Part);
            foreach (var part in reconHediffs)
            {
                if (!partsToRegenerate.Contains(part))
                    partsToRegenerate.Add(part);
            }

            if (!partsToRegenerate.Any())
            {
                if (DebugSettings.godMode)
                    Log.Message($"[HandleLimbRegeneration] No parts available for regeneration on {pawn.LabelShort}.");
                return;
            }

            // Retrieve delay parameters from your mod extension.
            var gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
            SolverRegenerationModExtension modExt = gene != null ? gene.def.GetModExtension<SolverRegenerationModExtension>() : null;
            int initialMissingDelay = modExt != null ? modExt.missingLimbWarmupTicks : 100;
            int additionalMissingDelay = modExt != null ? modExt.additionalDamageLimbDelayTicks : 50;

            // Filter by delay where appropriate:
            // For parts that are missing, check using your delay method.
            // For nonmissing parts (with the reconstruction hediff already in place), assume they are ready.
            List<BodyPartRecord> readyParts = new List<BodyPartRecord>();
            foreach (var part in partsToRegenerate)
            {
                // Check if this part is missing.
                var missingHediff = pawn.health.hediffSet.hediffs
                    .FirstOrDefault(h => h is Hediff_MissingPart m && m.Part == part) as Hediff_MissingPart;
                if (missingHediff != null)
                {
                    if (SolverGeneUtility.IsMissingPartReadyForRegeneration(pawn, missingHediff, initialMissingDelay, additionalMissingDelay))
                        readyParts.Add(part);
                }
                else
                {
                    // Otherwise, the part already has the reconstruction hediff.
                    readyParts.Add(part);
                }
            }

            if (!readyParts.Any())
            {
                if (DebugSettings.godMode)
                    Log.Message($"[HandleLimbRegeneration] No parts are ready for regeneration on {pawn.LabelShort}.");
                return;
            }

            if (linkedHeatGene == null)
                return;

            try
            {
                // Process regeneration for each ready part.
                foreach (var part in readyParts)
                {
                    RoboticLimbRegenerator.ProcessRegenerationForPart(pawn, part);
                }

                // If there's an efficiency bonus, process extra cycles.
                if (!SolverGeneUtility.IsACoreHeart(pawn))
                {
                    float extraCycles = efficiencyBonusMultiplier - 1f;
                    int fullExtraCycles = Mathf.FloorToInt(extraCycles);
                    float fractionalCycle = extraCycles - fullExtraCycles;
                    for (int i = 0; i < fullExtraCycles; i++)
                    {
                        foreach (var part in readyParts)
                        {
                            RoboticLimbRegenerator.ProcessRegenerationForPart(pawn, part);
                        }
                    }
                    if (Rand.Value < fractionalCycle)
                    {
                        foreach (var part in readyParts)
                        {
                            RoboticLimbRegenerator.ProcessRegenerationForPart(pawn, part);
                        }
                    }
                }

                float currentHeat = RoboticLimbRegenerator.CalculateHeatForPawn(pawn);
                float multiplier = (Value <= 0f) ? OilEmptyHeatMultiplier : 1f;
                linkedHeatGene.IncreaseHeat(currentHeat * multiplier);
            }
            catch (Exception ex)
            {
                Log.Error($"Limb regeneration error: {ex}");
            }
        }

        public void UseOilForLimbRegen()
        {
            RoboticLimbRegenerator.ProcessRegeneration(pawn);
            RoboticLimbRegenerator.ProcessRegeneration(pawn);
            RoboticLimbRegenerator.ProcessRegeneration(pawn);
        }

        public bool IsFullyHealed()
        {
            bool hasInjuries = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Any(injury => injury.Severity > 0.01f);
            bool hasMissingParts = pawn.health.hediffSet.hediffs
                .Any(hediff => hediff is Hediff_MissingPart);
            return !hasInjuries && !hasMissingParts;
        }

        #endregion

        #region Sunlight and Overheating

        private void HandleSunlightEffects()
        {
            // Ensure we are on a map; if not, do not apply sunlight effects.
            if (pawn.Map == null || pawn.Position.Roofed(pawn.Map))
            {
                sunWarningTickCounter = 0;
                return;
            }

            float skyGlow = pawn.Map.skyManager.CurSkyGlow;
            bool isSafeFromSun = SolverGeneUtility.IsSafeFromSun(pawn);

            if (!isSafeFromSun)
            {
                float multiplier = (Value <= 0f) ? OilEmptyHeatMultiplier : 1f;
                linkedHeatGene?.IncreaseHeat(SunHeatGainRate * multiplier);
                HandleSunlightWarnings();
            }
            else
            {
                sunWarningTickCounter = 0;
            }
        }

        private void HandleSunlightWarnings()
        {
            sunWarningTickCounter++;
            if (sunWarningTickCounter % SunWarningInterval == 0 && SolverGeneUtility.ShouldNotifyPlayer(pawn))
            {
                if (DebugSettings.godMode)
                    Messages.Message($"{pawn.LabelShort} is exposed to sunlight - internal heat rising!", MessageTypeDefOf.CautionInput);
            }
        }

        private void HandleOverheating()
        {
            if (pawn.Map == null || linkedHeatGene == null)
                return;

            float heatPercent = linkedHeatGene.Value / linkedHeatGene.InitialResourceMax;
            HediffDef overheatingDef = MD_DefOf.MD_Overheating;

            if (heatPercent >= 1.0f)
            {
                // Here the utility applies the overheating effect.
                SolverGeneUtility.ApplyOverheatingEffect(pawn, overheatingDef, heatPercent);
                return;
            }

            if (heatPercent > OverheatingThresholdPercent)
            {
                overheatingExposureTicks++;
                if (overheatingExposureTicks >= OverheatingExposureThreshold)
                    SolverGeneUtility.TryApplyOverheating(pawn, linkedHeatGene, heatPercent, overheatingDef, OverheatingThresholdPercent);
            }
            else
            {
                overheatingExposureTicks = 0;
            }
        }

        #endregion

        #region Ambient Heat and Auto Sheltering

        private void HandleAmbientHeatPush()
        {
            if (pawn.Map == null || linkedHeatGene == null)
                return;
            Room room = pawn.Position.GetRoom(pawn.Map);
            if (room == null || room.UsesOutdoorTemperature)
                return;

            float heatPercent = linkedHeatGene.Value / linkedHeatGene.InitialResourceMax;
            if (heatPercent <= 0.6f)
                return;

            float pushFactor = (heatPercent - 0.6f) / 0.4f;
            GenTemperature.PushHeat(pawn, 1f * pushFactor);
        }

        private void HandleAutoSheltering()
        {
            if (!autoShelter)
            {
                ReleaseFromShelterIfRestricted();
                return;
            }

            if (!pawn.IsColonistPlayerControlled || pawn.Map == null || pawn.Drafted)
                return;

            Area shelterArea = GetShelterArea();
            float skyGlow = pawn.Map.skyManager.CurSkyGlow;

            if (skyGlow > 0.5f)
                RestrictToShelter(shelterArea);
            else if (ShouldReleaseFromShelter(skyGlow))
                ReleaseFromShelter(shelterArea);
        }

        private Area GetShelterArea() =>
            pawn.Map.areaManager.AllAreas.FirstOrDefault(a => a.Label == "Shaded Shelter");

        private void RestrictToShelter(Area shelterArea)
        {
            if (shelterArea != null && pawn.playerSettings.AreaRestrictionInPawnCurrentMap != shelterArea)
            {
                pawn.playerSettings.AreaRestrictionInPawnCurrentMap = shelterArea;
                Messages.Message($"{pawn.LabelShort} restricted to shelter due to sunlight.", MessageTypeDefOf.SilentInput);
            }
        }

        private bool ShouldReleaseFromShelter(float skyGlow) =>
            skyGlow <= 0.5f && pawn.playerSettings.AreaRestrictionInPawnCurrentMap?.Label == "Shaded Shelter";

        private void ReleaseFromShelter(Area shelterArea)
        {
            if (shelterArea != null && pawn.playerSettings.AreaRestrictionInPawnCurrentMap == shelterArea)
            {
                pawn.playerSettings.AreaRestrictionInPawnCurrentMap = null;
                Messages.Message($"{pawn.LabelShort} released from shelter.", MessageTypeDefOf.SilentInput);
            }
        }

        private void ReleaseFromShelterIfRestricted()
        {
            if (pawn.playerSettings.AreaRestrictionInPawnCurrentMap != null &&
                pawn.playerSettings.AreaRestrictionInPawnCurrentMap.Label == "Shaded Shelter")
            {
                pawn.playerSettings.AreaRestrictionInPawnCurrentMap = null;
                Messages.Message($"{pawn.LabelShort} released from shelter due to autoShelter being toggled off.", MessageTypeDefOf.SilentInput);
            }
        }

        #endregion

        #region PostAdd & PostRemove

        public override void PostAdd()
        {
            base.PostAdd();
            HasSolver = true;
            if (MD_DefOf.HD_HeatDamageMonitor != null &&
                !pawn.health.hediffSet.hediffs.Any(h => h.def == MD_DefOf.HD_HeatDamageMonitor))
            {
                pawn.health.AddHediff(MD_DefOf.HD_HeatDamageMonitor);
                if (DebugSettings.godMode)
                    Log.Message($"[Gene_NeutroamineOil] Added heat damage monitor hediff to {pawn.LabelShort}.");
            }
        }

        public override void PostRemove()
        {
            base.PostRemove();
            HasSolver = false;
        }

        #endregion

        #region Resource Overrides

        public override float InitialResourceMax => 100f;
        protected override Color BarColor => new Color(0.2f, 0.2f, 0.2f);
        protected override Color BarHighlightColor => Color.white;
        public override float MinLevelForAlert => InitialResourceMax * 0.1f;
        public override int PostProcessValue(float value) => Mathf.RoundToInt(value);

        public override void Reset()
        {
            base.Reset();
            Value = InitialResourceMax;
        }

        #endregion

        #region Efficiency Bonus Handling

        /// <summary>
        /// Applies a temporary efficiency bonus to the oil's cooling effect.
        /// </summary>
        /// <param name="bonusMultiplier">Multiplier to apply, e.g. 1.5 for 50% more efficiency.</param>
        /// <param name="durationTicks">Duration in ticks for the bonus.</param>
        public void ApplyEfficiencyBonus(float bonusMultiplier, int durationTicks)
        {
            efficiencyBonusMultiplier = bonusMultiplier;
            efficiencyBonusTicksRemaining = durationTicks;
            if (DebugSettings.godMode)
                Log.Message($"[Gene_NeutroamineOil] Efficiency bonus applied: {bonusMultiplier}x for {durationTicks} ticks.");
        }

        #endregion
    }
}