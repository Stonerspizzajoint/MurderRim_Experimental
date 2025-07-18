using HarmonyLib;
using System.Linq;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System;
using FacialAnimation;
using WorkerDronesMod.FacialAnimationCompat;
using System.Reflection;

namespace WorkerDronesMod.FacialAnimationCompat
{
    [HarmonyPatch(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.RemoveGene))]
    public static class Patch_PawnGeneTracker_RemoveGene_ClearForcedTypes
    {
        private static readonly Type FacType = AccessTools.TypeByName("FacialAnimation.FacialAnimationControllerComp");
        private static readonly Type FaHelperType = AccessTools.TypeByName("FacialAnimation.FAHelper");
        private static readonly FieldInfo AnimDictField = FacType?.GetField("animationDict", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly MethodInfo CreateAnimDict = FaHelperType?.GetMethod("CreateAnimationDict", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        static void Postfix(Gene gene, Pawn_GeneTracker __instance)
        {
            var pawn = __instance.pawn;
            if (pawn == null) return;

            // Gather all relevant gene extensions (no LINQ)
            var allExts = new List<GeneForcedFacetypesExtension>();
            var genes = pawn.genes?.GenesListForReading;
            if (genes != null)
            {
                foreach (var g in genes)
                {
                    var ext = g.def.GetModExtension<GeneForcedFacetypesExtension>();
                    if (ext != null)
                        allExts.Add(ext);
                }
            }
            bool hasAnyExtensions = allExts.Count > 0;
            string fallback = pawn.def.defName;

            // Precompute tags for each part, with overrideOthers support
            var tagsForPart = new Dictionary<FacePartType, string[]>();
            foreach (FacePartType part in Enum.GetValues(typeof(FacePartType)))
            {
                // 1. Find all RaceTagOptions with overrideOthers == true for this part
                var overrideOptions = allExts
                    .SelectMany(ext => ext.raceTagOptions)
                    .Where(opt => opt.facePart == part && opt.overrideOthers && !string.IsNullOrWhiteSpace(opt.raceTag))
                    .ToList();

                HashSet<string> tags = new HashSet<string>();

                if (overrideOptions.Count > 0)
                {
                    foreach (var opt in overrideOptions)
                        tags.Add(opt.raceTag);
                }
                else if (hasAnyExtensions)
                {
                    foreach (var ext in allExts)
                    {
                        foreach (var opt in ext.raceTagOptions)
                            if (opt.facePart == part && !string.IsNullOrWhiteSpace(opt.raceTag))
                                tags.Add(opt.raceTag);
                        foreach (var tag in ext.raceTags)
                            if (!string.IsNullOrWhiteSpace(tag))
                                tags.Add(tag);
                    }
                }
                // Always add fallback, but only if not already present
                if (!tags.Contains(fallback))
                    tags.Add(fallback);

                // If still empty, add a hardcoded fallback to avoid empty arrays
                if (tags.Count == 0)
                    tags.Add("normal");

                tagsForPart[part] = tags.ToArray();
            }

            // Helper to get forced types for a part
            List<T> ForcedTypes<T>(Func<GeneForcedFacetypesExtension, IEnumerable<T>> selector)
            {
                if (!hasAnyExtensions) return new List<T>();
                var list = new List<T>();
                foreach (var ext in allExts)
                    list.AddRange(selector(ext));
                return list;
            }

            // Helper to pick a type
            T PickType<T>(FacePartType part, List<T> forced) where T : FaceTypeDef, new()
            {
                if (forced.Count > 0)
                    return forced.RandomElementByWeight(x => x.probability);
                foreach (var tag in tagsForPart[part])
                {
                    try
                    {
                        var def = FaceTypeGenerator<T>.GetRandomDef(tag, pawn.gender);
                        if (def != null)
                            return def;
                    }
                    catch (KeyNotFoundException) { }
                }
                // As a last resort, try to get any def for the fallback race using reflection
                var allDefs = DefDatabase<T>.AllDefsListForReading;
                var prop = typeof(T).GetProperty("raceTag");
                if (prop != null)
                {
                    foreach (var d in allDefs)
                    {
                        if ((string)prop.GetValue(d) == fallback)
                            return d;
                    }
                }
                if (allDefs.Count > 0)
                    return allDefs[0];
                Log.Error($"[WDM] PickType<{typeof(T).Name}>: No valid FaceTypeDef found for {part}, returning new {typeof(T).Name}()");
                return new T();
            }

            // If no gene extensions remain, revert to base race facetypes
            if (!hasAnyExtensions)
            {
                ResetToBaseRace<HeadControllerComp, FacialAnimation.HeadTypeDef>(pawn, FacePartType.Head, fallback);
                ResetToBaseRace<BrowControllerComp, BrowTypeDef>(pawn, FacePartType.Brow, fallback);
                ResetToBaseRace<MouthControllerComp, MouthTypeDef>(pawn, FacePartType.Mouth, fallback);
                ResetToBaseRace<EyeballControllerComp, EyeballTypeDef>(pawn, FacePartType.Eye, fallback);
                ResetToBaseRace<LidControllerComp, LidTypeDef>(pawn, FacePartType.Lid, fallback);
                ResetToBaseRace<LidOptionControllerComp, LidOptionTypeDef>(pawn, FacePartType.LidOption, fallback);
                ResetToBaseRace<SkinControllerComp, SkinTypeDef>(pawn, FacePartType.Skin, fallback);
            }
            else
            {
                // Assign face types for each part, only reload if changed
                SetIfChanged<HeadControllerComp, FacialAnimation.HeadTypeDef>(pawn, FacePartType.Head, ext => ext.forcedHeadTypes);
                SetIfChanged<BrowControllerComp, BrowTypeDef>(pawn, FacePartType.Brow, ext => ext.forcedBrowTypes);
                SetIfChanged<MouthControllerComp, MouthTypeDef>(pawn, FacePartType.Mouth, ext => ext.forcedMouthTypes);
                SetIfChanged<EyeballControllerComp, EyeballTypeDef>(pawn, FacePartType.Eye, ext => ext.forcedEyeTypes);
                SetIfChanged<LidControllerComp, LidTypeDef>(pawn, FacePartType.Lid, ext => ext.forcedLidTypes);
                SetIfChanged<LidOptionControllerComp, LidOptionTypeDef>(pawn, FacePartType.LidOption, ext => ext.forcedLidOptionTypes);
                SetIfChanged<SkinControllerComp, SkinTypeDef>(pawn, FacePartType.Skin, ext => ext.forcedSkinTypes);
            }

            // Update FacialAnimationControllerComp after gene removal
            Verse.LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (FacType == null || AnimDictField == null || CreateAnimDict == null) return;

                var facComp = pawn.AllComps.FirstOrDefault(c => FacType.IsInstanceOfType(c));
                if (facComp == null) return;

                var animDict = AnimDictField.GetValue(facComp) as Dictionary<string, List<FaceAnimation>>;
                object[] parameters = new object[] { pawn, Find.TickManager.TicksGame, animDict };
                CreateAnimDict.Invoke(null, parameters);
                AnimDictField.SetValue(facComp, parameters[2]);
            });

            // Reload all face part controllers
            var reloaders = new Action[]
            {
                () => SafeReload(pawn.GetComp<HeadControllerComp>()),
                () => SafeReload(pawn.GetComp<BrowControllerComp>()),
                () => SafeReload(pawn.GetComp<MouthControllerComp>()),
                () => SafeReload(pawn.GetComp<EyeballControllerComp>()),
                () => SafeReload(pawn.GetComp<LidControllerComp>()),
                () => SafeReload(pawn.GetComp<LidOptionControllerComp>()),
                () => SafeReload(pawn.GetComp<SkinControllerComp>()),
            };
            LongEventHandler.ExecuteWhenFinished(() => { foreach (var r in reloaders) r(); });
        }

        private static void SetIfChanged<TComp, TDef>(Pawn pawn, FacePartType part, Func<GeneForcedFacetypesExtension, IEnumerable<TDef>> selector)
            where TComp : ThingComp
            where TDef : FaceTypeDef, new()
        {
            var comp = pawn.GetComp<TComp>();
            if (comp == null) return;
            var prop = comp.GetType().GetProperty("FaceType");
            if (prop == null) return;
            var current = prop.GetValue(comp) as TDef;
            var next = default(TDef);
            // Use PickType logic from above
            var allExts = pawn.genes?.GenesListForReading
                .Select(g => g.def.GetModExtension<GeneForcedFacetypesExtension>())
                .Where(ext => ext != null)
                .ToList() ?? new List<GeneForcedFacetypesExtension>();
            var hasAnyExtensions = allExts.Count > 0;
            var tagsForPart = new List<string>();
            foreach (var ext in allExts)
            {
                foreach (var opt in ext.raceTagOptions)
                    if (opt.facePart == part && !string.IsNullOrWhiteSpace(opt.raceTag))
                        tagsForPart.Add(opt.raceTag);
                foreach (var tag in ext.raceTags)
                    if (!string.IsNullOrWhiteSpace(tag))
                        tagsForPart.Add(tag);
            }
            if (tagsForPart.Count == 0)
                tagsForPart.Add(pawn.def.defName);

            var forced = hasAnyExtensions ? allExts.SelectMany(selector).ToList() : new List<TDef>();
            if (forced.Count > 0)
                next = forced.RandomElementByWeight(x => x.probability);
            else
            {
                foreach (var tag in tagsForPart)
                {
                    try
                    {
                        var def = FaceTypeGenerator<TDef>.GetRandomDef(tag, pawn.gender);
                        if (def != null)
                        {
                            next = def;
                            break;
                        }
                    }
                    catch (KeyNotFoundException) { }
                }
            }
            if (!EqualityComparer<TDef>.Default.Equals(current, next) && next != null)
            {
                prop.SetValue(comp, next);
                var setDirty = comp.GetType().GetMethod("SetDirty", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                setDirty?.Invoke(comp, null);
            }
        }

        private static void ResetToBaseRace<TComp, TDef>(Pawn pawn, FacePartType part, string fallback)
            where TComp : ThingComp
            where TDef : FaceTypeDef, new()
        {
            var comp = pawn.GetComp<TComp>();
            if (comp == null) return;
            var prop = comp.GetType().GetProperty("FaceType");
            if (prop == null) return;
            var allDefs = DefDatabase<TDef>.AllDefsListForReading;
            var propRaceTag = typeof(TDef).GetProperty("raceTag");
            TDef def = null;
            if (propRaceTag != null)
            {
                foreach (var d in allDefs)
                {
                    if ((string)propRaceTag.GetValue(d) == fallback)
                    {
                        def = d;
                        break;
                    }
                }
            }
            if (def == null && allDefs.Count > 0)
                def = allDefs[0];
            if (def != null)
            {
                prop.SetValue(comp, def);
                var setDirty = comp.GetType().GetMethod("SetDirty", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                setDirty?.Invoke(comp, null);
            }
        }

        private static void SafeReload(object comp)
        {
            if (comp == null) return;
            var pawnField = comp.GetType().GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var pawnVal = pawnField?.GetValue(comp) as Pawn;
            if (pawnVal == null || pawnVal.DestroyedOrNull()) return;
            var faceTypeProp = comp.GetType().GetProperty("FaceType");
            if (faceTypeProp != null && faceTypeProp.GetValue(comp) == null) return;
            var reload = comp.GetType().GetMethod("ReloadIfNeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            reload?.Invoke(comp, null);
        }
    }
}



