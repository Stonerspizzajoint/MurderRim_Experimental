using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using VREAndroids;

namespace WorkerDronesMod
{
    public class Recipe_ExtractNeutroamine : Recipe_Surgery
    {
        private const float OilPerExtraction = 10f;
        private const float NeutroLossPerExtraction = 0.01f; // 1 unit of neutroamine == 0.01 Severity

        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            Pawn pawn = thing as Pawn;
            if (pawn == null || !pawn.IsAndroid())
                return false;

            // Do not allow extraction if severity is at or above 1.0 and the pawn does not have a solver
            if (!SolverGeneUtility.HasSolver(pawn))
            {
                var existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(VREA_DefOf.VREA_NeutroLoss);
                if (existingHediff != null && existingHediff.Severity >= 1.0f)
                    return false;
            }

            return base.AvailableOnNow(thing, part);
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (!pawn.IsAndroid()) return;

            bool usedOil = false;

            // Solver-based extraction if HasSolver
            if (SolverGeneUtility.HasSolver(pawn))
            {
                var gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
                if (gene != null && gene.Value >= OilPerExtraction)
                {
                    gene.Value -= OilPerExtraction;
                    usedOil = true;
                }
                else
                {
                    Messages.Message("Not enough oil to extract neutroamine".Translate(), pawn, MessageTypeDefOf.RejectInput);
                    return;
                }
            }
            else
            {
                var existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(VREA_DefOf.VREA_NeutroLoss);
                if (existingHediff != null && existingHediff.Severity >= 1.0f)
                {
                    Messages.Message("Too much neutroamine loss to extract more".Translate(), pawn, MessageTypeDefOf.RejectInput);
                    return;
                }

                // Apply or increase NeutroLoss hediff if not HasSolver
                if (existingHediff != null)
                {
                    existingHediff.Severity += NeutroLossPerExtraction;
                }
                else
                {
                    var hediff = HediffMaker.MakeHediff(VREA_DefOf.VREA_NeutroLoss, pawn);
                    hediff.Severity = NeutroLossPerExtraction;
                    pawn.health.AddHediff(hediff);
                }
            }

            // Produce neutroamine Thing
            var neutro = ThingMaker.MakeThing(MD_DefOf.Neutroamine);
            neutro.stackCount = 1;
            GenPlace.TryPlaceThing(neutro, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);

            if (!usedOil)
            {
                base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);
            }
        }
    }
}
