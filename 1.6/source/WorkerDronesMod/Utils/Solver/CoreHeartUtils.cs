using RimWorld;
using Verse;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Verse.AI;

namespace WorkerDronesMod
{
    public static class CoreHeartUtils
    {
        private static readonly List<IntVec3> tmpTakenCells = new List<IntVec3>();

        public static void SpawnCoreHeartCopyFromCorpse(Corpse corpse)
        {
            if (!CanSpawnCoreHeartFromCorpse(corpse))
                return;

            if (corpse == null || !corpse.Spawned || corpse.Map == null)
                return;

            Pawn original = corpse.InnerPawn;
            if (original == null)
                return;

            // Prevent recursion or duplicate core heart spawns (core hearts should not spawn more core hearts)
            if (original.kindDef == MD_DefOf.MD_CoreHeartBasic)
                return;

            // After spawning the core heart and before/after CopyRelations:
            RemoveBodyPartsFromCorpse(original, new[] { MD_DefOf.Stomach, MD_DefOf.Brain });

            Map map = corpse.Map;
            IntVec3 rootCell = corpse.Position;

            // Generate the core heart pawn, copying aspects from the original
            PawnGenerationRequest req = new PawnGenerationRequest(
                MD_DefOf.MD_CoreHeartBasic,
                original.Faction,
                PawnGenerationContext.NonPlayer,
                -1,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                colonistRelationChanceFactor: 0f,
                forceAddFreeWarmLayerIfNeeded: false,
                allowGay: false,
                allowFood: false,
                allowAddictions: false,
                inhabitant: false,
                certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false,
                worldPawnFactionDoesntMatter: false,
                biocodeWeaponChance: 0f,
                relationWithExtraPawnChanceFactor: 0f,
                validatorPreGear: null,
                validatorPostGear: null,
                fixedBiologicalAge: original.ageTracker.AgeBiologicalYearsFloat,
                fixedChronologicalAge: original.ageTracker.AgeChronologicalYearsFloat,
                fixedGender: original.gender
            );

            Pawn coreHeart = PawnGenerator.GeneratePawn(req);

            // Name
            coreHeart.Name = original.Name;
            if (coreHeart.Faction != original.Faction)
                coreHeart.SetFaction(original.Faction);

            // Use duplicator's robust copy methods for most aspects
            PawnCopyUtil.CopyStoryAndTraits(original, coreHeart);
            PawnCopyUtil.CopyApperance(original, coreHeart);
            PawnCopyUtil.CopyStyle(original, coreHeart);
            PawnCopyUtil.CopySkills(original, coreHeart);
            PawnCopyUtil.CopyAbilities(original, coreHeart);

            // Optionally: Copy needs, hediffs, etc. if you want
            // GameComponent_PawnDuplicator.CopyNeeds(original, coreHeart);
            // GameComponent_PawnDuplicator.CopyHediffs(original, coreHeart);

            // Only transfer solver trait progress if the original pawn has MD_AbsoluteSolver (not just MD_BasicSolver)
            if (original.genes != null && original.genes.HasActiveGene(MD_DefOf.MD_AbsoluteSolver))
            {
                // Remove MD_BasicSolver from the new pawn if present (since we want to use MD_AbsoluteSolver instead)
                var basicGene = coreHeart.genes.GetGene(MD_DefOf.MD_BasicSolver);
                if (basicGene != null)
                    coreHeart.genes.RemoveGene(basicGene);

                // Add MD_AbsoluteSolver to the new pawn if not present
                if (!coreHeart.genes.HasActiveGene(MD_DefOf.MD_AbsoluteSolver))
                    coreHeart.genes.AddGene(MD_DefOf.MD_AbsoluteSolver, false);

                // Now copy the SolverTraitProgress from the original's MD_AbsoluteSolver gene to the new pawn's MD_AbsoluteSolver gene
                var origGene = original.genes.GetGene(MD_DefOf.MD_AbsoluteSolver) as Gene_BasicSolver;
                var newGene = coreHeart.genes.GetGene(MD_DefOf.MD_AbsoluteSolver) as Gene_BasicSolver;
                if (origGene != null && newGene != null)
                {
                    SolverTraitEffectManager.CopyPawnLevelandUnlockedTraits(origGene.solverTraitProgress, newGene.solverTraitProgress);
                    SolverTraitEffectManager.SyncAllTraitEffects(coreHeart, newGene.solverTraitProgress);
                }
            }


            CopyRelations(original, coreHeart);

            ApplyStomachBasedSpawnDamage(original, coreHeart);

            // Use robust flyer spawn logic (adapted from vanilla)
            SpawnPawnAsFlyerSafe(coreHeart, map, rootCell, ThingDefOf.PawnFlyer, 5, true);

            if (MD_DefOf.MD_CoreExit != null)
            {
                Effecter effecter = MD_DefOf.MD_CoreExit.Spawn();
                effecter.Trigger(new TargetInfo(rootCell, map), TargetInfo.Invalid);
                effecter.Cleanup();
            }

            MakePawnExitMapFast(coreHeart);

            if (MD_DefOf.MD_CoreExit != null)
            {
                Effecter effecter = MD_DefOf.MD_CoreExit.Spawn();
                effecter.Trigger(new TargetInfo(rootCell, map), TargetInfo.Invalid);
                effecter.Cleanup();
            }

        }

