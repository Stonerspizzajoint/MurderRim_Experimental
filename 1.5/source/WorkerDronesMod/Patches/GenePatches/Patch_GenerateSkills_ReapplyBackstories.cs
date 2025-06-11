using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using VREAndroids;
using System;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(PawnGenerator), "GenerateSkills")]
    public static class Patch_GenerateSkills_ReapplyBackstories
    {
        static void Postfix(Pawn pawn, PawnGenerationRequest request)
        {
            // Defensive checks to prevent early-world-gen NREs
            if (pawn?.genes == null || pawn.story == null || pawn.skills == null || pawn.story.traits == null)
                return;

            // Only affect awakened androids
            if (!pawn.genes.GetGene(VREA_DefOf.VREA_SyntheticBody)?.Active ?? true)
                return;

            if (!pawn.IsAwakened())
                return;

            try
            {
                // 1) Gather allowed spawnCategories from our gene extension
                var allowedCats = pawn.genes.GenesListForReading
                    .Select(g => g.def.GetModExtension<GeneBackstoryFilterExtension>())
                    .Where(ext => ext?.allowedSpawnCategories?.Any() == true)
                    .SelectMany(ext => ext.allowedSpawnCategories)
                    .Distinct()
                    .ToList();
                if (!allowedCats.Any()) return;

                // 2) Pick & assign backstories
                BackstoryDef Pick(BackstorySlot slot) =>
                    DefDatabase<BackstoryDef>.AllDefsListForReading
                        .Where(bs => bs.slot == slot && bs.spawnCategories?.Intersect(allowedCats).Any() == true)
                        .ToList()
                        .RandomElement();

                pawn.story.Childhood = Pick(BackstorySlot.Childhood) ?? pawn.story.Childhood;

                // 3) Apply forced traits
                ApplyForcedTraits(pawn);

                // 4) Apply skill gains & passions
                ApplyBackstorySkillsAndPassions(pawn, request);
            }
            catch (Exception ex)
            {
                Log.ErrorOnce(
                    $"[WorkerDronesMod] Error in Patch_GenerateSkills_ReapplyBackstories: {ex}",
                    57382611
                );
            }
        }


        private static void ApplyForcedTraits(Pawn pawn)
        {
            var traits = pawn.story.traits;

            foreach (var bs in pawn.story.AllBackstories.Where(b => b != null))
            {
                if (bs.forcedTraits == null) continue;

                foreach (var entry in bs.forcedTraits)
                {
                    // only add if not already present
                    if (!traits.allTraits.Any(t => t.def == entry.def))
                        traits.GainTrait(new Trait(entry.def, entry.degree));
                }
            }
        }

        private static void ApplyBackstorySkillsAndPassions(Pawn pawn, PawnGenerationRequest request)
        {
            // reset all to zero first
            foreach (var skill in pawn.skills.skills)
            {
                skill.Level = 0;
                skill.passion = Passion.None;
            }

            // sum up skillGains from backstories & traits & kind
            foreach (var skill in pawn.skills.skills)
            {
                float total = 0f;

                // backstory gains
                foreach (var bs in pawn.story.AllBackstories.Where(b => b != null))
                    total += bs.skillGains
                              .Where(g => g.skill == skill.def)
                              .Sum(g => g.amount);

                // trait gains
                foreach (var tr in pawn.story.traits.allTraits.Where(t => !t.Suppressed))
                {
                    var gain = tr.CurrentData.skillGains.FirstOrDefault(g => g.skill == skill.def);
                    if (gain != null) total += gain.amount;
                }

                // extra from kindDef
                if (total > 0f) total += pawn.kindDef.extraSkillLevels;

                skill.Level = Mathf.Clamp(Mathf.RoundToInt(total), 0, 20);
            }

            // example passion assignment
            int majors = 2, minors = 2;
            foreach (var skill in pawn.skills.skills.OrderByDescending(s => s.Level))
            {
                if (skill.TotallyDisabled) continue;
                if (majors-- > 0) skill.passion = Passion.Major;
                else if (minors-- > 0) skill.passion = Passion.Minor;
                else skill.passion = Passion.None;
            }

            pawn.Notify_DisabledWorkTypesChanged();
        }
    }
}




