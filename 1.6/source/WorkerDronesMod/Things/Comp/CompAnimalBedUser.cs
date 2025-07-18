using Verse;

namespace WorkerDronesMod
{
    // This component can be added to pawn defs to allow them to use animal beds.
    public class CompAnimalBedUser : ThingComp
    {
        public CompProperties_AnimalBedUser Props => (CompProperties_AnimalBedUser)this.props;

        // You could have runtime state here if needed; for now, we simply rely on the properties.
        // Optionally, you could cache the value in a field but here we delegate directly to Props.
    }

}
