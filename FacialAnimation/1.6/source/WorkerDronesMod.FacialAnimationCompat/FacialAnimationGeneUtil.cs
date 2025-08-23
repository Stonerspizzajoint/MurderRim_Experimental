using System;
using System.Collections.Generic;
using System.Reflection; // <-- Add this line
using Verse;
using FacialAnimation;


namespace WorkerDronesMod.FacialAnimationCompat
{
    public static class FacialAnimationGeneUtil
    {
        public static List<T> ForcedTypes<T>(
            List<GeneForcedFacetypesExtension> allExts,
            bool hasAnyExtensions,
            Func<GeneForcedFacetypesExtension, IEnumerable<T>> selector)
        {
            if (!hasAnyExtensions) return new List<T>();
            var list = new List<T>();
            for (int i = 0; i < allExts.Count; i++)
                list.AddRange(selector(allExts[i]));
            return list;
        }

        public static T PickType<T>(
            FacePartType part,
            List<T> forced,
            Dictionary<FacePartType, string[]> tagsForPart,
            Pawn pawn,
            string fallback)
            where T : FaceTypeDef, new()
        {
            if (forced.Count > 0)
                return forced.RandomElementByWeight(x => x.probability);
            var tags = tagsForPart[part];
            for (int i = 0; i < tags.Length; i++)
            {
                try
                {
                    var def = FaceTypeGenerator<T>.GetRandomDef(tags[i], pawn.gender);
                    if (def != null)
                        return def;
                }
                catch (KeyNotFoundException) { }
            }
            var allDefs = DefDatabase<T>.AllDefsListForReading;
            var prop = typeof(T).GetProperty("raceTag");
            if (prop != null)
            {
                for (int i = 0; i < allDefs.Count; i++)
                {
                    if ((string)prop.GetValue(allDefs[i]) == fallback)
                        return allDefs[i];
                }
            }
            if (allDefs.Count > 0)
                return allDefs[0];
            Log.Error($"[WDM] PickType<{typeof(T).Name}>: No valid FaceTypeDef found for {part}, returning new {typeof(T).Name}()");
            return new T();
        }

        public static Dictionary<FacePartType, string[]> BuildTagsForParts(
            List<GeneForcedFacetypesExtension> allExts,
            bool hasAnyExtensions,
            string fallback)
        {
            var tagsForPart = new Dictionary<FacePartType, string[]>();
            foreach (FacePartType part in Enum.GetValues(typeof(FacePartType)))
            {
                List<string> overrideTags = null;
                HashSet<string> tags = new HashSet<string>();

                for (int i = 0; i < allExts.Count; i++)
                {
                    var ext = allExts[i];
                    for (int j = 0; j < ext.raceTagOptions.Count; j++)
                    {
                        var opt = ext.raceTagOptions[j];
                        if (opt.facePart == part && opt.overrideOthers && !string.IsNullOrWhiteSpace(opt.raceTag))
                        {
                            if (overrideTags == null) overrideTags = new List<string>();
                            overrideTags.Add(opt.raceTag);
                        }
                    }
                }

                if (overrideTags != null && overrideTags.Count > 0)
                {
                    for (int i = 0; i < overrideTags.Count; i++)
                        tags.Add(overrideTags[i]);
                }
                else if (hasAnyExtensions)
                {
                    for (int i = 0; i < allExts.Count; i++)
                    {
                        var ext = allExts[i];
                        for (int j = 0; j < ext.raceTagOptions.Count; j++)
                        {
                            var opt = ext.raceTagOptions[j];
                            if (opt.facePart == part && !string.IsNullOrWhiteSpace(opt.raceTag))
                                tags.Add(opt.raceTag);
                        }
                        for (int j = 0; j < ext.raceTags.Count; j++)
                        {
                            var tag = ext.raceTags[j];
                            if (!string.IsNullOrWhiteSpace(tag))
                                tags.Add(tag);
                        }
                    }
                }
                if (!tags.Contains(fallback))
                    tags.Add(fallback);
                if (tags.Count == 0)
                    tags.Add("normal");
                tagsForPart[part] = new List<string>(tags).ToArray();
            }
            return tagsForPart;
        }
        public static void SetIfChanged<TComp, TDef>(
            Pawn pawn,
            FacePartType part,
            List<GeneForcedFacetypesExtension> allExts,
            bool hasAnyExtensions,
            Dictionary<FacePartType, string[]> tagsForPart,
            string fallback,
            Func<GeneForcedFacetypesExtension, IEnumerable<TDef>> selector)
            where TComp : ThingComp
            where TDef : FaceTypeDef, new()
        {
            var comp = pawn.GetComp<TComp>();
            if (comp == null) return;
            var prop = comp.GetType().GetProperty("FaceType");
            if (prop == null) return;
            var current = prop.GetValue(comp) as TDef;
            var forced = ForcedTypes(allExts, hasAnyExtensions, selector);
            var next = PickType(part, forced, tagsForPart, pawn, fallback);
            if (next == null) return; // Defensive: never set null
            if (!EqualityComparer<TDef>.Default.Equals(current, next))
            {
                prop.SetValue(comp, next);
                var setDirty = comp.GetType().GetMethod("SetDirty", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                setDirty?.Invoke(comp, null);
            }
        }

        public static void ResetToBaseRace<TComp, TDef>(Pawn pawn, FacePartType part, string fallback)
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
                for (int i = 0; i < allDefs.Count; i++)
                {
                    if ((string)propRaceTag.GetValue(allDefs[i]) == fallback)
                    {
                        def = allDefs[i];
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
        public static List<GeneForcedFacetypesExtension> GetAllGeneFacetypesExtensions(Pawn pawn)
        {
            var result = new List<GeneForcedFacetypesExtension>();
            var genes = pawn.genes?.GenesListForReading;
            if (genes != null)
            {
                for (int i = 0; i < genes.Count; i++)
                {
                    var ext = genes[i].def.GetModExtension<GeneForcedFacetypesExtension>();
                    if (ext != null)
                        result.Add(ext);
                }
            }
            return result;
        }

        public static void ReloadAllFacePartControllers(Pawn pawn)
        {
            SafeReload(pawn.GetComp<HeadControllerComp>());
            SafeReload(pawn.GetComp<BrowControllerComp>());
            SafeReload(pawn.GetComp<MouthControllerComp>());
            SafeReload(pawn.GetComp<EyeballControllerComp>());
            SafeReload(pawn.GetComp<LidControllerComp>());
            SafeReload(pawn.GetComp<LidOptionControllerComp>());
            SafeReload(pawn.GetComp<SkinControllerComp>());
        }

        public static void SafeReload(object comp)
        {
            if (comp == null) return;
            var pawnField = comp.GetType().GetField("pawn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var pawnVal = pawnField?.GetValue(comp) as Pawn;
            if (pawnVal == null || pawnVal.DestroyedOrNull()) return;
            var faceTypeProp = comp.GetType().GetProperty("FaceType");
            if (faceTypeProp != null && faceTypeProp.GetValue(comp) == null) return;
            var reload = comp.GetType().GetMethod("ReloadIfNeed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            reload?.Invoke(comp, null);
        }

    }
}

