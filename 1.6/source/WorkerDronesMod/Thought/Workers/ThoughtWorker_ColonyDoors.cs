using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    // Custom ThoughtWorker that scales mood based on colony-owned door count.
    public class ThoughtWorker_ColonyDoors : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            // Only consider pawns with the MD_DoorEnthusiast trait.
            if (!p.story.traits.HasTrait(TraitDef.Named("MD_DoorEnthusiast")))
            {
                return ThoughtState.Inactive;
            }
            if (p.Map == null)
            {
                return ThoughtState.Inactive;
            }

            // Count all colony-owned doors on the map.
            List<Building_Door> colonyDoors = p.Map.listerBuildings.AllBuildingsColonistOfClass<Building_Door>().ToList();
            int doorCount = colonyDoors.Count;

            // Determine stage based on door count thresholds.
            int stage;
            if (doorCount == 0)
            {
                stage = 0;  // No doors: Strong negative mood.
            }
            else if (doorCount <= 2)
            {
                stage = 1;  // 1–2 doors: Slightly negative.
            }
            else if (doorCount <= 5)
            {
                stage = 2;  // 3–5 doors: Neutral.
            }
            else if (doorCount <= 39)
            {
                stage = 3;  // 6–20 doors: Positive.
            }
            else if (doorCount <= 40)
            {
                stage = 4;  // 21-40 or more doors: Strongly positive.
            }
            else
            {
                stage = 4;  // 40 or more doors: Strongly positive.
            }

            return ThoughtState.ActiveAtStage(stage);
        }
    }
}
