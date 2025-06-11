using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using VREAndroids;  // for IsAndroid()

namespace WorkerDronesMod
{
    public class StockGenerator_AndroidSlavesOnly : StockGenerator
    {
        // If true, obey population intent like the base StockGenerator_Slaves does
        private bool respectPopulationIntent = true;

        public override IEnumerable<Thing> GenerateThings(int forTile, Faction faction = null)
        {
            // 1) population-intent check
            if (respectPopulationIntent && Rand.Value > StorytellerUtilityPopulation.PopulationIntent)
                yield break;

            // 2) ideological check for slavery (if you still want that)
            if (faction != null && faction.ideos != null
                && !faction.ideos.AllIdeos.All(i => i.IdeoApprovesOfSlavery()))
            {
                yield break;
            }

            int targetCount = this.countRange.RandomInRange;
            int spawned = 0;
            int safety = targetCount * 5;  // avoid infinite loops if no androids exist

            while (spawned < targetCount && safety-- > 0)
            {
                // pick a random visible, non-player, humanlike, non-temporary faction
                var possibleFactions = Find.FactionManager.AllFactionsVisible
                    .Where(f => f != Faction.OfPlayer && f.def.humanlikeFaction && !f.temporary);
                if (!possibleFactions.TryRandomElement(out Faction sourceFac))
                    yield break;

                // build a simple request: slaveKindDef or default to PawnKindDefOf.Slave
                var kind = PawnKindDefOf.Slave;
                var req = new PawnGenerationRequest(
                    kind,
                    sourceFac,
                    PawnGenerationContext.NonPlayer,
                    forTile
                );

                // generate the pawn
                Pawn pawn = PawnGenerator.GeneratePawn(req);

                // only accept if it's actually an android
                if (pawn.IsAndroid())
                {
                    spawned++;
                    yield return pawn;
                }
                else
                {
                    // otherwise destroy it and try again
                    pawn.Destroy();
                }
            }
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            // we only generate humanlike pawns; the android filter
            // happens at runtime in GenerateThings via pawn.IsAndroid()
            return thingDef.category == ThingCategory.Pawn
                && thingDef.race.Humanlike
                && thingDef.tradeability > Tradeability.None;
        }
    }
}




