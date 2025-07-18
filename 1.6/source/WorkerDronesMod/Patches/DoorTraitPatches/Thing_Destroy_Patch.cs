using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    // Patch the base Thing.Destroy(DestroyMode) method.
    [HarmonyPatch(typeof(Thing), "Destroy", new Type[] { typeof(DestroyMode) })]
    public static class Thing_Destroy_Patch
    {
        // Prefix: store the door's map (using door.Map or door.MapHeld) before it's destroyed.
        public static void Prefix(Thing __instance, out Map __state)
        {
            __state = null;
            if (__instance is Building_Door door)
            {
                __state = door.Map ?? door.MapHeld;
            }
        }

        // Postfix: use the stored map to add the memory to colonists.
        public static void Postfix(Thing __instance, DestroyMode mode, Map __state)
        {
            if (__instance is Building_Door door)
            {
                Log.Message($"[WorkerDronesMod] Door destroyed: {door} with mode: {mode}");
                // Only process if the door is owned by the player and it wasn't deconstructed.
                if (door.Faction == Faction.OfPlayer && mode != DestroyMode.Deconstruct)
                {
                    if (__state == null)
                    {
                        Log.Warning("[WorkerDronesMod] Stored map is null!");
                        return;
                    }
                    // Loop through free colonists on the stored map.
                    foreach (Pawn pawn in __state.mapPawns.FreeColonists)
                    {
                        if (pawn.story != null && pawn.story.traits != null &&
                            pawn.story.traits.HasTrait(TraitDef.Named("MD_DoorEnthusiast")))
                        {
                            Log.Message($"[WorkerDronesMod] Adding door broken memory for pawn: {pawn.Name}");
                            ThoughtDef doorBrokenThought = ThoughtDef.Named("DoorBrokenSadness");
                            pawn.needs.mood.thoughts.memories.TryGainMemory(doorBrokenThought, null);
                        }
                    }
                }
            }
        }
    }
}




