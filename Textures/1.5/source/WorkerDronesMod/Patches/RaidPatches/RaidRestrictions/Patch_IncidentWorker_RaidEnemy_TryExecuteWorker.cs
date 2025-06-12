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
            // If the faction is null, try to re-resolve it using the protected method via Traverse.
            if (parms.faction == null)
            {
                Log.Message("[RaidRestrictions] Faction is null. Attempting to resolve via TryResolveRaidFaction.");
                bool resolved = Traverse.Create(__instance)
                    .Method("TryResolveRaidFaction", new object[] { parms })
                    .GetValue<bool>();
                if (resolved && parms.faction != null)
                {
                    Log.Message($"[RaidRestrictions] Successfully resolved faction: {parms.faction.def.defName}.");
                }
                else
                {
                    Log.Message("[RaidRestrictions] Faction is null and could not be resolved via TryResolveRaidFaction. Proceeding with default execution.");
                    return true; // Proceed with default handling if resolution fails.
                }
            }
            else
            {
                Log.Message($"[RaidRestrictions] TryExecuteWorker invoked with faction: {parms.faction.def.defName} and target: {parms.target}");
            }

            var ext = parms.faction.def.GetModExtension<RaidRestrictions>();
            if (ext != null && ext.onlyNighttime)
            {
                Log.Message("[RaidRestrictions] Found RaidRestrictions extension with onlyNighttime enabled.");

                if (parms.target is Map map)
                {
                    bool isSunSafe = SolverGeneUtility.IsMapSafeFromSun(map);
                    Log.Message($"[RaidRestrictions] Map target detected. Current sky glow: {map.skyManager.CurSkyGlow}. IsMapSafeFromSun returns: {isSunSafe}");

                    if (!isSunSafe)
                    {
                        Log.Message("[RaidRestrictions] Map is NOT sun safe so proceeding to queue the raid instead of immediate execution.");

                        // Queue the raid for later.
                        var comp = map.GetComponent<PendingRaidMapComponent>();
                        if (comp == null)
                        {
                            Log.Error($"[RaidRestrictions] PendingRaidMapComponent missing on map {map}.");
                        }
                        else
                        {
                            comp.Pending.Add(new PendingRaid
                            {
                                worker = __instance,
                                parms = parms
                            });
                            Log.Message("[RaidRestrictions] Raid has been queued in PendingRaidMapComponent.");

                            // Pick a random colonist’s short name or "Someone" if none is available.
                            string witness = "Someone";
                            if (!map.mapPawns.FreeColonists.NullOrEmpty())
                            {
                                var pawn = map.mapPawns.FreeColonists.RandomElement();
                                witness = pawn.LabelShort;
                            }
                            Log.Message($"[RaidRestrictions] Using witness name: {witness}");

                            string letterLabel = "Unsettling Prelude";
                            var templates = new List<string>();

                            bool isDroneFaction = parms.faction.def == MD_DefOf.MD_DisassemblyDronesFaction;
                            Log.Message($"[RaidRestrictions] IsDroneFaction: {isDroneFaction}");

                            void AddGenericDropHints()
                            {
                                templates.Add($"{witness} noticed an odd silence overhead earlier—something felt poised to move when the light faded. It suggests a force waiting for cover of darkness.");
                                templates.Add($"{witness} observed shadows shifting strangely in the sky just before. It was brief, but enough to hint that an attack could come when visibility drops.");
                            }

                            void AddGenericWalkInHints()
                            {
                                templates.Add($"{witness} felt an unusual stillness at the boundary today—no wildlife or distant voices. It felt like unseen watchers waiting for the right moment to advance.");
                                templates.Add($"{witness} glimpsed a vague shape near the fence earlier—nothing concrete, but enough to suggest someone might circle in under darkness.");
                            }

                            void AddGenericFallbackHints()
                            {
                                templates.Add($"{witness} sensed a sudden calm in the area, as if something was preparing to move when conditions change.");
                                templates.Add($"{witness} felt a faint tremor underfoot not long ago, then nothing—an unsettling indication that forces may be poised to strike once the map is less exposed.");
                            }

                            void AddDroneDropHints()
                            {
                                templates.Add($"{witness} thought they heard a faint, warped giggle drifting from above—barely perceptible, but unsettling enough to suggest mechanical watchers preparing to drop in.");
                                templates.Add($"{witness} caught a soft hiss of metal brushing against metal earlier—too quiet to locate, yet chilling, as if something mechanical readies itself to descend.");
                            }

                            void AddDroneWalkInHints()
                            {
                                templates.Add($"{witness} heard a soft, irregular tapping from beyond the treeline—like metal feet testing the ground. It hints that something mechanical is circling, waiting to move in unseen.");
                                templates.Add($"{witness} caught a warped laugh drifting on the breeze—an odd, inhuman sound suggesting something clockwork circles nearby, poised to slip in under cover of darkness.");
                            }

                            void AddDroneFallbackHints()
                            {
                                templates.Add($"{witness} heard a faint, rhythmic clank earlier, followed by silence—almost as if something tested its strength before disappearing again.");
                                templates.Add($"{witness} detected a sudden, broken laugh not long ago—too jagged to be human, implying something cruel and mechanical lurks out there.");
                            }

                            if (isDroneFaction)
                            {
                                if (parms.raidArrivalMode != null)
                                {
                                    string mode = parms.raidArrivalMode.defName;
                                    Log.Message($"[RaidRestrictions] Drone arrival mode is: {mode}");
                                    if (mode == "EdgeDrop" || mode == "CenterDrop")
                                        AddDroneDropHints();
                                    else if (mode == "EdgeWalkIn")
                                        AddDroneWalkInHints();
                                    else
                                        AddDroneFallbackHints();
                                }
                                else
                                {
                                    AddDroneFallbackHints();
                                }
                            }
                            else
                            {
                                if (parms.raidArrivalMode != null)
                                {
                                    string mode = parms.raidArrivalMode.defName;
                                    Log.Message($"[RaidRestrictions] Non-drone raid arrival mode is: {mode}");
                                    if (mode == "EdgeDrop" || mode == "CenterDrop")
                                        AddGenericDropHints();
                                    else if (mode == "EdgeWalkIn")
                                        AddGenericWalkInHints();
                                    else
                                        AddGenericFallbackHints();
                                }
                                else
                                {
                                    AddGenericFallbackHints();
                                }
                            }

                            string letterText = templates.RandomElement();
                            Log.Message($"[RaidRestrictions] Sending warning letter with text: {letterText}");
                            Find.LetterStack.ReceiveLetter(
                                letterLabel,
                                letterText,
                                LetterDefOf.ThreatSmall,
                                new TargetInfo(map.Center, map, false)
                            );
                        }

                        __result = false;
                        Log.Message("[RaidRestrictions] Preventing immediate raid execution; raid is queued.");
                        return false;
                    }
                }
                else
                {
                    Log.Message("[RaidRestrictions] Target is not a Map; proceeding with standard execution.");
                }
            }
            else
            {
                Log.Message("[RaidRestrictions] No RaidRestrictions extension found or onlyNighttime not enabled; proceeding with standard execution.");
            }

            return true;
        }
    }
}














