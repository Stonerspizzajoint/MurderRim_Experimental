using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class CompProperties_AbilityFleckOnCaster : CompProperties_AbilityEffect
    {
        public FleckDef fleckDef;
        public List<FleckDef> fleckDefs;
        public float scale = 1f;
        public int preCastTicks;
        public bool UseSkinColor = false;

        public CompProperties_AbilityFleckOnCaster()
        {
            this.compClass = typeof(CompAbilityEffect_FleckOnCaster);
        }
    }
}

