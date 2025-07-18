using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WorkerDronesMod
{
    // ---------------------------------------------------------
    // Holder class for one pending raid on a specific map.
    // ---------------------------------------------------------
    public class PendingRaid : IExposable
    {
        public IncidentDef incidentDef;
        public IncidentParms parms;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref incidentDef, "incidentDef");
            Scribe_Deep.Look(ref parms, "parms");
        }
    }

    public class PendingRaidMapComponent : MapComponent
    {
        public List<PendingRaid> Pending = new List<PendingRaid>();

        public PendingRaidMapComponent(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % 250 != 0)
                return;

            var pendingComp = map.GetComponent<PendingRaidMapComponent>();
            if (pendingComp == null || pendingComp.Pending.Count == 0)
                return;

            var copy = new List<PendingRaid>(pendingComp.Pending);
            foreach (var pr in copy)
            {
                if (pr.parms.target is Map targetMap && targetMap == this.map)
                {
                    var restrictions = pr.parms.faction?.def.GetModExtension<RaidRestrictions>();
                    var geneExt = pr.parms.faction?.def.GetModExtension<SolverGeneExtension>();
                    if (SolarUtil.IsMapSafeForSolvers(this.map, restrictions, geneExt))
                    {
                        if (pr.incidentDef != null)
                        {
                            pr.incidentDef.Worker.TryExecute(pr.parms);
                        }
                        pendingComp.Pending.Remove(pr);
                    }
                }
                else
                {
                    pendingComp.Pending.Remove(pr);
                }
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref Pending, "pendingRaids", LookMode.Deep);
        }
    }
}