        // Adapted from vanilla FleshbeastUtility.SpawnPawnAsFlyer
        public static Thing SpawnPawnAsFlyerSafe(Pawn pawn, Map map, IntVec3 rootCell, ThingDef flyerDef, int jumpDist = 5, bool requiresLOS = true)
        {
            if (!pawn.Spawned)
            {
                GenSpawn.Spawn(pawn, rootCell, map, WipeMode.Vanish);
            }
            tmpTakenCells.Clear();
            IntVec3 intVec;
            Predicate<IntVec3> validator = c =>
                !c.Fogged(map) &&
                c.Standable(map) &&
                !tmpTakenCells.Contains(c) &&
                c.GetFirstPawn(map) == null &&
                (!requiresLOS || GenSight.LineOfSight(rootCell, c, map, true, null, 0, 0));
            if (RCellFinder.TryFindRandomCellNearWith(rootCell, validator, map, out intVec, 5, jumpDist))
            {
                pawn.rotationTracker.FaceCell(intVec);
                tmpTakenCells.Add(intVec);
                PawnFlyer pawnFlyer = PawnFlyer.MakeFlyer(flyerDef, pawn, intVec, null, null, false, null, null, default(LocalTargetInfo));
                if (pawnFlyer != null)
                {
                    GenSpawn.Spawn(pawnFlyer, intVec, map, WipeMode.Vanish);
                }
                return pawnFlyer;
            }
            Log.Warning("[CoreHeart] No valid flyer cell found for core heart spawn.");
            return null;
        }

