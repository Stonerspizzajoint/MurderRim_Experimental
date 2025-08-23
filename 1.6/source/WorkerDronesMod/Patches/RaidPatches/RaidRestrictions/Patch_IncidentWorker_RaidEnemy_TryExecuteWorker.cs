using RimWorld;
using System.Collections.Generic;
using Verse;
using HarmonyLib;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
    public static class Patch_IncidentWorker_RaidEnemy_TryExecuteWorker
    {
        public static bool Prefix(IncidentParms parms, IncidentWorker_RaidEnemy __instance, ref bool __result)
        {
            Log.Message("[RaidRestrictions] Prefix called for raid incident.");

            if (parms.faction == null)
            {
                bool resolved = Traverse.Create(__instance)
                    .Method("TryResolveRaidFaction", new object[] { parms })
                    .GetValue<bool>();
                if (!resolved || parms.faction == null)
                {
                    Log.Message("[RaidRestrictions] Faction could not be resolved, letting vanilla handle.");
                    return true;
                }
            }

            var ext = parms.faction.def.GetModExtension<RaidRestrictions>();
            var geneExt = parms.faction.def.GetModExtension<SolverGeneExtension>();
            if (ext != null && ext.onlyNighttime && parms.target is Map map)
            {
                Log.Message($"[RaidRestrictions] Faction {parms.faction.Name} has onlyNighttime restriction. Checking map safety...");
                if (!SolarUtil.IsMapSafeForSolvers(map, ext, geneExt))
                {
                    Log.Message("[RaidRestrictions] Map is NOT safe for solvers. Queuing raid and blocking execution.");
                    var comp = map.GetComponent<PendingRaidMapComponent>();
                    if (comp != null)
                    {
                        bool alreadyQueued = comp.Pending.Exists(pr => pr.incidentDef == __instance.def && pr.parms == parms);
                        if (!alreadyQueued)
                        {
                            comp.Pending.Add(new PendingRaid
                            {
                                incidentDef = __instance.def,
                                parms = parms
                            });

                            // Send a warning letter/message to the player
                            string witness = "MD.NightRaid_WitnessFallback".Translate();
                            if (!map.mapPawns.FreeColonists.NullOrEmpty())
                            {
                                var pawn = map.mapPawns.FreeColonists.RandomElement();
                                witness = pawn.LabelShort;
                            }

                            string letterLabel = "MD.NightRaid_LetterLabel".Translate();
                            string letterText = "MD.NightRaid_LetterText".Translate(witness);
                            Find.LetterStack.ReceiveLetter(
                                letterLabel,
                                letterText,
                                LetterDefOf.ThreatSmall,
                                new TargetInfo(map.Center, map, false)
                            );

                            Log.Message("[RaidRestrictions] Raid queued and player notified.");
                        }
                        else
                        {
                            Log.Message("[RaidRestrictions] Raid already queued, not adding duplicate.");
                        }
                    }
                    else
                    {
                        Log.Error("[RaidRestrictions] PendingRaidMapComponent missing on map!");
                    }
                    __result = false;
                    return false; // Block immediate execution
                }
                else
                {
                    Log.Message("[RaidRestrictions] Map is safe for solvers. Allowing raid to proceed.");
                }
            }
            else
            {
                Log.Message("[RaidRestrictions] No restriction or not a map target. Allowing raid to proceed.");
            }
            return true; // Allow raid if not restricted or map is safe
        }
    }
}














