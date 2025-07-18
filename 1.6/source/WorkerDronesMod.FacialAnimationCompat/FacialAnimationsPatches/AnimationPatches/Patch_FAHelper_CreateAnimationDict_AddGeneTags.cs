using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using FacialAnimation;
using UnityEngine;

namespace WorkerDronesMod.FacialAnimationCompat.Patches
{
    [HarmonyPatch]
    static class Patch_FAHelper_CreateAnimationDict_AddGeneTags
    {
        // 1) Find the exact CreateAnimationDict overload
        static MethodBase TargetMethod()
        {
            var faHelperType = AccessTools.TypeByName("FacialAnimation.FAHelper");
            if (faHelperType == null)
            {
                Log.Error("[GeneFacetypes] Could not find FacialAnimation.FAHelper");
                return null;
            }

            return faHelperType.GetMethod(
                "CreateAnimationDict",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Pawn), typeof(int), typeof(Dictionary<string, List<FaceAnimation>>).MakeByRefType() },
                null
            );
        }

        // 2) Postfix injection
        static void Postfix(Pawn pawn, int initialTick, ref Dictionary<string, List<FaceAnimation>> animationDict)
        {
            try
            {
                if (pawn?.genes == null || animationDict == null) return;

                // Gather all race tags: base race + gene tags
                var taggedRaces = new HashSet<string> { pawn.def.defName };
                foreach (var g in pawn.genes.GenesListForReading)
                {
                    var ext = g.def.GetModExtension<GeneForcedFacetypesExtension>();
                    if (ext != null)
                        foreach (var tag in ext.raceTags)
                            taggedRaces.Add(tag);
                }

                // Find all FaceAnimationDefs for any of these tags
                var extraDefs = DefDatabase<FaceAnimationDef>.AllDefs
                    .Where(fd => taggedRaces.Contains(fd.raceName))
                    .ToList();

                if (extraDefs.Count == 0) return;

                // Instantiate FaceAnimation for each def
                var extraAnims = extraDefs
                    .Select(fd => new FaceAnimation(fd, initialTick))
                    .ToList();
                if (extraAnims.Count == 0) return;

                // Precompute the constant-job animations
                var constantAnims = extraAnims
                    .Where(a => a.animationDef.targetJobs != null && a.animationDef.targetJobs.Contains(FaceAnimationDef.CONSTANT_JOB))
                    .ToList();

                // For duplicate prevention, use animationDef as key
                foreach (var kvp in animationDict)
                {
                    var jobKey = kvp.Key;
                    var list = kvp.Value;
                    if (list == null) continue;

                    var existingDefs = new HashSet<FaceAnimationDef>(list.Select(a => a.animationDef));

                    // 1) ConstantJob goes everywhere
                    foreach (var anim in constantAnims)
                        if (!existingDefs.Contains(anim.animationDef))
                        {
                            list.Add(anim);
                            existingDefs.Add(anim.animationDef);
                        }

                    // 2) Job-specific: only if the def’s targetJobs contains this jobKey
                    if (!string.IsNullOrEmpty(jobKey))
                    {
                        foreach (var anim in extraAnims.Where(a => a.animationDef.targetJobs != null && a.animationDef.targetJobs.Contains(jobKey)))
                            if (!existingDefs.Contains(anim.animationDef))
                            {
                                list.Add(anim);
                                existingDefs.Add(anim.animationDef);
                            }
                    }

                    // 3) Re-sort by priority
                    list.Sort((a, b) => a.animationDef.priority - b.animationDef.priority);
                }

                if (Prefs.DevMode)
                    Log.Message($"[GeneFacetypes] Injected {extraAnims.Count} animations for pawn {pawn.LabelShort}.");
            }
            catch (Exception ex)
            {
                Log.Error($"[GeneFacetypes] Exception in Patch_FAHelper_CreateAnimationDict_AddGeneTags: {ex}");
            }
        }
    }
}






