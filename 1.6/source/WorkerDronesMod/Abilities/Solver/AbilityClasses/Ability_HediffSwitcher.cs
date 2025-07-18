using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorkerDronesMod
{
    public class Ability_HediffSwitcher : Ability
    {
        // The currently selected hediff option (set by the gizmo)
        public ModExtension_AbilityHediffSwitcher.SelectableHediffOption selectedOption;

        public Ability_HediffSwitcher() : base() { }
        public Ability_HediffSwitcher(Pawn pawn) : base(pawn) { }
        public Ability_HediffSwitcher(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        // This method is called when the ability is cast (after targeting, etc.)
        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            bool result = base.Activate(target, dest);

            if (selectedOption != null && pawn != null)
            {
                var ext = def.GetModExtension<ModExtension_AbilityHediffSwitcher>();
                if (ext != null)
                {
                    var hands = pawn.RaceProps.body.GetPartsWithDef(MD_DefOf.Hand).ToList();

                    // Remove all other selectable hediffs from hands
                    foreach (var hand in hands)
                    {
                        foreach (var hdOpt in ext.selectableHediffs)
                        {
                            if (hdOpt.Hediff != selectedOption.Hediff)
                            {
                                var existing = pawn.health.hediffSet.hediffs
                                    .FirstOrDefault(h => h.def == hdOpt.Hediff && h.Part == hand);
                                if (existing != null)
                                    pawn.health.RemoveHediff(existing);
                            }
                        }
                    }

                    if (selectedOption.IsRanged)
                    {
                        // Apply to only one hand (the first available)
                        if (hands.Count > 0)
                        {
                            var hand = hands[0];
                            bool hasHediff = pawn.health.hediffSet.hediffs
                                .Any(h => h.def == selectedOption.Hediff && h.Part == hand);
                            if (!hasHediff)
                            {
                                pawn.health.AddHediff(selectedOption.Hediff, hand);
                            }
                        }
                    }
                    else
                    {
                        // Apply to all hands
                        foreach (var hand in hands)
                        {
                            bool hasHediff = pawn.health.hediffSet.hediffs
                                .Any(h => h.def == selectedOption.Hediff && h.Part == hand);
                            if (!hasHediff)
                            {
                                pawn.health.AddHediff(selectedOption.Hediff, hand);
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}

