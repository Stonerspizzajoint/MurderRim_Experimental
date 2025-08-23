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

            if (Find.TickManager.TicksGame % 250 != 0)
                return;

            List<Lord> lords = new List<Lord>(map.lordManager.lords);
            for (int i = 0; i < lords.Count; i++)
            {
                Lord lord = lords[i];
                if (lord.LordJob is LordJob_AssaultColony || lord.LordJob is LordJob_AssaultThings)
                {
                    Faction faction = lord.faction;
                    RaidRestrictions restrictions = faction.def.GetModExtension<RaidRestrictions>();
                    if (restrictions != null && restrictions.onlyNighttime)
                    {
                        if (!SolarUtil.IsMapSafeForSolvers(map, restrictions))
                        {
                            if (!notifiedRaiders.Contains(lord))
                            {
                                Find.LetterStack.ReceiveLetter(
                                    "Raid Retreating",
                                    $"The raid from {faction.Name} is retreating due to increasing daylight.",
                                    LetterDefOf.PositiveEvent,
                                    new LookTargets(lord.ownedPawns),
                                    faction
                                );
                                notifiedRaiders.Add(lord);
                            }

                            foreach (Pawn pawn in lord.ownedPawns)
                            {
                                if (pawn.mindState != null)
                                {
                                    pawn.mindState.duty = new PawnDuty(MD_DefOf.MD_ExitMapPanicFly);
                                    pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced, true);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

