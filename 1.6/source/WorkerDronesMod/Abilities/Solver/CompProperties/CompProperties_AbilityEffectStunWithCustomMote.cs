using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class CompProperties_AbilityEffectStunWithCustomMote : CompProperties_AbilityEffectWithDuration
    {
        public ThingDef customStunMoteDef;
        public float stunMoteSpinSpeed = 0f; // degrees per second, default 0 (no spin)

        public CompProperties_AbilityEffectStunWithCustomMote()
        {
            this.compClass = typeof(CompAbilityEffect_StunWithCustomMote);
        }
    }
}


