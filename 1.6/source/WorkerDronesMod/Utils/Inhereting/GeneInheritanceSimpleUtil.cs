using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace WorkerDronesMod
{
    /// <summary>
    /// Simple, readable gene inheritance utility inspired by vanilla RimWorld.
    /// Supports mod extension GeneInheritExtension for custom rules.
    /// </summary>
    public static class GeneInheritanceSimpleUtil
    {
        /// <summary>
        /// Returns a list of inherited genes for a child pawn, given two parents.
        /// </summary>
        public static List<GeneDef> GetInheritedGenes(Pawn parentA, Pawn parentB)
        {
            // Get inheritable genes for both parents
            var genesA = GetInheritableGenes(parentA).ToList();
            var genesB = GetInheritableGenes(parentB).ToList();

            // Select favored parent using FavoredParentChance
            Pawn favoredParent = SelectFavoredParent(parentA, parentB, genesA, genesB);
            Pawn otherParent = (favoredParent == parentA) ? parentB : parentA;
            var favoredGenes = (favoredParent == parentA) ? genesA : genesB;
            var otherGenes = (favoredParent == parentA) ? genesB : genesA;

            // Use favored parent as parentA for inheritance logic
            parentA = favoredParent;
            parentB = otherParent;
            genesA = favoredGenes;
            genesB = otherGenes;

            var allGenes = genesA.Concat(genesB).Distinct().ToList();
            var inheritedGenes = new HashSet<GeneDef>();

            // Group genes by exclusion tag
            var tagToGenes = new Dictionary<string, List<GeneDef>>();
            foreach (var gene in allGenes)
            {
                if (gene.exclusionTags != null)
                {
                    foreach (var tag in gene.exclusionTags)
                    {
                        if (!tagToGenes.TryGetValue(tag, out var list))
                        {
                            list = new List<GeneDef>();
                            tagToGenes[tag] = list;
                        }
                        list.Add(gene);
                    }
                }
            }

            var processedGenes = new HashSet<GeneDef>();
            var inheritedExclusionTags = new HashSet<string>();

            // Process genes with exclusion tags
            foreach (var kvp in tagToGenes)
            {
                var tag = kvp.Key;
                var genesWithTag = kvp.Value.Distinct().ToList();

                // Find which parent(s) have which gene(s) for this tag
                var parentAGenes = genesWithTag.Where(g => genesA.Contains(g)).ToList();
                var parentBGenes = genesWithTag.Where(g => genesB.Contains(g)).ToList();

                if (parentAGenes.Count > 0 && parentBGenes.Count > 0)
                {
                    // Both parents have a gene with this tag: randomly pick one gene from either parent
                    var options = parentAGenes.Concat(parentBGenes).Distinct()
                        .Where(g => g.exclusionTags == null || !g.exclusionTags.Any(t => inheritedExclusionTags.Contains(t)))
                        .ToList();
                    if (options.Count > 0)
                    {
                        var chosenGene = options[Rand.Range(0, options.Count)];
                        inheritedGenes.Add(chosenGene);
                        processedGenes.Add(chosenGene);
                        if (chosenGene.exclusionTags != null)
                        {
                            foreach (var t in chosenGene.exclusionTags)
                                inheritedExclusionTags.Add(t);
                        }
                    }
                }
                else if (parentAGenes.Count > 0 || parentBGenes.Count > 0)
                {
                    // Only one parent has a gene with this tag: use unique gene logic
                    var gene = parentAGenes.Count > 0 ? parentAGenes[0] : parentBGenes[0];
                    if (gene.exclusionTags != null && gene.exclusionTags.Any(t => inheritedExclusionTags.Contains(t)))
                        continue;
                    var ext = gene.GetModExtension<GeneInheritExtension>();
                    if (ext != null && ext.CannotInherit)
                        continue;
                    if (ext != null && ext.AlwaysInherit)
                    {
                        inheritedGenes.Add(gene);
                        processedGenes.Add(gene);
                        if (gene.exclusionTags != null)
                        {
                            foreach (var t in gene.exclusionTags)
                                inheritedExclusionTags.Add(t);
                        }
                        continue;
                    }
                    float chance = ext?.InheritChance ?? 1.0f;
                    float uniqueChance = (chance >= 1.0f) ? 0.5f : chance * 0.5f;
                    if (Rand.Chance(uniqueChance))
                    {
                        inheritedGenes.Add(gene);
                        processedGenes.Add(gene);
                        if (gene.exclusionTags != null)
                        {
                            foreach (var t in gene.exclusionTags)
                                inheritedExclusionTags.Add(t);
                        }
                    }
                }
                // If neither parent has a gene for this tag, do nothing.
            }

            // Now process any genes that don't have exclusion tags (or weren't processed above)
            foreach (var gene in allGenes)
            {
                if (processedGenes.Contains(gene))
                    continue;

                // Skip if any exclusion tag is already inherited
                if (gene.exclusionTags != null && gene.exclusionTags.Any(tag => inheritedExclusionTags.Contains(tag)))
                    continue;

                var ext = gene.GetModExtension<GeneInheritExtension>();
                if (ext != null && ext.CannotInherit)
                    continue;
                if (ext != null && ext.AlwaysInherit)
                {
                    inheritedGenes.Add(gene);
                    if (gene.exclusionTags != null)
                    {
                        foreach (var t in gene.exclusionTags)
                            inheritedExclusionTags.Add(t);
                    }
                    continue;
                }

                bool inA = genesA.Contains(gene);
                bool inB = genesB.Contains(gene);
                float chance = ext?.InheritChance ?? 1.0f;

                if (inA && inB)
                {
                    if (chance >= 1.0f || Rand.Chance(chance))
                    {
                        inheritedGenes.Add(gene);
                        if (gene.exclusionTags != null)
                        {
                            foreach (var t in gene.exclusionTags)
                                inheritedExclusionTags.Add(t);
                        }
                    }
                }
                else
                {
                    float uniqueChance = (chance >= 1.0f) ? 0.5f : chance * 0.5f;
                    if (Rand.Chance(uniqueChance))
                    {
                        inheritedGenes.Add(gene);
                        if (gene.exclusionTags != null)
                        {
                            foreach (var t in gene.exclusionTags)
                                inheritedExclusionTags.Add(t);
                        }
                    }
                }
            }

            // Guarantee at least one "MD_DisplayEye" and "MD_DisplayEyeColor" gene if available and not already present
            string[] requiredTags = { "MD_DisplayEye", "MD_DisplayEyeColor" };
            foreach (string tag in requiredTags)
            {
                bool alreadyInherited = inheritedGenes.Any(g => g.exclusionTags != null && g.exclusionTags.Contains(tag));
                if (!alreadyInherited && !inheritedExclusionTags.Contains(tag))
                {
                    var candidates = genesA.Concat(genesB)
                        .Where(g => g.exclusionTags != null && g.exclusionTags.Contains(tag))
                        .Distinct()
                        .Where(g => g.exclusionTags.All(t => !inheritedExclusionTags.Contains(t)))
                        .ToList();

                    if (candidates.Count > 0)
                    {
                        var chosenGene = candidates[Rand.Range(0, candidates.Count)];
                        inheritedGenes.Add(chosenGene);
                        foreach (var t in chosenGene.exclusionTags)
                            inheritedExclusionTags.Add(t);
                    }
                }
            }

            // Never inherit MD_BabyDisplayEyes, replace with parent's display eye gene if possible
            if (inheritedGenes.Contains(MD_DefOf.MD_BabyDisplayEyes))
            {
                inheritedGenes.Remove(MD_DefOf.MD_BabyDisplayEyes);

                // Find parent's display eye genes (excluding the baby version)
                var parentDisplayEyes = genesA.Concat(genesB)
                    .Where(g => g.exclusionTags != null && g.exclusionTags.Contains("MD_DisplayEye") && g != MD_DefOf.MD_BabyDisplayEyes)
                    .Distinct()
                    .ToList();

                if (parentDisplayEyes.Count > 0)
                {
                    var chosenGene = parentDisplayEyes[Rand.Range(0, parentDisplayEyes.Count)];
                    inheritedGenes.Add(chosenGene);
                }
            }

            return inheritedGenes.ToList();
        }

        /// <summary>
        /// Returns all inheritable genes (endo or xeno) from a pawn.
        /// </summary>
        public static IEnumerable<GeneDef> GetInheritableGenes(Pawn pawn)
        {
            if (pawn?.genes == null)
                yield break;

            foreach (var gene in pawn.genes.Endogenes)
                yield return gene.def;
            foreach (var gene in pawn.genes.Xenogenes)
                yield return gene.def;
        }

        /// <summary>
        /// Applies the given genes to the target pawn, adding each gene if not already present.
        /// </summary>
        /// <param name="target">The pawn to apply genes to.</param>
        /// <param name="genes">The list of GeneDefs to apply.</param>
        public static void ApplyAssignedGenesToPawn(Pawn target, IEnumerable<GeneDef> genes)
        {
            if (target?.genes == null || genes == null)
                return;

            foreach (var geneDef in genes)
            {
                if (!target.genes.HasActiveGene(geneDef))
                {
                    target.genes.AddGene(geneDef, false);
                }
            }
        }

        public static void RemoveAllGenesAndResetXenotype(Pawn pawn)
        {
            if (pawn?.genes == null)
                return;

            // Remove all endogenes
            for (int i = pawn.genes.Endogenes.Count - 1; i >= 0; i--)
                pawn.genes.RemoveGene(pawn.genes.Endogenes[i]);

            // Remove all xenogenes
            for (int i = pawn.genes.Xenogenes.Count - 1; i >= 0; i--)
                pawn.genes.RemoveGene(pawn.genes.Xenogenes[i]);

            // Reset xenotype to Baseliner
            pawn.genes.SetXenotype(XenotypeDefOf.Baseliner);

            // Clear custom xenotype name/icon
            pawn.genes.xenotypeName = null;
            pawn.genes.iconDef = null;
        }



        /// <summary>
        /// Applies a skin color to the target pawn, using either one parent's color or a blend.
        /// </summary>
        /// <param name="target">The pawn to apply the skin color to.</param>
        /// <param name="colorA">First parent's skin color.</param>
        /// <param name="colorB">Second parent's skin color.</param>
        /// <param name="blendFactor">0.0 = only colorA, 1.0 = only colorB, 0.5 = equal blend.</param>
        public static void ApplyInheritedSkinColor(Pawn target, Color colorA, Color colorB, float blendFactor = 0.5f)
        {
            if (target?.story == null)
                return;

            Color finalColor;
            float rand = Rand.Value;
            if (rand < 0.45f)
            {
                finalColor = colorA;
            }
            else if (rand < 0.90f)
            {
                finalColor = colorB;
            }
            else
            {
                // Rarely, use a blend
                blendFactor = Mathf.Clamp01(blendFactor);
                finalColor = Color.Lerp(colorA, colorB, blendFactor);
            }

            target.story.skinColorOverride = finalColor;
            target.Drawer?.renderer?.SetAllGraphicsDirty();
        }
        public static bool GeneSetHasDisplayEye(IEnumerable<GeneDef> genes)
        {
            return genes.Any(g => g.exclusionTags != null && g.exclusionTags.Contains("MD_DisplayEye"));
        }
        public static bool ParentHasDisplayEyes(Pawn parent)
        {
            return GetInheritableGenes(parent)
                .Any(g => g.exclusionTags != null && g.exclusionTags.Contains("MD_DisplayEye"));
        }
        public static Pawn SelectFavoredParent(Pawn parentA, Pawn parentB, IEnumerable<GeneDef> genesA, IEnumerable<GeneDef> genesB)
        {
            float favorA = 0f;
            float favorB = 0f;

            // For each gene in parentA, roll for favor
            foreach (var gene in genesA)
            {
                var ext = gene.GetModExtension<GeneInheritExtension>();
                if (ext?.FavoredParentChance != null)
                {
                    if (Rand.Chance(ext.FavoredParentChance.Value))
                        favorA += 1f;
                }
            }

            // For each gene in parentB, roll for favor
            foreach (var gene in genesB)
            {
                var ext = gene.GetModExtension<GeneInheritExtension>();
                if (ext?.FavoredParentChance != null)
                {
                    if (Rand.Chance(ext.FavoredParentChance.Value))
                        favorB += 1f;
                }
            }

            // If either parent has a higher favor score, select them
            if (favorA > favorB)
                return parentA;
            if (favorB > favorA)
                return parentB;

            // If scores are equal, pick randomly
            return Rand.Value < 0.5f ? parentA : parentB;
        }
    }
}

