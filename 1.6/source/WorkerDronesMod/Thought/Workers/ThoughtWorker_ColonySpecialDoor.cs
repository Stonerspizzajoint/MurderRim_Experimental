using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    // Custom ThoughtWorker with three mood stages based on door type.
    public class ThoughtWorker_ColonySpecialDoor : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            // Only apply for pawns with the MD_DoorEnthusiast trait.
            if (!p.story.traits.HasTrait(TraitDef.Named("MD_DoorEnthusiast")))
            {
                return ThoughtState.Inactive;
            }
            if (p.Map == null)
            {
                return ThoughtState.Inactive;
            }

            // Get all colony-owned doors on the map.
            List<Building_Door> colonyDoors = p.Map.listerBuildings.AllBuildingsColonistOfClass<Building_Door>().ToList();

            // Check for a security door first.
            bool securityDoorExists = colonyDoors.Any(door =>
            {
                // Ensure the door is powered.
                CompPowerTrader compPower = door.TryGetComp<CompPowerTrader>();
                bool powered = compPower == null || compPower.PowerOn;
                if (!powered) return false;

                // Security door check: defName contains "Security".
                return door.def.defName.Contains("Security");
            });

            if (securityDoorExists)
            {
                return ThoughtState.ActiveAtStage(3);
            }

            // Next, check for a special door (Auto or Ornate).
            bool specialDoorExists = colonyDoors.Any(door =>
            {
                CompPowerTrader compPower = door.TryGetComp<CompPowerTrader>();
                bool powered = compPower == null || compPower.PowerOn;
                if (!powered) return false;

                return door.def.defName.Contains("Auto") || door.def.defName.Contains("Ornate");
            });

            if (specialDoorExists)
            {
                return ThoughtState.ActiveAtStage(1);
            }

            // Default: no qualifying door found.
            return ThoughtState.ActiveAtStage(0);
        }
    }
}


