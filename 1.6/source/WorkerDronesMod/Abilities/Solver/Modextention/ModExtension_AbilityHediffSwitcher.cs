using Verse;
using System.Collections.Generic;

namespace WorkerDronesMod
{
    public class ModExtension_AbilityHediffSwitcher : DefModExtension
    {
        public List<SelectableHediffOption> selectableHediffs;

        public class SelectableHediffOption
        {
            public HediffDef Hediff;
            public string IconPath;
            public List<GeneDef> requiredGenes;
            public bool AIfavored;
            public bool IsMelee;   // True if this is a melee weapon hediff
            public bool IsRanged;  // True if this is a ranged weapon hediff
            public bool IsDefault; // True if this is the normal hand hediff
        }
    }
}

