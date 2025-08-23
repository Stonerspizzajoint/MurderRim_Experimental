using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace WorkerDronesMod
{
    public static class SolverTraitEffectManager
    {
        /// <summary>
        /// Syncs all SolverTrait effects: removes all, then applies all from current progress.
        /// Call this after any trait unlock/removal or on load.
        /// </summary>
        public static void SyncAllTraitEffects(Pawn pawn, SolverTraitProgress progress)
        {
            RemoveAllTraitEffects(pawn);
            ApplyAllTraitEffects(pawn, progress);
        }

        /// <summary>
        /// Applies all effects for currently unlocked traits.
        /// </summary>
        public static void ApplyAllTraitEffects(Pawn pawn, SolverTraitProgress progress)
        {
            if (pawn == null || progress == null) return;

            foreach (string traitDefName in progress.unlockedTraits)
            {
                SolverTraitDef def = DefDatabase<SolverTraitDef>.GetNamedSilentFail(traitDefName);
                if (def == null) continue;

                // Add ability if not already present
                if (def.GivenAbility != null && pawn.abilities != null && !pawn.abilities.abilities.Any(a => a.def == def.GivenAbility))
                {
                    pawn.abilities.GainAbility(def.GivenAbility);
                }

                // Add hediff if not already present
                if (def.GivenHediff != null && pawn.health != null && !pawn.health.hediffSet.HasHediff(def.GivenHediff))
                {
                    // Apply to the whole body
                    pawn.health.AddHediff(def.GivenHediff, pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(p => p == pawn.RaceProps.body.corePart) ?? pawn.RaceProps.body.corePart);
                }
            }
        }

        // This method will be called from the transpiler-injected code
        public static float ApplySolverTraitStatEffects(Pawn pawn, StatDef stat, float value)
        {
            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            var progress = gene?.solverTraitProgress;
            if (progress == null) return value;

            float offsetSum = 0;
            float factorProduct = 1f;

            foreach (var traitDefName in progress.unlockedTraits)
            {
                var def = DefDatabase<SolverTraitDef>.GetNamedSilentFail(traitDefName);
                if (def == null) continue;

                if (def.statOffsets != null)
                {
                    foreach (var mod in def.statOffsets)
                    {
                        if (mod.stat == stat)
                        {
                            Log.Message($"[SolverTrait] Applying offset {mod.value} to {stat.defName} for {pawn.LabelShort}");
                            offsetSum += mod.value;
                        }
                    }
                }
                if (def.statFactors != null)
                {
                    foreach (var mod in def.statFactors)
                    {
                        if (mod.stat == stat)
                        {
                            Log.Message($"[SolverTrait] Applying factor {mod.value} to {stat.defName} for {pawn.LabelShort}");
                            factorProduct *= mod.value;
                        }
                    }
                }
            }

            return (value + offsetSum) * factorProduct;
        }

        public static void ForceStatRecalc(Pawn pawn, StatDef stat)
        {
            stat.Worker?.ClearCacheForThing(pawn);
        }

        public static string GetSolverTraitStatExplanation(Pawn pawn, StatDef stat)
        {
            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            var progress = gene?.solverTraitProgress;
            if (progress == null) return null;

            List<string> lines = new List<string>();
            foreach (var traitDefName in progress.unlockedTraits)
            {
                var def = DefDatabase<SolverTraitDef>.GetNamedSilentFail(traitDefName);
                if (def?.statOffsets != null)
                {
                    foreach (var mod in def.statOffsets)
                    {
                        if (mod.stat == stat)
                        {
                            string valueStr = mod.stat.Worker.ValueToString(mod.value, false, ToStringNumberSense.Offset);
                            lines.Add($"SolverTrait: {def.label} {valueStr}");
                        }
                    }
                }
                if (def?.statFactors != null)
                {
                    foreach (var mod in def.statFactors)
                    {
                        if (mod.stat == stat)
                        {
                            string factorStr = mod.stat.Worker.ValueToString(mod.value, false, ToStringNumberSense.Factor);
                            lines.Add($"SolverTrait: {def.label} x{factorStr}");
                        }
                    }
                }
            }
            return lines.Count > 0 ? string.Join("\n", lines) : null;
        }

        public static void AddSolverTrait(Pawn pawn, SolverTraitProgress progress, SolverTraitDef traitDef)
        {
            if (progress.unlockedTraits.Add(traitDef.defName))
            {
                // Apply effects (abilities, hediffs)
                ApplyAllTraitEffects(pawn, progress);

                // Invalidate stat cache for all affected stats
                if (traitDef.statOffsets != null)
                    foreach (var mod in traitDef.statOffsets)
                        ForceStatRecalc(pawn, mod.stat);
                if (traitDef.statFactors != null)
                    foreach (var mod in traitDef.statFactors)
                        ForceStatRecalc(pawn, mod.stat);
            }
        }

        public static void RemoveSolverTrait(Pawn pawn, SolverTraitProgress progress, SolverTraitDef traitDef)
        {
            if (progress.unlockedTraits.Remove(traitDef.defName))
            {
                // Remove all effects and re-apply current ones
                SyncAllTraitEffects(pawn, progress);

                // Invalidate stat cache for all affected stats
                if (traitDef.statOffsets != null)
                    foreach (var mod in traitDef.statOffsets)
                        ForceStatRecalc(pawn, mod.stat);
                if (traitDef.statFactors != null)
                    foreach (var mod in traitDef.statFactors)
                        ForceStatRecalc(pawn, mod.stat);
            }
        }


        /// <summary>
        /// Removes all effects that could have been added by any SolverTraitDef.
        /// </summary>
        public static void RemoveAllTraitEffects(Pawn pawn)
        {
            if (pawn == null) return;

            // Remove all abilities that were added by any SolverTraitDef
            if (pawn.abilities != null)
            {
                var allSolverTraitAbilities = DefDatabase<SolverTraitDef>.AllDefsListForReading
                    .Where(def => def.GivenAbility != null)
                    .Select(def => def.GivenAbility)
                    .ToHashSet();

                var toRemove = pawn.abilities.abilities
                    .Where(ab => allSolverTraitAbilities.Contains(ab.def))
                    .ToList();

                foreach (var ab in toRemove)
                    pawn.abilities.RemoveAbility(ab.def);
            }

            // Remove all hediffs that were added by any SolverTraitDef
            if (pawn.health != null)
            {
                var allSolverTraitHediffs = DefDatabase<SolverTraitDef>.AllDefsListForReading
                    .Where(def => def.GivenHediff != null)
                    .Select(def => def.GivenHediff)
                    .ToHashSet();

                var hediffsToRemove = pawn.health.hediffSet.hediffs
                    .Where(h => allSolverTraitHediffs.Contains(h.def))
                    .ToList();

                foreach (var h in hediffsToRemove)
                    pawn.health.RemoveHediff(h);
            }

            // TODO: Remove other effects (traits, passions, etc.) as needed
        }
        public static void CopyPawnLevelandUnlockedTraits(SolverTraitProgress from, SolverTraitProgress to)
        {
            if (from == null || to == null)
                return;

            // Copy unspent skill points
            to.unspentSkillPoints = from.unspentSkillPoints;

            // Copy unlocked traits (replace the set)
            to.unlockedTraits.Clear();
            foreach (var trait in from.unlockedTraits)
            {
                to.unlockedTraits.Add(trait);
            }
        }
    }
}

