using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorkerDronesMod
{
    public class Command_AbilityHediffSwitcher : Command_Ability
    {
        public Command_AbilityHediffSwitcher(Ability ability, Pawn pawn) : base(ability, pawn) { }

        public override void ProcessInput(Event ev)
        {
            var ext = ability.def.GetModExtension<ModExtension_AbilityHediffSwitcher>();
            var pawn = ability.pawn;
            if (ext == null || ext.selectableHediffs == null || pawn == null)
            {
                Messages.Message("No selectable modes available.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var options = new List<FloatMenuOption>();
            foreach (var option in ext.selectableHediffs)
            {
                bool hasAllGenes = true;
                if (option.requiredGenes != null && option.requiredGenes.Count > 0)
                {
                    if (pawn.genes == null)
                    {
                        hasAllGenes = false;
                    }
                    else
                    {
                        foreach (var geneDef in option.requiredGenes)
                        {
                            if (!pawn.genes.HasActiveGene(geneDef))
                            {
                                hasAllGenes = false;
                                break;
                            }
                        }
                    }
                }

                if (!hasAllGenes)
                    continue;

                Texture2D iconTex = null;
                if (!string.IsNullOrEmpty(option.IconPath))
                    iconTex = ContentFinder<Texture2D>.Get(option.IconPath, true);

                string label = option.Hediff.label;
                options.Add(new FloatMenuOption(label, () =>
                {
                    var switcher = ability as Ability_HediffSwitcher;
                    if (switcher != null)
                    {
                        switcher.selectedOption = option;
                    }
                    base.ProcessInput(ev);
                }, iconTex, Color.white));
            }
            if (options.Count > 0)
                Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}

