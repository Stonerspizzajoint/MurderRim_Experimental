using Verse;

namespace WorkerDronesMod
{
    // Define the properties for the gun-hand hediff.
    public class HediffCompProperties_GunHand : HediffCompProperties
    {
        // The weapon that will be given to the pawn.
        public ThingDef weaponDef;

        public HediffCompProperties_GunHand()
        {
            compClass = typeof(HediffComp_GunHand);
        }
    }
}
