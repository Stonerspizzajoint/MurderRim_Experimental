using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    /// <summary>
    /// This think node checks if the pawn has any hediff from a list with severity exceeding a threshold.
    /// If any one of the hediffs in the list is present with severity at or above a random value from the severityRange,
    /// then the condition returns true—indicating that the pawn cannot continue with the current behavior.
    /// </summary>
    public class ThinkNode_ConditionalHasAnyHediff : ThinkNode_Conditional
    {
        // A list of hediff definitions to check.
        public List<HediffDef> hediffList = new List<HediffDef>();

        // The severity range that is used to determine if a matching hediff is severe enough.
        public FloatRange severityRange = FloatRange.Zero;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_ConditionalHasAnyHediff copy = (ThinkNode_ConditionalHasAnyHediff)base.DeepCopy(resolve);
            // Create a new list with the same elements so that modifications on the copy don't affect the original.
            copy.hediffList = new List<HediffDef>(this.hediffList);
            copy.severityRange = this.severityRange;
            return copy;
        }

        protected override bool Satisfied(Pawn pawn)
        {
            // Iterate over every hediff definition in the list.
            if (hediffList != null)
            {
                foreach (HediffDef def in hediffList)
                {
                    // Try to get the first hediff matching the definition.
                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def, false);

                    // If found and the severity meets the threshold, then we consider the condition met.
                    if (hediff != null && hediff.Severity >= severityRange.RandomInRange)
                    {
                        // Pawn has one of the hediffs that disqualifies it (cannot continue).
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
