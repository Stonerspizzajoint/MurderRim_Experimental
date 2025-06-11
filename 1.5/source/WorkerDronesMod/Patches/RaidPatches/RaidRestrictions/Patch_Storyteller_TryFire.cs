using RimWorld;
using Verse;
using HarmonyLib;
using System.Collections.Generic;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Storyteller), "TryFire")]
    public static class Patch_Storyteller_TryFire_CacheFaction
    {
        public static class IncidentFactionCache
        {
            // We key the cache on the same IncidentParms instance.
            public static Dictionary<IncidentParms, Faction> cache = new Dictionary<IncidentParms, Faction>();
        }

        public static void Prefix(FiringIncident fi, bool queued)
        {
            if (fi.def != null && fi.def == IncidentDefOf.RaidEnemy && fi.parms != null && fi.parms.faction != null)
            {
                // Cache the faction using the IncidentParms as the key.
                IncidentFactionCache.cache[fi.parms] = fi.parms.faction;
                Log.Message($"[RaidRestrictions Cache] Cached faction {fi.parms.faction.def.defName} for incident {fi.def.defName}.");
            }
        }

        // Optionally, clean up the cache when the incident fires successfully.
        public static void Postfix(FiringIncident fi, bool __result)
        {
            if (fi.parms != null && IncidentFactionCache.cache.ContainsKey(fi.parms))
            {
                IncidentFactionCache.cache.Remove(fi.parms);
                Log.Message($"[RaidRestrictions Cache] Removed cached faction for incident {fi.def.defName}.");
            }
        }
    }
}
