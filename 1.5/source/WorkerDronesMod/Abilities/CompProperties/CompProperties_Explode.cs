using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class CompProperties_Explode : CompProperties_AbilityEffect
    {
        public CompProperties_Explode()
        {
            this.compClass = typeof(CompExplode);
        }

        public float radius;

        public DamageDef damageType;

        public int damageAmount = -1;

        public float damagePenetration = -1f;

        public SoundDef soundCreated = null;

        public ThingDef thingCreated = null;

        public float thingCreatedChance = 0f;

        public float chanceToStartFire = 0f;

        public bool damageUser = true;
    }
}
