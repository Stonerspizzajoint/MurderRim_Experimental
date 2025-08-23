using HarmonyLib;
using Verse;
using RimWorld;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Linq;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn_FlightTracker), "FlightTick")]
    public static class Patch_FlightTick_FlightControl
    {
        private static readonly FieldInfo pawnField = typeof(Pawn_FlightTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo flightStateField = typeof(Pawn_FlightTracker).GetField("flightState", BindingFlags.Instance | BindingFlags.NonPublic);

        // Track effecters per pawn
        private static readonly ConditionalWeakTable<Pawn, Effecter> flyingEffecters = new ConditionalWeakTable<Pawn, Effecter>();
        // Track previous flight state per pawn
        private static readonly ConditionalWeakTable<Pawn, Holder> prevFlightStates = new ConditionalWeakTable<Pawn, Holder>();

        private class Holder { public int state; }

        static void Postfix(object __instance)
        {
            Pawn pawn = pawnField?.GetValue(__instance) as Pawn;
            if (pawn?.genes == null) return;

            var flightState = (int)flightStateField.GetValue(__instance);

            // Get previous state (default to -1 if not present)
            int prevState = -1;
            if (!prevFlightStates.TryGetValue(pawn, out var holder))
            {
                holder = new Holder();
                prevFlightStates.Add(pawn, holder);
            }
            else
            {
                prevState = holder.state;
            }
            holder.state = flightState;

            foreach (var gene in pawn.genes.GenesListForReading)
            {
                var ext = gene.def.GetModExtension<WingsFlightControl>();
                if (ext == null) continue;

                // Default: allow flight hediffs for non-controllable pawns
                bool allowFlightHediffs = true;

                // Only check ability toggle for player-controllable pawns
                if (pawn.Faction == Faction.OfPlayer && pawn.IsColonist)
                {
                    var dismissAbility = pawn.abilities?.abilities
                        .FirstOrDefault(ab => ab.def == MD_DefOf.MD_Dismisswings);

                    if (dismissAbility != null)
                    {
                        var comp = dismissAbility.comps?.OfType<Comp_ToggleHediffEffect>().FirstOrDefault();
                        if (comp != null)
                        {
                            allowFlightHediffs = comp.toggledOn;
                        }
                    }
                }

                if (!allowFlightHediffs)
                {
                    // Remove both flying and landed hediffs if present
                    if (ext.FlyingHediff != null)
                    {
                        var flying = pawn.health.hediffSet.GetFirstHediffOfDef(ext.FlyingHediff);
                        if (flying != null)
                            pawn.health.RemoveHediff(flying);
                    }
                    if (ext.LandedHediff != null)
                    {
                        var landed = pawn.health.hediffSet.GetFirstHediffOfDef(ext.LandedHediff);
                        if (landed != null)
                            pawn.health.RemoveHediff(landed);
                    }
                    // Cleanup flying effecter if present
                    if (flyingEffecters.TryGetValue(pawn, out var effecter))
                    {
                        effecter.Cleanup();
                        flyingEffecters.Remove(pawn);
                    }
                    continue; // Skip normal flight hediff/effecter logic
                }

                // --- LIFTOFF EFFECTER ---
                // If just transitioned to TakingOff (2)
                if (flightState == 2 && prevState != 2 && ext.LiftOffEffecter != null)
                {
                    Effecter lift = ext.LiftOffEffecter.Spawn();
                    var target = new TargetInfo(pawn.Position, pawn.Map);
                    lift.Trigger(target, target);
                    lift.Cleanup();
                }

                // When taking off or flying, ensure FlyingHediff is present and LandedHediff is removed
                if (flightState == 1 || flightState == 2) // FlightState.Flying or FlightState.TakingOff
                {
                    if (ext.LandedHediff != null)
                    {
                        var landed = pawn.health.hediffSet.GetFirstHediffOfDef(ext.LandedHediff);
                        if (landed != null)
                            pawn.health.RemoveHediff(landed);
                    }
                    if (ext.FlyingHediff != null && !pawn.health.hediffSet.HasHediff(ext.FlyingHediff))
                        pawn.health.AddHediff(ext.FlyingHediff);

                    // Handle flying effecter
                    if (ext.FlyingEffecter != null)
                    {
                        if (!flyingEffecters.TryGetValue(pawn, out var effecter))
                        {
                            effecter = ext.FlyingEffecter.Spawn();
                            flyingEffecters.Add(pawn, effecter);
                        }
                        effecter.EffectTick(pawn, null);
                    }
                }
                // When grounded, ensure LandedHediff is present and FlyingHediff is removed
                else if (flightState == 0) // FlightState.Grounded
                {
                    if (ext.FlyingHediff != null)
                    {
                        var flying = pawn.health.hediffSet.GetFirstHediffOfDef(ext.FlyingHediff);
                        if (flying != null)
                            pawn.health.RemoveHediff(flying);
                    }
                    if (ext.LandedHediff != null && !pawn.health.hediffSet.HasHediff(ext.LandedHediff))
                        pawn.health.AddHediff(ext.LandedHediff);

                    // Cleanup flying effecter
                    if (flyingEffecters.TryGetValue(pawn, out var effecter))
                    {
                        effecter.Cleanup();
                        flyingEffecters.Remove(pawn);
                    }
                }
            }
        }
    }
}
