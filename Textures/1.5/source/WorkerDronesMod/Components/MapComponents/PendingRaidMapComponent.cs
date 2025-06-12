using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WorkerDronesMod
{
    // ---------------------------------------------------------
    // Holder class for one pending raid on a specific map.
    // ---------------------------------------------------------
    public class PendingRaid
    {
        // The IncidentWorker_RaidEnemy instance that would have fired.
        public IncidentWorker_RaidEnemy worker;

        // The exact IncidentParms that storyteller created.
        public IncidentParms parms;
    }

    // ---------------------------------------------------------
    //    stores all pending raids for this map.
    //    We override MapComponentTick() to check sun-safety & fire them.
    // ---------------------------------------------------------
    public class PendingRaidMapComponent : MapComponent
    {
        // All raids that were intercepted on this map and are waiting for safety.
        public List<PendingRaid> Pending = new List<PendingRaid>();

        public PendingRaidMapComponent(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            // Only do the check every 250 ticks to avoid over-calling.
            if (Find.TickManager.TicksGame % 250 != 0)
                return;

            if (Pending.Count == 0)
                return;

            // Copy the list so we can remove inside the loop.
            var copy = new List<PendingRaid>(Pending);
            foreach (var pr in copy)
            {
                // pr.parms.target should be this map (if it isn’t, drop it).
                if (pr.parms.target is Map targetMap && targetMap == this.map)
                {
                    // If the map is now sun-safe, fire the raid immediately:
                    if (SolverGeneUtility.IsMapSafeFromSun(this.map))
                    {
                        pr.worker.TryExecute(pr.parms);
                        Pending.Remove(pr);
                    }
                }
                else
                {
                    // If the target isn’t exactly this.map, just drop it.
                    Pending.Remove(pr);
                }
            }
        }

        // Optional: if you ever save/load, you might want to expose data.
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref Pending, "pendingRaids", LookMode.Deep, LookMode.Deep);
            // Note: If PendingRaid isn’t automatically savable, you may need custom Scribe. 
            // For simplicity, this assumes IncidentWorker_Raid and IncidentParms are Scribe-able.
        }
    }
}
