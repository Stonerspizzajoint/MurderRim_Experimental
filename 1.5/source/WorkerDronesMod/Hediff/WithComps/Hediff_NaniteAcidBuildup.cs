using System;
using System.Linq;
using RimWorld;
using UnityEngine;  // For Mathf and Rand
using Verse;

namespace WorkerDronesMod
{
    public class Hediff_NaniteAcidBuildup : HediffWithComps
    {
        private int baseTickMax = 64;
        private int tickCounter = 0;
        private float splitChance = 0.5f;
        private int maxAffectedParts = 0;

        public override void Tick()
        {
            base.Tick();

            if (pawn == null || pawn.health == null)
                return;

            tickCounter++;

            float effectiveSeverity = Mathf.Clamp(this.Severity, 0.1f, 1f);
            int effectiveTickMax = (int)(baseTickMax / effectiveSeverity);

            if (tickCounter > effectiveTickMax)
            {
                // Only apply damage logic if the pawn is on a map
                if (pawn.Map == null)
                {
                    tickCounter = 0;
                    return;
                }

                BodyPartRecord originPart = null;
                Hediff acidSting = pawn.health.hediffSet.hediffs.FirstOrDefault(
                    h => h.def == MD_DefOf.MD_NaniteAcidSting);
                if (acidSting != null && acidSting.Part != null)
                    originPart = acidSting.Part;
                else
                {
                    Hediff acidBurn = pawn.health.hediffSet.hediffs
                        .Where(h => h.def == MD_DefOf.MD_NaniteAcidBurn)
                        .RandomElementWithFallback(null);
                    if (acidBurn != null && acidBurn.Part != null)
                        originPart = acidBurn.Part;
                    else
                    {
                        var intactParts = pawn.health.hediffSet.GetNotMissingParts();
                        if (intactParts.Any())
                            originPart = intactParts.RandomElement();
                        else
                        {
                            tickCounter = 0;
                            return;
                        }
                    }
                }

                if (originPart != null && originPart.def.defName == "Waist" && originPart.parent != null)
                    originPart = originPart.parent;

                bool isOriginDestroyed = false;
                if (originPart != null)
                {
                    if (originPart.def == MD_DefOf.Torso)
                    {
                        float torsoHealth = pawn.health.hediffSet.GetPartHealth(originPart);
                        isOriginDestroyed = torsoHealth <= 0f;
                    }
                    else
                    {
                        isOriginDestroyed = !pawn.health.hediffSet.GetNotMissingParts().Contains(originPart);
                    }
                }

                float GetFinalDamage(BodyPartRecord target, float baseDamage)
                {
                    float finalDamage = baseDamage;
                    Hediff burnHediff = pawn.health.hediffSet.hediffs
                        .FirstOrDefault(h => h.def == MD_DefOf.MD_NaniteAcidBurn && h.Part == target);
                    if (burnHediff != null)
                        finalDamage *= burnHediff.Severity;
                    return finalDamage;
                }

                if (originPart != null && originPart.def == MD_DefOf.Torso)
                {
                    var innerCandidates = new System.Collections.Generic.List<BodyPartRecord>();
                    if (originPart.parts != null)
                    {
                        foreach (BodyPartRecord child in originPart.parts)
                        {
                            if (pawn.health.hediffSet.GetNotMissingParts().Contains(child) &&
                                child.def.defName != "Waist")
                                innerCandidates.Add(child);
                        }
                    }
                    if (innerCandidates.Any())
                    {
                        var partsToDamage = new System.Collections.Generic.List<BodyPartRecord>();
                        BodyPartRecord primaryCandidate = innerCandidates.RandomElement();
                        partsToDamage.Add(primaryCandidate);
                        if (innerCandidates.Count > 1 && Rand.Chance(splitChance))
                        {
                            var remaining = innerCandidates.Where(x => x != primaryCandidate).ToList();
                            BodyPartRecord secondaryCandidate = remaining.RandomElement();
                            partsToDamage.Add(secondaryCandidate);
                        }
                        if (maxAffectedParts > 0 && partsToDamage.Count > maxAffectedParts)
                            partsToDamage = partsToDamage.Take(maxAffectedParts).ToList();

                        foreach (var part in partsToDamage)
                        {
                            float damage = GetFinalDamage(part, effectiveSeverity);
                            DamageInfo dinfo = new DamageInfo(
                                MD_DefOf.MD_NaniteAcid,
                                damage,
                                0f,
                                -1f,
                                null,
                                part,
                                null,
                                DamageInfo.SourceCategory.ThingOrUnknown,
                                null,
                                true,
                                true,
                                QualityCategory.Normal,
                                true);
                            pawn.TakeDamage(dinfo);
                        }
                    }
                }
                else
                {
                    float finalDamage = GetFinalDamage(originPart, effectiveSeverity);
                    float currentHealth = pawn.health.hediffSet.GetPartHealth(originPart);

                    if (!isOriginDestroyed)
                    {
                        if (finalDamage >= currentHealth)
                        {
                            if (originPart.parent != null &&
                                pawn.health.hediffSet.GetNotMissingParts().Contains(originPart.parent) &&
                                originPart.parent.def.defName != "Waist")
                            {
                                float spreadDamage = GetFinalDamage(originPart.parent, effectiveSeverity);
                                DamageInfo spreadDinfo = new DamageInfo(
                                    MD_DefOf.MD_NaniteAcid,
                                    spreadDamage,
                                    0f,
                                    -1f,
                                    null,
                                    originPart.parent,
                                    null,
                                    DamageInfo.SourceCategory.ThingOrUnknown,
                                    null,
                                    true,
                                    true,
                                    QualityCategory.Normal,
                                    true);
                                pawn.TakeDamage(spreadDinfo);
                            }
                        }
                        DamageInfo dinfo = new DamageInfo(
                            MD_DefOf.MD_NaniteAcid,
                            finalDamage,
                            0f,
                            -1f,
                            null,
                            originPart,
                            null,
                            DamageInfo.SourceCategory.ThingOrUnknown,
                            null,
                            true,
                            true,
                            QualityCategory.Normal,
                            true);
                        pawn.TakeDamage(dinfo);
                    }
                    else
                    {
                        BodyPartRecord candidate = null;
                        if (originPart.parent != null &&
                            pawn.health.hediffSet.GetNotMissingParts().Contains(originPart.parent) &&
                            originPart.parent.def.defName != "Waist")
                        {
                            candidate = originPart.parent;
                        }
                        if (maxAffectedParts > 0 && maxAffectedParts < 1)
                            candidate = null;
                        if (candidate != null)
                        {
                            float damage = GetFinalDamage(candidate, effectiveSeverity);
                            DamageInfo spreadDinfo = new DamageInfo(
                                MD_DefOf.MD_NaniteAcid,
                                damage,
                                0f,
                                -1f,
                                null,
                                candidate,
                                null,
                                DamageInfo.SourceCategory.ThingOrUnknown,
                                null,
                                true,
                                true,
                                QualityCategory.Normal,
                                true);
                            pawn.TakeDamage(spreadDinfo);
                        }
                    }
                }
                tickCounter = 0;
            }
        }
    }
}
