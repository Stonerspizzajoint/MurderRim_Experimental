using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public static class SimpleFlyerUtility
    {
        /// <summary>
        /// Spawns the given pawn as a flyer.
        /// The pawn is first spawned (if not already) at the provided rootCell position.
        /// A landing cell is determined (within jumpDist) and the pawn is converted into a flyer,
        /// which is then spawned at that landing cell.
        /// </summary>
        /// <param name="pawn">The pawn to spawn as flyer.</param>
        /// <param name="map">The map on which to spawn the pawn.</param>
        /// <param name="rootCell">The starting cell (often the corpse position).</param>
        /// <param name="jumpDist">The maximum jump distance for landing.</param>
        /// <returns>The spawned PawnFlyer if successful, otherwise null.</returns>
        public static PawnFlyer SpawnPawnAsFlyer(Pawn pawn, Map map, IntVec3 rootCell, int jumpDist = 5)
        {
            // Ensure the pawn is spawned.
            if (!pawn.Spawned)
            {
                GenSpawn.Spawn(pawn, rootCell, map, WipeMode.Vanish);
            }

            // Try to find a valid landing cell near the provided rootCell.
            IntVec3 landingCell;
            bool found = RCellFinder.TryFindRandomCellNearWith(
                rootCell,
                (IntVec3 c) => c.InBounds(map) && c.Standable(map) && c.GetFirstPawn(map) == null,
                map,
                out landingCell,
                jumpDist, jumpDist);

            // If no landing cell is found, default to the root cell.
            if (!found)
            {
                landingCell = rootCell;
            }

            // Have the pawn face the landing cell.
            pawn.rotationTracker.FaceCell(landingCell);

            // Use the vanilla flyer ThingDef.
            ThingDef flyerDef = ThingDefOf.PawnFlyer;

            // Retrieve the landing sound definition.
            SoundDef landingSound = SoundDef.Named("Longjump_Land");

            // Create the flyer using PawnFlyer.MakeFlyer.
            PawnFlyer flyer = PawnFlyer.MakeFlyer(
                flyerDef,
                pawn,
                landingCell,
                null,            // flightEffecterDef (optional)
                landingSound,    // landing sound
                false,           // flyWithCarriedThing
                null,            // overrideStartVec (optional)
                null,            // triggeringAbility (optional)
                default(LocalTargetInfo)  // target (optional)
            );

            if (flyer != null)
            {
                // Spawn the flyer at the landing cell.
                GenSpawn.Spawn(flyer, landingCell, map, WipeMode.Vanish);
            }

            return flyer;
        }
    }
}
