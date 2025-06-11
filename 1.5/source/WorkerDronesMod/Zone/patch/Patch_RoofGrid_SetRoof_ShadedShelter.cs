using System.Linq;
using HarmonyLib;
using Verse;

namespace WorkerDronesMod
{
    [HarmonyPatch(typeof(RoofGrid), "SetRoof", new[] { typeof(IntVec3), typeof(RoofDef) })]
    public static class Patch_RoofGrid_SetRoof_ShadedShelter
    {
        [HarmonyPostfix]
        public static void Postfix(RoofGrid __instance, IntVec3 c, RoofDef def)
        {
            // Retrieve the private "map" field from the RoofGrid instance using Traverse.
            Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
            if (map == null)
                return;

            // Get our custom zone (Area_Shade) from the map's area manager.
            Area_Shade shade = map.areaManager.AllAreas.OfType<Area_Shade>().FirstOrDefault();
            if (shade == null)
                return;

            // Optionally, update only cells that are in the Home area.
            bool inHome = (map.areaManager.Home != null) && map.areaManager.Home[c];

            // Update our custom zone: include the cell if it is in Home and a roof is set (def is not null).
            shade[c] = inHome && (def != null);
        }
    }
}









