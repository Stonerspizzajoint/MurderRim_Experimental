using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using VFECore; // For SetStringTag/RemoveStringTag extensions.

namespace WorkerDronesMod
{
    public class Hediff_OverHeatingDamage : HediffWithComps
    {
        private int tickMax = 64;
        private int tickCounter = 0;
        private bool hasSetOnFire = false;

        // Base damage values (adjustable via XML or in code).
        public float initialFireDamage = 10f;
        public float ongoingBurnDamage = 2f;
        public float fireSize = 0.5f;

        // Controls how much extra damage is applied at high severity.
        // At low severity the damage multiplier is 1; at maximum severity it becomes 1 + severityImpact.
        public float severityImpact = 1f;

        // These settings control how many body parts are affected.
        // Scaling is done over the range [minSeverityForScaling, 1.0]:
        // At minSeverityForScaling the effect damages minPartsAffected part(s),
        // and at Severity = 1.0 it affects maxPartsAffected parts.
        public float minSeverityForScaling = 0.1f;
        public int minPartsAffected = 1;
        public int maxPartsAffected = 3;

        // These allow you to change the actual DamageDef used.
        public DamageDef initialDamageDef = MD_DefOf.MD_OverHeating;
        public DamageDef ongoingDamageDef = MD_DefOf.MD_OverHeating_Burn;

        public override void Tick()
        {
            base.Tick();

            // If pawn's heat is below 10%, remove the overheating hediff.
            if (pawn != null && pawn.genes != null)
            {
                Gene_HeatBuildup heatGene = pawn.genes.GetFirstGeneOfType<Gene_HeatBuildup>();
                if (heatGene != null)
                {
                    float heatPercent = heatGene.Value / heatGene.InitialResourceMax;
                    if (heatPercent < 0.1f)
                    {
                        pawn.RemoveStringTag("Overheating");
                        pawn.health.RemoveHediff(this);
                        return;
                    }
                }
            }

            // Update the "Overheating" tag.
            if (pawn != null)
            {
                pawn.SetStringTag("Overheating", "Overheating");
            }

            // Synchronize Severity with the pawn's internal heat.
            if (pawn != null && pawn.genes != null)
            {
                Gene_HeatBuildup heatGene = pawn.genes.GetFirstGeneOfType<Gene_HeatBuildup>();
                if (heatGene != null)
                {
                    Severity = heatGene.Value / heatGene.InitialResourceMax;
                }
            }

            // If Severity is high enough and fire hasn't been set, set the pawn on fire.
            if (Severity >= 0.9f && !hasSetOnFire)
            {
                SetPawnOnFire();
                hasSetOnFire = true;
            }

            // Apply periodic burn damage.
            tickCounter++;
            if (tickCounter > tickMax)
            {
                ApplyBurnDamage();
                tickCounter = 0;
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            if (pawn != null)
            {
                pawn.RemoveStringTag("Overheating");
            }
        }

        /// <summary>
        /// Computes a damage multiplier for burn damage.
        /// The multiplier scales from 1 at minSeverityForScaling up to 1 + severityImpact when Severity is 1.0.
        /// </summary>
        private float GetDamageMultiplier()
        {
            float clampedSeverity = Mathf.Clamp(Severity, minSeverityForScaling, 1f);
            float severityFactor = Mathf.InverseLerp(minSeverityForScaling, 1f, clampedSeverity);
            return 1f + severityImpact * severityFactor;
        }

        /// <summary>
        /// Determines the number of body parts to affect.
        /// The number scales linearly from minPartsAffected (at Severity = minSeverityForScaling)
        /// to maxPartsAffected (at Severity = 1.0).
        /// </summary>
        private int GetAffectedPartCount()
        {
            float clampedSeverity = Mathf.Clamp(Severity, minSeverityForScaling, 1f);
            float severityFactor = Mathf.InverseLerp(minSeverityForScaling, 1f, clampedSeverity);
            int partsAffected = Mathf.RoundToInt(Mathf.Lerp(minPartsAffected, maxPartsAffected, severityFactor));
            return Mathf.Max(minPartsAffected, partsAffected);
        }

        private void SetPawnOnFire()
        {
            // Apply initial fire damage scaled by the damage multiplier.
            float damageMultiplier = GetDamageMultiplier();
            float damage = initialFireDamage * damageMultiplier;

            // Start fire effect.
            if (pawn.Position.IsValid && pawn.Map != null)
            {
                FireUtility.TryStartFireIn(pawn.Position, pawn.Map, fireSize, pawn);
            }

            pawn.TakeDamage(new DamageInfo(
                initialDamageDef,
                damage,
                hitPart: null,
                instigator: null,
                weapon: null,
                category: DamageInfo.SourceCategory.ThingOrUnknown
            ));

            Messages.Message("{0} was set on fire by overheating components!".Translate(pawn.LabelShort),
                pawn, MessageTypeDefOf.ThreatSmall);
        }

        private void ApplyBurnDamage()
        {
            // Calculate ongoing burn damage for each affected part.
            float damageMultiplier = GetDamageMultiplier();
            float burnDamage = ongoingBurnDamage * damageMultiplier;

            // Determine how many parts should be affected this tick.
            int partsAffected = GetAffectedPartCount();

            // Get a list of non-missing body parts.
            // Here we exclude any part whose def name contains "Waist" (case-insensitive) so that the Waist part is not affected.
            var availableParts = pawn.health.hediffSet
                .GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null)
                .Where(part => !part.def.defName.ToLower().Contains("waist"))
                .ToList();

            // Shuffle the list randomly.
            availableParts = availableParts.OrderBy(x => UnityEngine.Random.value).ToList();

            // Make sure we don't exceed the number of available parts.
            partsAffected = Mathf.Min(partsAffected, availableParts.Count);

            // Apply burn damage to each selected body part.
            for (int i = 0; i < partsAffected; i++)
            {
                pawn.TakeDamage(new DamageInfo(
                    ongoingDamageDef,
                    burnDamage,
                    hitPart: availableParts[i],
                    instigator: null,
                    weapon: null,
                    category: DamageInfo.SourceCategory.ThingOrUnknown
                ));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickMax, "tickMax", 64);
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
            Scribe_Values.Look(ref hasSetOnFire, "hasSetOnFire", false);
            Scribe_Values.Look(ref initialFireDamage, "initialFireDamage", 10f);
            Scribe_Values.Look(ref ongoingBurnDamage, "ongoingBurnDamage", 10f);
            Scribe_Values.Look(ref fireSize, "fireSize", 0.5f);
            Scribe_Defs.Look(ref initialDamageDef, "initialDamageDef");
            Scribe_Defs.Look(ref ongoingDamageDef, "ongoingDamageDef");

            Scribe_Values.Look(ref severityImpact, "severityImpact", 1f);
            Scribe_Values.Look(ref minSeverityForScaling, "minSeverityForScaling", 0.1f);
            Scribe_Values.Look(ref minPartsAffected, "minPartsAffected", 1);
            Scribe_Values.Look(ref maxPartsAffected, "maxPartsAffected", 3);
        }
    }
}






