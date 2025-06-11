using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    // PATCH 2: When a frame completes construction, if it's for a door, give the builder the memory.
    [HarmonyPatch(typeof(Frame), "CompleteConstruction")]
    public static class Frame_CompleteConstruction_Patch
    {
        // Prefix: Capture the map reference before it might be cleared.
        public static void Prefix(Frame __instance, out Map __state)
        {
            __state = __instance.Map ?? __instance.MapHeld;
        }

        // Postfix: Process the completed frame.
        public static void Postfix(Frame __instance, Map __state)
        {
            if (__state == null)
            {
                Log.Warning("[WorkerDronesMod] Built door has no map!");
                return;
            }

            // Cast entityDefToBuild to ThingDef to access its thingClass.
            ThingDef thingDefToBuild = __instance.def.entityDefToBuild as ThingDef;
            if (thingDefToBuild == null || thingDefToBuild.thingClass != typeof(Building_Door))
                return; // Not a door—do nothing.

            // Retrieve the builder from our tracker.
            if (!DoorBuilderTracker.BuilderByFrame.TryGetValue(__instance, out Pawn builder))
            {
                Log.Warning("[WorkerDronesMod] No builder found for door frame!");
                return;
            }
            // Remove the entry since we've processed it.
            DoorBuilderTracker.BuilderByFrame.Remove(__instance);

            // Only proceed if the builder has the MD_DoorEnthusiast trait.
            if (builder.story == null || builder.story.traits == null ||
                !builder.story.traits.HasTrait(TraitDef.Named("MD_DoorEnthusiast")))
                return;

            // Enforce a cooldown (e.g., 60,000 ticks ≈ 1 in-game day).
            int currentTick = Find.TickManager.TicksGame;
            int cooldownTicks = 60000;
            if (DoorBuiltTracker.LastDoorBuiltTick.TryGetValue(builder, out int lastTick))
            {
                if (currentTick - lastTick < cooldownTicks)
                {
                    Log.Message($"[WorkerDronesMod] Cooldown active for pawn: {builder.Name}, skipping door-built memory.");
                    return;
                }
            }

            // Add the door-built memory thought.
            ThoughtDef doorBuiltThought = ThoughtDef.Named("DoorCreatedJoy");
            builder.needs.mood.thoughts.memories.TryGainMemory(doorBuiltThought, null);
            DoorBuiltTracker.LastDoorBuiltTick[builder] = currentTick;
            Log.Message($"[WorkerDronesMod] Added door-built memory for builder: {builder.Name}");
        }
    }
}

