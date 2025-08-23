using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorkerDronesMod
{
    /// <summary>
    /// Stores gene and parent info for baby androids, persists across saves.
    /// </summary>
    public class BabyAndroidGeneMemoryComponent : GameComponent, IExposable
    {
        /// <summary>
        /// Maps pawn ID to an InheritedGeneSet for baby androids.
        /// </summary>
        public Dictionary<int, InheritedGeneSet> babyGeneMemory = new Dictionary<int, InheritedGeneSet>();

        /// <summary>
        /// Maps pawn ID to parent pawn IDs.
        /// </summary>
        public Dictionary<int, int[]> babyParentMemory = new Dictionary<int, int[]>();

        /// <summary>
        /// Tracks if a pawn was ever a baby android.
        /// </summary>
        public Dictionary<int, bool> wasBornAsBabyAndroid = new Dictionary<int, bool>();

        public Dictionary<int, Color[]> babyParentSkinColors = new Dictionary<int, Color[]>();

        // Mark when a pawn is created as a baby android
        public void MarkAsBornAsBabyAndroid(int pawnId)
        {
            wasBornAsBabyAndroid[pawnId] = true;
        }

        // Check if a pawn was ever born as a baby android
        public bool WasBornAsBabyAndroid(int pawnId)
        {
            return wasBornAsBabyAndroid.ContainsKey(pawnId) && wasBornAsBabyAndroid[pawnId];
        }

        public void SetParentSkinColors(int babyPawnId, Color colorA, Color colorB)
        {
            babyParentSkinColors[babyPawnId] = new[] { colorA, colorB };
        }

        public Color[] GetParentSkinColors(int babyPawnId)
        {
            Color[] colors;
            babyParentSkinColors.TryGetValue(babyPawnId, out colors);
            return colors;
        }


        public BabyAndroidGeneMemoryComponent(Game game) : base() { }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref babyGeneMemory, "babyGeneMemory", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref babyParentMemory, "babyParentMemory", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref wasBornAsBabyAndroid, "wasBornAsBabyAndroid", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref babyParentSkinColors, "babyParentSkinColors", LookMode.Value, LookMode.Value);
        }

        /// <summary>
        /// Singleton accessor for this component.
        /// </summary>
        public static BabyAndroidGeneMemoryComponent Instance =>
            Current.Game.GetComponent<BabyAndroidGeneMemoryComponent>();

        /// <summary>
        /// Helper: Add or update gene memory for a pawn.
        /// </summary>
        public void SetGeneMemory(int pawnId, InheritedGeneSet geneSet)
        {
            if (geneSet == null) return;
            babyGeneMemory[pawnId] = geneSet;
        }

        /// <summary>
        /// Helper: Add or update parent memory for a pawn.
        /// </summary>
        public void SetParentMemory(int pawnId, int[] parentIds)
        {
            if (parentIds == null) return;
            babyParentMemory[pawnId] = parentIds;
        }

        /// <summary>
        /// Helper: Remove all memory for a pawn.
        /// </summary>
        public void RemovePawnMemory(int pawnId)
        {
            babyGeneMemory.Remove(pawnId);
            babyParentMemory.Remove(pawnId);
        }

        /// <summary>
        /// Helper: Get the gene set for a pawn, or null if not found.
        /// </summary>
        public InheritedGeneSet GetGeneSet(int pawnId)
        {
            babyGeneMemory.TryGetValue(pawnId, out var geneSet);
            return geneSet;
        }
    }

    /// <summary>
    /// Represents a set of inherited genes for a baby android.
    /// </summary>
    public class InheritedGeneSet : IExposable
    {
        private List<GeneDef> genes = new List<GeneDef>();
        private string name;

        public IReadOnlyList<GeneDef> Genes => genes;

        public void AddGene(GeneDef gene)
        {
            if (!genes.Contains(gene) && CanAddGene(gene))
            {
                genes.Add(gene);
            }
        }

        public bool CanAddGene(GeneDef gene)
        {
            // Check for conflicts, prerequisites, biostat limits, etc.
            // (Implement logic similar to GeneSet.CanAddGeneDuringGeneration)
            return true;
        }

        public int ComplexityTotal => genes.Sum(g => g.biostatCpx);
        public int MetabolismTotal => genes.Sum(g => g.biostatMet);
        public int ArchitesTotal => genes.Sum(g => g.biostatArc);

        public void ExposeData()
        {
            Scribe_Collections.Look(ref genes, "genes", LookMode.Def);
            Scribe_Values.Look(ref name, "name");
        }
    }
}