        private static void CopyRelations(Pawn original, Pawn coreHeart)
        {
            if (original.relations == null || coreHeart.relations == null)
                return;

            List<DirectPawnRelation> relations = original.relations.DirectRelations.ListFullCopy();

            foreach (var rel in relations)
            {
                if (rel.otherPawn != null && rel.def != null)
                {
                    original.relations.RemoveDirectRelation(rel.def, rel.otherPawn);
                    coreHeart.relations.AddDirectRelation(rel.def, rel.otherPawn);
                    if (rel.otherPawn.relations != null)
                    {
                        rel.otherPawn.relations.RemoveDirectRelation(rel.def, original);
                        rel.otherPawn.relations.AddDirectRelation(rel.def, coreHeart);
                    }
                }
            }
        }
        public static bool CanSpawnCoreHeartFromCorpse(Corpse corpse)
        {
            if (corpse == null || !corpse.Spawned || corpse.Map == null)
                return false;

            Pawn pawn = corpse.InnerPawn;
            if (pawn == null)
                return false;

            // Example: Prevent certain xenotypes, factions, or other pawn kinds from spawning core hearts
            // if (pawn.genes?.Xenotype == SomeSpecialXenotypeDef)
            //     return false;

            // Check if stomach is missing or destroyed
            var stomach = pawn.health.hediffSet.GetNotMissingParts()
                .FirstOrDefault(part => part.def == MD_DefOf.Stomach);
            if (stomach == null)
                return false;

            // Add more checks here as needed

            return true;
        }
        private static void RemoveBodyPartsFromCorpse(Pawn pawn, IEnumerable<BodyPartDef> partsToRemove)
        {
            if (pawn == null || partsToRemove == null)
                return;

            foreach (var partDef in partsToRemove)
            {
                var part = pawn.health.hediffSet.GetNotMissingParts()
                    .FirstOrDefault(p => p.def == partDef);
                if (part != null)
                {
                    pawn.health.AddHediff(HediffDefOf.MissingBodyPart, part);
                }
            }
        }
        private static void ApplyStomachBasedSpawnDamage(Pawn original, Pawn newPawn)
        {
            // Find stomach on original
            var origStomach = original.health.hediffSet.GetNotMissingParts()
                .FirstOrDefault(part => part.def == MD_DefOf.Stomach);
            if (origStomach == null)
                return; // No stomach, nothing to do

            float origCurrent = original.health.hediffSet.GetPartHealth(origStomach);
            float origMax = origStomach.def.GetMaxHealth(original);

            if (origMax <= 0f)
                return;

            float healthRatio = Mathf.Clamp01(origCurrent / origMax);

            // The lower the health ratio, the more damage to apply (e.g., up to 50% of newPawn's total health)
            float damageFraction = 1f - healthRatio;
            if (damageFraction <= 0f)
                return;

            float totalHealth = newPawn.health.summaryHealth.SummaryHealthPercent;
            float damageToApply = totalHealth * damageFraction * 0.5f; // up to 50% of total health

            if (damageToApply > 1f)
            {
                // Apply as a single hit to the core part (torso), or distribute as you wish
                var corePart = newPawn.RaceProps.body.corePart;
                var dinfo = new DamageInfo(DamageDefOf.Blunt, damageToApply, 999f, -1f, null, corePart);
                newPawn.TakeDamage(dinfo);
            }
        }
        private static void MakePawnExitMapFast(Pawn pawn)
        {
            if (pawn == null || pawn.Faction == null || pawn.Faction == Faction.OfPlayer)
                return;

            if (pawn.MentalStateDef != MentalStateDefOf.PanicFlee)
            {
                pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, "CoreHeart spawned from hostile/neutral faction", forceWake: true);
            }
            if (pawn.drafter != null)
            {
                pawn.drafter.Drafted = false;
            }
        }
        public static bool IsCoreHeart(Pawn pawn)
        {
            return pawn != null && pawn.def == MD_DefOf.MD_CoreHeartRace;
        }
    }
    public static class PawnCopyUtil
    {
        public static void CopyStoryAndTraits(Pawn pawn, Pawn newPawn)
        {
            newPawn.story.favoriteColor = pawn.story.favoriteColor;
            newPawn.story.Childhood = pawn.story.Childhood;
            newPawn.story.Adulthood = pawn.story.Adulthood;
            newPawn.story.traits.allTraits.Clear();
            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                if (!ModsConfig.BiotechActive || trait.sourceGene == null)
                {
                    newPawn.story.traits.GainTrait(new Trait(trait.def, trait.Degree, trait.ScenForced), false);
                }
            }
        }

        public static void CopyApperance(Pawn pawn, Pawn newPawn)
        {
            newPawn.story.headType = pawn.story.headType;
            newPawn.story.bodyType = pawn.story.bodyType;
            newPawn.story.hairDef = pawn.story.hairDef;
            newPawn.story.HairColor = pawn.story.HairColor;
            newPawn.story.SkinColorBase = pawn.story.SkinColorBase;
            newPawn.story.skinColorOverride = pawn.story.skinColorOverride;
            newPawn.story.furDef = pawn.story.furDef;
        }

        public static void CopyStyle(Pawn pawn, Pawn newPawn)
        {
            newPawn.style.beardDef = pawn.style.beardDef;
            if (ModsConfig.IdeologyActive)
            {
                newPawn.style.BodyTattoo = pawn.style.BodyTattoo;
                newPawn.style.FaceTattoo = pawn.style.FaceTattoo;
            }
        }


        public static void CopySkills(Pawn pawn, Pawn newPawn)
        {
            newPawn.skills.skills.Clear();
            foreach (SkillRecord skillRecord in pawn.skills.skills)
            {
                SkillRecord item = new SkillRecord(newPawn, skillRecord.def)
                {
                    levelInt = skillRecord.levelInt,
                    passion = skillRecord.passion,
                    xpSinceLastLevel = skillRecord.xpSinceLastLevel,
                    xpSinceMidnight = skillRecord.xpSinceMidnight
                };
                newPawn.skills.skills.Add(item);
            }
        }

        public static void CopyAbilities(Pawn pawn, Pawn newPawn)
        {
            // Get all abilities that can be granted by solver traits
            var solverTraitAbilities = DefDatabase<SolverTraitDef>.AllDefsListForReading
                .Where(def => def.GivenAbility != null)
                .Select(def => def.GivenAbility)
                .ToHashSet();

            // Add abilities present on the original pawn that are solver trait abilities
            if (pawn.abilities != null && newPawn.abilities != null)
            {
                foreach (var abilityDef in solverTraitAbilities)
                {
                    bool originalHas = pawn.abilities.abilities.Any(a => a.def == abilityDef);
                    bool newHas = newPawn.abilities.abilities.Any(a => a.def == abilityDef);

                    if (originalHas && !newHas)
                        newPawn.abilities.GainAbility(abilityDef);
                    else if (!originalHas && newHas)
                        newPawn.abilities.RemoveAbility(abilityDef);
                }
            }
        }

    }
}

