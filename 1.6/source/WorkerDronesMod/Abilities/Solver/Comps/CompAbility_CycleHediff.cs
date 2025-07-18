using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class CompAbility_CycleHediff : CompAbilityEffect
    {
        private Dictionary<Pawn, int> pawnIndices = new Dictionary<Pawn, int>();

        public CompProperties_CycleHediff Props => props as CompProperties_CycleHediff;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || Props?.hediffs == null) return;

            List<HediffDef> effectiveList = GetEffectiveHediffList(pawn);
            if (effectiveList.Count == 0) return;

            // Select next hediff based on pawn type
            HediffDef nextHediff;
            if (pawn.IsColonist)
            {
                // Colonists cycle through list
                if (!pawnIndices.TryGetValue(pawn, out int currentIndex))
                {
                    currentIndex = 0;
                    pawnIndices[pawn] = currentIndex;
                }
                nextHediff = effectiveList[currentIndex];
                pawnIndices[pawn] = (currentIndex + 1) % effectiveList.Count;
            }
            else
            {
                // NPCs choose randomly
                nextHediff = effectiveList.RandomElement();
            }

            DropAllEquipment(pawn);
            RemoveCurrentHediffs(pawn, effectiveList);
            ApplyHediffToHands(pawn, nextHediff);

            if (pawn.IsColonist)
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, nextHediff.label, 4f);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || Props?.hediffs == null) return false;

            if (!pawn.IsColonist)
            {
                return Props.npcMeleeHediffs.Count + Props.npcRangedHediffs.Count > 0;
            }

            return base.Valid(target, throwMessages) && GetEffectiveHediffList(pawn).Count > 0;
        }

        private List<HediffDef> GetEffectiveHediffList(Pawn pawn)
        {
            if (!pawn.IsColonist)
            {
                // NPCs combine melee and ranged lists
                return new List<HediffDef>()
                    .Union(Props.npcMeleeHediffs)
                    .Union(Props.npcRangedHediffs)
                    .ToList();
            }

            // Colonist logic with gene-locked hediffs
            List<HediffDef> list = new List<HediffDef>(Props.hediffs);

            if (pawn.genes != null && Props.geneLockedHediffs != null)
            {
                foreach (GeneLockedHediff entry in Props.geneLockedHediffs)
                {
                    if (string.IsNullOrEmpty(entry.hediffDefName)) continue;
                    if (string.IsNullOrEmpty(entry.requiredGeneDefName)) continue;

                    GeneDef requiredGene = DefDatabase<GeneDef>.GetNamed(entry.requiredGeneDefName, false);
                    HediffDef geneHediff = DefDatabase<HediffDef>.GetNamed(entry.hediffDefName, false);

                    if (requiredGene == null || geneHediff == null) continue;

                    if (pawn.genes.HasActiveGene(requiredGene))
                    {
                        if (!list.Contains(geneHediff)) list.Add(geneHediff);
                    }
                    else
                    {
                        list.Remove(geneHediff);
                    }
                }
            }
            return list;
        }

        private void DropAllEquipment(Pawn pawn)
        {
            if (pawn.equipment == null) return;

            List<ThingWithComps> equipment = new List<ThingWithComps>(pawn.equipment.AllEquipmentListForReading);
            foreach (ThingWithComps eq in equipment)
            {
                pawn.equipment.TryDropEquipment(eq, out _, pawn.Position);
            }
        }

        private void RemoveCurrentHediffs(Pawn pawn, List<HediffDef> effectiveList)
        {
            List<Hediff> toRemove = pawn.health.hediffSet.hediffs
                .Where(h => effectiveList.Contains(h.def))
                .ToList();

            foreach (Hediff h in toRemove)
            {
                pawn.health.RemoveHediff(h);
            }
        }

        private void ApplyHediffToHands(Pawn pawn, HediffDef hediffDef)
        {
            // Get available hands
            var hands = pawn.health.hediffSet.GetNotMissingParts()
                .Where(p => p.def == MD_DefOf.Hand ||
                          p.def.defName == "HandLeft" ||
                          p.def.defName == "HandRight")
                .ToList();

            if (hands.Count == 0) return;

            if (Props.onlyOneHand)
            {
                // Manual GetValueOrDefault implementation
                int handIndex = pawnIndices.TryGetValue(pawn, out int idx) ? idx : 0;
                BodyPartRecord targetHand = pawn.IsColonist
                    ? hands[handIndex % hands.Count]
                    : hands.RandomElement();

                pawn.health.AddHediff(hediffDef, targetHand);
            }
            else
            {
                foreach (BodyPartRecord hand in hands)
                {
                    pawn.health.AddHediff(hediffDef, hand);
                }
            }
        }
    }
}












