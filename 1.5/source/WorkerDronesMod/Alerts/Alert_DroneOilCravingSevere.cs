using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class Alert_DroneOilCravingSevere : Alert
    {
        private List<Pawn> severeCravingDrones = new List<Pawn>();
        private StringBuilder sb = new StringBuilder();

        // You can adjust this threshold as needed
        private const float SeverityThreshold = 0.5f;

        public Alert_DroneOilCravingSevere()
        {
            this.defaultLabel = "MD.AlertDroneOilCravingSevereLabel".Translate();
            this.defaultPriority = AlertPriority.High;
        }

        private List<Pawn> SevereCravingDrones
        {
            get
            {
                severeCravingDrones.Clear();
                foreach (Pawn pawn in PawnsFinder.AllMaps_Spawned)
                {
                    if ((pawn.IsColonist || pawn.IsPrisonerOfColony) && pawn.health != null)
                    {
                        var craving = pawn.health.hediffSet.GetFirstHediffOfDef(MD_DefOf.MD_OilLoss) as Hediff_NeutroOilCraving;
                        if (craving != null && craving.Severity >= SeverityThreshold)
                        {
                            severeCravingDrones.Add(pawn);
                        }
                    }
                }
                return severeCravingDrones;
            }
        }

        public override TaggedString GetExplanation()
        {
            sb.Length = 0;
            foreach (Pawn pawn in this.SevereCravingDrones)
            {
                sb.AppendLine("  - " + pawn.NameShortColored.Resolve());
            }
            // Add the description and solution lines
            return "MD.AlertDroneOilCravingSevereDesc".Translate(sb.ToString()) + "\n\n" +
                   "MD.AlertDroneOilCravingSevereDescription".Translate() + "\n\n" +
                   "MD.AlertDroneOilCravingSevereSolution".Translate();
        }


        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(this.SevereCravingDrones);
        }
    }
}

