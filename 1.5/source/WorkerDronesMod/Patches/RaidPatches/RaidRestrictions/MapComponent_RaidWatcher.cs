using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace WorkerDronesMod.Patches
{
    public class MapComponent_RaidWatcher : MapComponent
    {
        // Keeps track of raiders for which we’ve shown the leaving message, 
        // so we prevent spamming multiple letters for the same raid.
        private HashSet<Lord> notifiedRaiders = new HashSet<Lord>();

        public MapComponent_RaidWatcher(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            // Create a separate list of Lords to avoid modifying the collection while iterating.
            List<Lord> lords = new List<Lord>(map.lordManager.lords);
            for (int i = 0; i < lords.Count; i++)
            {
                Lord lord = lords[i];
                // Only consider raids (using accessible types for Assault raids).
                if (lord.LordJob is LordJob_AssaultColony || lord.LordJob is LordJob_AssaultThings)
                {
                    Faction faction = lord.faction;
                    // Retrieve the mod extension from the faction's def.
                    RaidRestrictions restrictions = faction.def.GetModExtension<RaidRestrictions>();
                    if (restrictions != null)
                    {
                        float skyGlow = map.skyManager.CurSkyGlow;
                        // If the current sky glow exceeds the maximum allowed, end the raid.
                        if (skyGlow > restrictions.maxSkyGlow)
                        {
                            // Only send a notification once per raid.
                            if (!notifiedRaiders.Contains(lord))
                            {
                                Find.LetterStack.ReceiveLetter(
                                    "Raid Retreating", // Title
                                    $"The raid from {faction.Name} is retreating due to increasing daylight.", // Description
                                    LetterDefOf.PositiveEvent, // Letter type, adjust as desired
                                    new LookTargets(lord.ownedPawns), // Targets so the player can see the raiders
                                    faction
                                );
                                notifiedRaiders.Add(lord);
                            }

                            // For each pawn in the raid, cancel current jobs and assign an exit job.
                            foreach (Pawn pawn in lord.ownedPawns)
                            {
                                if (pawn.mindState != null)
                                {
                                    pawn.mindState.duty = new PawnDuty(DutyDefOf.ExitMapBest);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

