using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn), "Kill", new Type[] { typeof(DamageInfo?), typeof(Hediff) })]
    public static class Pawn_ReactorSpawn_OnDeath_Patch
    {
        public static PawnKindDef ReactorPawnKindDef = MD_DefOf.MD_CoreHeartBasic;

        // Keep track of processed host pawns to avoid duplicate processing.
        private static readonly HashSet<int> _spawnedHostPawnIds = new HashSet<int>();

        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
        {
            // Do nothing if pawn is already a reactor pawn.
            if (__instance.def == MD_DefOf.MD_CoreHeartRace)
                return;

            // Already processed check.
            if (_spawnedHostPawnIds.Contains(__instance.thingIDNumber))
                return;

            if (!__instance.Dead)
                return;

            // Only process for Androids with the specific gene.
            var solverGene = __instance.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
            if (solverGene == null || !solverGene.HasSolver || !__instance.IsAndroid())
                return;

            // Decide whether to spawn a reactor pawn.
            BodyPartRecord stomach = __instance.health.hediffSet.GetNotMissingParts()
                .FirstOrDefault(part => part.def == MD_DefOf.Stomach);

            bool spawnReactor = false;
            if (stomach != null)
            {
                bool reactorIntact = __instance.health.hediffSet.hediffs.Any(
                    h => h.def == VREA_DefOf.VREA_Reactor && h.Part == stomach);
                if (!reactorIntact)
                    return;  // Reactor reactor must be intact on the stomach.
                spawnReactor = true;
            }
            else
            {
                // With no stomach present, do not spawn the reactor pawn.
                return;
            }

            // Mark as processed even if errors occur later.
            _spawnedHostPawnIds.Add(__instance.thingIDNumber);

            try
            {
                // Save the original faction.
                Faction originalFaction = __instance.Faction;

                // Determine spawn location.
                IntVec3 spawnPosition = __instance.Position;
                Map spawnMap = __instance.Map;
                if (spawnMap == null && __instance.Corpse != null)
                {
                    spawnMap = __instance.Corpse.Map;
                    spawnPosition = __instance.Corpse.Position;
                }
                if (spawnMap == null)
                {
                    Log.Error("[ReactorSpawn] Cannot spawn reactor pawn because map is null.");
                    return;
                }

                // Generate the reactor pawn.
                Pawn reactorPawn = GenerateReactorPawn(__instance, originalFaction);
                if (reactorPawn == null)
                {
                    Log.Error("[ReactorSpawn] Failed to generate reactor pawn.");
                    return;
                }

                // Spawn the reactor pawn as a flyer.
                PawnFlyer flyer = SimpleFlyerUtility.SpawnPawnAsFlyer(reactorPawn, spawnMap, spawnPosition, 5);
                if (flyer != null)
                    Log.Message($"[ReactorSpawn] Reactor pawn {reactorPawn.Name} spawned as flyer.");
                else
                    Log.Message($"[ReactorSpawn] Reactor pawn {reactorPawn.Name} spawned without flyer.");

                // If the stomach part exists, mark it as missing.
                if (stomach != null)
                {
                    var missing = (Hediff_MissingPart)HediffMaker.MakeHediff(
                        HediffDefOf.MissingBodyPart, __instance, stomach);
                    __instance.health.AddHediff(missing, stomach);
                    Log.Message("[ReactorSpawn] Removed stomach part from corpse.");
                }

                // Null out the host pawn's faction.
                FieldInfo factionField = typeof(Pawn).GetField("factionInt", BindingFlags.Instance | BindingFlags.NonPublic);
                if (factionField != null)
                {
                    factionField.SetValue(__instance, null);
                    Log.Message("[ReactorSpawn] Host pawn faction set to null.");
                }
                else
                {
                    Log.Error("[ReactorSpawn] Could not find factionInt field.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[ReactorSpawn] Error spawning reactor pawn: {ex}");
            }
        }

        /// <summary>
        /// Generates a new reactor pawn based on the original pawn's data.
        /// </summary>
        private static Pawn GenerateReactorPawn(Pawn original, Faction originalFaction)
        {
            PawnKindDef kind = ReactorPawnKindDef;
            if (kind == null)
            {
                Log.Error("[ReactorSpawn] Reactor pawn kind def is not set.");
                return null;
            }

            var request = new PawnGenerationRequest(
                kind,
                originalFaction, // Preserve the original faction.
                PawnGenerationContext.NonPlayer,
                original.Map?.Tile ?? -1,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: true,
                colonistRelationChanceFactor: 1f,
                fixedGender: original.gender
            );

            Pawn reactor = PawnGenerator.GeneratePawn(request);
            if (reactor != null)
                CopyPawnData(original, reactor);

            return reactor;
        }

        /// <summary>
        /// Copies basic data from the source pawn to the target pawn.
        /// Clears inventory, apparel, and equipment; transfers skin color,
        /// backstories, relations, ideology, and the xenotype.
        /// </summary>
        private static void CopyPawnData(Pawn source, Pawn target)
        {
            target.Name = source.Name;
            target.gender = source.gender;
            target.ageTracker.AgeBiologicalTicks = source.ageTracker.AgeBiologicalTicks;
            target.ageTracker.AgeChronologicalTicks = source.ageTracker.AgeChronologicalTicks;

            if (source.genes == null)
            {
                Log.Error("[ReactorSpawn] Failed to access gene list for xenotype transfer. Pawn: " +
                          (source != null ? source.LabelShort : "null"));
                return;
            }

            if (source.skills != null && target.skills != null)
            {
                foreach (SkillRecord skill in source.skills.skills)
                {
                    SkillRecord targetSkill = target.skills.GetSkill(skill.def);
                    targetSkill.levelInt = skill.levelInt;
                    targetSkill.xpSinceLastLevel = skill.xpSinceLastLevel;
                    targetSkill.passion = skill.passion;
                }
            }

            CopyBackstories(source, target);

            if (source.story != null && target.story != null)
            {
                target.story.traits.allTraits.Clear();
                foreach (Trait trait in source.story.traits.allTraits)
                {
                    target.story.traits.GainTrait(new Trait(trait.def, trait.Degree));
                }
            }

            // Transfer relations.
            if (source.relations != null && target.relations != null)
            {
                foreach (DirectPawnRelation rel in source.relations.DirectRelations)
                {
                    target.relations.AddDirectRelation(rel.def, rel.otherPawn);
                }
                source.relations.ClearAllRelations();
            }

            // Remove apparel.
            if (target.apparel != null)
            {
                var wornApparel = target.apparel.WornApparel.ToList();
                foreach (var apparel in wornApparel)
                {
                    target.apparel.Remove(apparel);
                    apparel.Destroy(DestroyMode.Vanish);
                }
            }
            // Clear inventory and equipment.
            if (target.inventory != null)
            {
                target.inventory.innerContainer.Clear();
            }
            if (target.equipment != null)
            {
                var equipments = target.equipment.AllEquipmentListForReading.ToList();
                foreach (var eq in equipments)
                {
                    target.equipment.Remove(eq);
                    eq.Destroy(DestroyMode.Vanish);
                }
            }

            // Transfer skin color.
            if (source.story != null && target.story != null)
            {
                try
                {
                    Color hostSkinColor = source.story.SkinColor;
                    Log.Message($"[ReactorSpawn] Host skin color: {hostSkinColor}");
                    FieldInfo skinColorField = typeof(Pawn_StoryTracker)
                        .GetField("<SkinColor>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (skinColorField != null)
                    {
                        skinColorField.SetValue(target.story, hostSkinColor);
                        Log.Message($"[ReactorSpawn] Transferred skin color via backing field: {target.story.SkinColor}");
                    }
                    else
                    {
                        var colorFields = typeof(Pawn_StoryTracker)
                            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                            .Where(f => f.FieldType == typeof(Color))
                            .ToList();
                        if (colorFields.Count == 1)
                        {
                            colorFields[0].SetValue(target.story, hostSkinColor);
                            Log.Message($"[ReactorSpawn] Transferred skin color via fallback field: {hostSkinColor}");
                        }
                        else if (colorFields.Count > 1)
                        {
                            Log.Warning("[ReactorSpawn] Multiple Color fields found in Pawn_StoryTracker; cannot determine which to use.");
                        }
                        else
                        {
                            Log.Warning("[ReactorSpawn] No Color fields found in Pawn_StoryTracker; cannot transfer skin color.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[ReactorSpawn] Error transferring skin color: " + ex);
                }
            }

            // Transfer ideology, if any.
            if (source.ideo != null && target.ideo != null)
            {
                try
                {
                    target.ideo.SetIdeo(source.ideo.Ideo);
                    Log.Message("[ReactorSpawn] Transferred ideology from host.");
                }
                catch (Exception ex)
                {
                    Log.Error("[ReactorSpawn] Error transferring ideology: " + ex);
                }
            }

            // Transfer xenotype: copy the xenotype field.
            if (source.genes != null && target.genes != null)
            {
                try
                {
                    FieldInfo xenotypeField = source.genes.GetType().GetField("xenotype", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (xenotypeField != null)
                    {
                        var xenotypeValue = xenotypeField.GetValue(source.genes);
                        xenotypeField.SetValue(target.genes, xenotypeValue);
                        Log.Message("[ReactorSpawn] Transferred xenotype field from host to reactor pawn.");
                    }
                    else
                    {
                        Log.Warning("[ReactorSpawn] Xenotype field not found on genes tracker. Saving xenotype instead.");
                        SaveXenotypeToPawn(source, target);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[ReactorSpawn] Error transferring xenotype: " + ex);
                }
            }
        }

        /// <summary>
        /// Copies the backstories (childhood and adulthood) from the source pawn to the target pawn via reflection.
        /// </summary>
        private static void CopyBackstories(Pawn source, Pawn target)
        {
            try
            {
                FieldInfo childhoodField = typeof(Pawn_StoryTracker).GetField("childhood", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo adulthoodField = typeof(Pawn_StoryTracker).GetField("adulthood", BindingFlags.Instance | BindingFlags.NonPublic);
                if (childhoodField != null && adulthoodField != null && source.story != null && target.story != null)
                {
                    object sourceChildhood = childhoodField.GetValue(source.story);
                    object sourceAdulthood = adulthoodField.GetValue(source.story);
                    childhoodField.SetValue(target.story, sourceChildhood);
                    adulthoodField.SetValue(target.story, sourceAdulthood);
                }
                else
                {
                    Log.Warning("[ReactorSpawn] Could not copy backstories using reflection (fields not found).");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[ReactorSpawn] Error copying backstories: " + ex);
            }
        }

        /// <summary>
        /// When gene lists aren’t accessible (for example after death), this method
        /// grabs the xenotype value as a string and saves it in the global tracker.
        /// </summary>
        private static void SaveXenotypeToPawn(Pawn source, Pawn target)
        {
            try
            {
                FieldInfo xenotypeField = source.genes.GetType().GetField("xenotype", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (xenotypeField != null)
                {
                    object xenotypeValue = xenotypeField.GetValue(source.genes);
                    if (xenotypeValue != null)
                    {
                        string xenotypeString = xenotypeValue.ToString();
                        // Save the xenotype in the global tracker.
                        XenotypeTracker tracker = Current.Game.GetComponent<XenotypeTracker>();
                        if (tracker != null)
                        {
                            // Using the reactor pawn's thingIDNumber as the key.
                            tracker.PawnXenotypes[target.thingIDNumber] = xenotypeString;
                            Log.Message("[ReactorSpawn] Saved original xenotype: " + xenotypeString + " for reactor pawn " + target);
                        }
                        else
                        {
                            Log.Warning("[ReactorSpawn] XenotypeTracker GameComponent not found; original xenotype: " + xenotypeString);
                        }
                    }
                    else
                    {
                        Log.Warning("[ReactorSpawn] Xenotype field value is null.");
                    }
                }
                else
                {
                    Log.Warning("[ReactorSpawn] Xenotype field not found on source genes tracker.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[ReactorSpawn] Error saving xenotype to pawn: " + ex);
            }
        }
    }
}

