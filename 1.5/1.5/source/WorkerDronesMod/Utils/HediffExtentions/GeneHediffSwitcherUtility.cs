using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    /// <summary>
    /// Utility for mapping a pawn’s dynamic “state” (downed, high heat, etc.)
    /// to a specific hediff, and ensuring that only the correct hediff is active.
    /// </summary>
    public static class GeneHediffSwitcherUtility
    {
        private static readonly BodyPartDef HeadDef = MD_DefOf.Head;

        /// <summary>
        /// Applies the hediff corresponding to the pawn’s current state, 
        /// removing any others from the same mapping. Only processes the update if the pawn is on a valid map.
        /// A target body part (in this example, the head) is chosen; if it’s missing, we log a warning and exit.
        /// </summary>
        public static void UpdateGeneHediff(Pawn pawn, Gene gene)
        {
            if (pawn == null || gene == null || pawn.health?.hediffSet == null)
                return;

            var ext = gene.def.GetModExtension<GeneHediffSwitcherExtension>();
            if (ext?.stateHediffs == null || ext.stateHediffs.Count == 0)
                return;

            PawnState state = GetPawnState(pawn);

            // Determine if this gene is "toggled-off capable" (i.e. it defines a ToggledOff mapping)
            bool hasToggledOffMapping = ext.stateHediffs.Any(s => s.state == PawnState.ToggledOff);

            if (hasToggledOffMapping)
            {
                // If the pawn is toggled off, then remove any hediffs defined in this gene’s extension
                if (state == PawnState.ToggledOff)
                {
                    var geneHediffDefs = new HashSet<HediffDef>(ext.stateHediffs.Select(s => s.hediffDef));
                    for (int i = pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                    {
                        var h = pawn.health.hediffSet.hediffs[i];
                        if (geneHediffDefs.Contains(h.def))
                        {
                            pawn.health.RemoveHediff(h);
                        }
                    }
                    return;
                }

                // For toggled-off capable genes, only allow a hediff if there is an explicit mapping for the current state.
                GeneHediffSwitcherState mapping = ext.stateHediffs.FirstOrDefault(s => s.state == state);
                if (mapping == null || mapping.hediffDef == null)
                {
                    // No explicit mapping exists for this state—prevent any hediff from spawning.
                    return;
                }

                // If the target hediff is already applied, nothing more to do.
                if (pawn.health.hediffSet.hediffs.Any(h => h.def == mapping.hediffDef))
                    return;

                // Remove any existing hediffs associated with this gene.
                var geneHediffDefsToRemove = new HashSet<HediffDef>(ext.stateHediffs.Select(s => s.hediffDef));
                for (int i = pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                {
                    var h = pawn.health.hediffSet.hediffs[i];
                    if (geneHediffDefsToRemove.Contains(h.def))
                    {
                        pawn.health.RemoveHediff(h);
                    }
                }

                // Use the hediff’s default installation part.
                if (mapping.hediffDef.defaultInstallPart == null)
                    return;

                BodyPartRecord targetPart = pawn.RaceProps.body.AllParts
                    .FirstOrDefault(part => part.def == mapping.hediffDef.defaultInstallPart);
                if (targetPart == null || pawn.health.hediffSet.PartIsMissing(targetPart))
                    return;

                pawn.health.AddHediff(mapping.hediffDef, targetPart);
                return;
            }
            else
            {
                // For genes that do NOT define a toggled-off mapping, use the usual logic—
                // attempt to match the pawn's state or fall back to the Normal mapping.
                GeneHediffSwitcherState mapping = ext.stateHediffs.FirstOrDefault(s => s.state == state)
                                                  ?? ext.stateHediffs.FirstOrDefault(s => s.state == PawnState.Normal);
                if (mapping == null || mapping.hediffDef == null)
                    return;

                if (pawn.health.hediffSet.hediffs.Any(h => h.def == mapping.hediffDef))
                    return;

                var geneHediffDefs = new HashSet<HediffDef>(ext.stateHediffs.Select(s => s.hediffDef));
                for (int i = pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                {
                    var h = pawn.health.hediffSet.hediffs[i];
                    if (geneHediffDefs.Contains(h.def))
                    {
                        pawn.health.RemoveHediff(h);
                    }
                }

                // Again, use the hediff's default installation part.
                if (mapping.hediffDef.defaultInstallPart == null)
                    return;

                BodyPartRecord targetPart = pawn.RaceProps.body.AllParts
                    .FirstOrDefault(part => part.def == mapping.hediffDef.defaultInstallPart);
                if (targetPart == null || pawn.health.hediffSet.PartIsMissing(targetPart))
                    return;

                pawn.health.AddHediff(mapping.hediffDef, targetPart);
                return;
            }
        }

        /// <summary>
        /// Removes all hediffs associated with a gene from the pawn.
        /// This is usually called when the gene is removed.
        /// </summary>
        /// <param name="pawn">The pawn from which to remove associated hediffs.</param>
        /// <param name="gene">The gene whose associated hediffs are to be removed.</param>
        public static void RemoveGeneHediff(Pawn pawn, Gene gene)
        {
            if (pawn == null || gene == null || pawn.health?.hediffSet == null)
                return;

            var ext = gene.def.GetModExtension<GeneHediffSwitcherExtension>();
            if (ext?.stateHediffs == null || ext.stateHediffs.Count == 0)
                return;

            // Gather all hediffs defined in the gene's extension.
            HashSet<HediffDef> geneHediffDefs = new HashSet<HediffDef>(ext.stateHediffs.Select(s => s.hediffDef));

            // Iterate in reverse order to safely remove hediffs.
            for (int i = pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
            {
                Hediff h = pawn.health.hediffSet.hediffs[i];
                if (geneHediffDefs.Contains(h.def))
                {
                    pawn.health.RemoveHediff(h);
                }
            }
        }

        /// <summary>
        /// Determines the pawn’s current state in a priority order.
        /// </summary>
        public static PawnState GetPawnState(Pawn pawn)
        {
            if (pawn == null)
                return PawnState.Normal;

            // 1) HighHeat (as determined by Gene_HeatBuildup)
            if (pawn.genes != null)
            {
                for (int i = 0; i < pawn.genes.GenesListForReading.Count; i++)
                {
                    if (pawn.genes.GenesListForReading[i] is Gene_HeatBuildup heatGene)
                    {
                        if (heatGene.InitialResourceMax > 0f &&
                            heatGene.Value / heatGene.InitialResourceMax > 0.6f)
                        {
                            return PawnState.HighHeat;
                        }
                        break;
                    }
                }
            }

            // 2) Downed  
            if (pawn.Downed)
                return PawnState.Downed;

            // 3) Sleeping (not awake and not drafted)
            if (!pawn.Awake() && !pawn.Drafted)
                return PawnState.Sleeping;

            // 4) Confused mental state   
            if (pawn.InMentalState && pawn.MentalState is MentalState_ConfusedWander confused && confused.IsConfused)
                return PawnState.IsConfused;

            // 5) Ability warmup  
            if (pawn.jobs?.curJob != null && pawn.jobs.curJob.def.defName == "CastAbilityOnThing")
            {
                var verb = pawn.jobs.curJob.verbToUse;
                if (verb != null && verb.WarmupTicksLeft > 0)
                {
                    return PawnState.AbilityWarmup;
                }
            }

            // 6) Hostile to player  
            if (pawn.Faction != null && pawn.HostileTo(Faction.OfPlayerSilentFail))
                return PawnState.Hostile;

            // 7) Check for toggled-off abilities. 
            if (pawn.abilities != null)
            {
                foreach (var ability in pawn.abilities.abilities)
                {
                    var toggleComp = ability.CompOfType<Comp_ToggleHediffEffect>();
                    if (toggleComp != null && !toggleComp.toggledOn)
                    {
                        return PawnState.ToggledOff;
                    }
                }
            }

            // 8) Drafted  
            if (pawn.Drafted)
                return PawnState.Drafted;

            // 9) Default – Normal state  
            return PawnState.Normal;
        }

        public enum PawnState
        {
            IsConfused,
            Downed,
            Sleeping,
            AbilityWarmup,
            Drafted,
            HighHeat,
            Hostile,
            Normal,
            ToggledOff  // NEW state indicating the toggle is off.
        }
    }
}