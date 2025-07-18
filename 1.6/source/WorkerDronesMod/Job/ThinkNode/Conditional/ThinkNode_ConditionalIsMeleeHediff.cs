using Verse;
using RimWorld;
using Verse.AI;
using System.Linq;

namespace WorkerDronesMod
{
    public class ThinkNode_ConditionalIsMeleeHediff : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            // Check for melee hediff
            var ext = JumpAbilityAIUtil.GetHediffSwitcherExtension(pawn);
            if (ext != null)
            {
                foreach (var hand in pawn.RaceProps.body.GetPartsWithDef(MD_DefOf.Hand))
                {
                    var hediff = pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.Part == hand);
                    if (hediff != null)
                    {
                        var option = ext.selectableHediffs.FirstOrDefault(o => o.Hediff == hediff.def);
                        if (option != null && option.IsMelee)
                            return true;
                    }
                }
            }

            // Check for melee-only weapon (no ranged verbs)
            var eq = pawn.equipment?.Primary;
            if (eq != null)
            {
                // If all verbs are melee, it's a melee weapon
                if (eq.def.Verbs.All(v => v.IsMeleeAttack))
                    return true;
            }

            return false;
        }
    }
}

