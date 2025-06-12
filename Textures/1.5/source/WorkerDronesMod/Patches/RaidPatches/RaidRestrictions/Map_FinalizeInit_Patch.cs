using HarmonyLib;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Map), "FinalizeInit")]
    public static class Map_FinalizeInit_Patch
    {
        public static void Postfix(Map __instance)
        {
            if (__instance.GetComponent<MapComponent_RaidWatcher>() == null)
            {
                __instance.components.Add(new MapComponent_RaidWatcher(__instance));
            }
        }
    }
}

