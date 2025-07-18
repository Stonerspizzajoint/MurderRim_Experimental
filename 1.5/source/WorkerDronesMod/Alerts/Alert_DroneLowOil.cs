using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class Alert_DroneLowOil : Alert
    {
        private List<Pawn> lowOilDrones = new List<Pawn>();
        private StringBuilder sb = new StringBuilder();

        public Alert_DroneLowOil()
        {
            this.defaultLabel = "MD.AlertDroneLowOilLabel".Translate();
            this.defaultPriority = AlertPriority.Medium;
        }

        private List<Pawn> LowOilDrones
        {
            get
            {
                lowOilDrones.Clear();
                foreach (Pawn pawn in PawnsFinder.AllMaps_Spawned)
                {
                    // Only include colony pawns or prisoners
                    if ((pawn.IsColonist || pawn.IsPrisonerOfColony) && pawn.genes != null)
                    {
                        var gene = pawn.genes.GetFirstGeneOfType<Gene_BasicSolver>();
                        if (gene != null && gene.Oil < (gene.InitialResourceMax * 0.2f))
                        {
                            lowOilDrones.Add(pawn);
                        }
                    }
                }
                return lowOilDrones;
            }
        }

        public override TaggedString GetExplanation()
        {
            sb.Length = 0;
            foreach (Pawn pawn in this.LowOilDrones)
            {
                sb.AppendLine("  - " + pawn.NameShortColored.Resolve());
            }
            return "MD.AlertDroneLowOilDesc".Translate(sb.ToString());
        }

        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(this.LowOilDrones);
        }
    }
}

