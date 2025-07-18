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

            // Only allow extraction if the pawn has enough oil in Gene_BasicSolver
            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene == null || gene.Oil < OilPerExtraction)
                return false;

            return base.AvailableOnNow(thing, part);
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (!pawn.IsAndroid()) return;

            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene == null || gene.Oil < OilPerExtraction)
            {
                Messages.Message("Not enough oil to extract neutroamine".Translate(), pawn, MessageTypeDefOf.RejectInput);
                return;
            }

            // Consume oil
            gene.Oil -= OilPerExtraction;

            // Produce neutroamine Thing
            var neutro = ThingMaker.MakeThing(MD_DefOf.Neutroamine);
            neutro.stackCount = 1;
            GenPlace.TryPlaceThing(neutro, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);

            base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);
        }
    }
}
