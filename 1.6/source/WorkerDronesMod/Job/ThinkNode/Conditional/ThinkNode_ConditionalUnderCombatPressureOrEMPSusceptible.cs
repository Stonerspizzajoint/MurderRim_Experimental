using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace WorkerDronesMod
{
    // Inherit from the base class for combat pressure and extend its logic.
    public class ThinkNode_ConditionalCombatByEMPWeak : ThinkNode_ConditionalUnderCombatPressure
    {
        protected override bool Satisfied(Pawn pawn)
        {
            // First use the base class's logic for combat pressure.
            if (base.Satisfied(pawn))
            {
                return true;
            }

            // If combat pressure isn't already met by the pawn's own state,
            // check nearby enemy targets for our specific conditions.
            List<Thing> nearbyThings =
                GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, this.maxThreatDistance, false).ToList();

            foreach (Thing thing in nearbyThings)
            {
                Pawn enemy = thing as Pawn;
                // Only consider enemy pawns that are not down or dead.
                if (enemy != null && enemy.HostileTo(pawn) && !enemy.Dead && !enemy.Downed)
                {
                    // Check if the enemy is a mechanoid.
                    bool isMechanoid = enemy.RaceProps?.IsMechanoid == true;
                    // Check if the enemy has the EMP vulnerability gene.
                    bool hasEMPVulnerability = enemy.genes != null && enemy.genes.HasActiveGene(MD_DefOf.VREA_EMPVulnerability);

                    if (isMechanoid || hasEMPVulnerability)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_ConditionalCombatByEMPWeak copy = (ThinkNode_ConditionalCombatByEMPWeak)base.DeepCopy(resolve);
            // Inherited fields (maxThreatDistance, etc.) have been copied by the base.DeepCopy call.
            return copy;
        }
    }
}
