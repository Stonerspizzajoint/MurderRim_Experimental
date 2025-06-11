using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class CompProperties_CycleHediff : CompProperties_AbilityEffect
    {
        // Base list of interchangeable hediffs for player-controlled pawns.
        public List<HediffDef> hediffs = new List<HediffDef>();

        // List of gene-locked hediffs.
        public List<GeneLockedHediff> geneLockedHediffs = new List<GeneLockedHediff>();

        // New bool: if true, only apply the hediff to one hand.
        public bool onlyOneHand = false;

        // New: lists for NPCs so you can choose favored weapons via XML.
        public List<HediffDef> npcMeleeHediffs = new List<HediffDef>();
        public List<HediffDef> npcRangedHediffs = new List<HediffDef>();

        public CompProperties_CycleHediff()
        {
            this.compClass = typeof(CompAbility_CycleHediff);
        }
    }

    public class GeneLockedHediff
    {
        // The defName of the hediff to add (e.g., "MD_interchangeableRanged_SMGhand").
        public string hediffDefName;
        // The defName of the gene required (e.g., "MD_interchangeableRanged_Upgrade").
        public string requiredGeneDefName;
    }
}


