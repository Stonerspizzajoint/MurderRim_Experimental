using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public static class AutoRoofedZoneMaker
    {
        /// <summary>
        /// Automatically creates a zone containing all roofed cells from the Home area.
        /// The new zone will be an instance of Area_Shade.
        /// </summary>
        /// <param name="map">The current map.</param>
        /// <returns>The created zone, or null if no Home area exists.</returns>
        public static Area_Shade CreateAutoRoofedZone(Map map)
        {
            if (map == null)
                return null;

            // Get the Home area (player-designated).
            Area_Home homeArea = map.areaManager.Home;
            if (homeArea == null)
                return null;

            // Create a new Area_Shade instance using map.areaManager.
            Area_Shade roofedZone = new Area_Shade(map.areaManager);

            // Iterate over all cells in the Home area and mark each cell as included if it is roofed.
            foreach (IntVec3 cell in homeArea.ActiveCells)
            {
                roofedZone[cell] = cell.Roofed(map);
            }

            // Add the new area to the map's AreaManager if not already added.
            if (!map.areaManager.AllAreas.Contains(roofedZone))
            {
                map.areaManager.AllAreas.Add(roofedZone);
            }
            return roofedZone;
        }
    }
}





