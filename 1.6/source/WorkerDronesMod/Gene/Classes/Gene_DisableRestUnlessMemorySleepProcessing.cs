using System;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class Gene_DisableRestUnlessMemorySleepProcessing : Gene
    {
        private int updateCounter = 0;
        // Regeneration percentage per update tick when sleeping (0.005 = 0.5% of MaxLevel per second)
        private const float SleepRegenPercentage = 0.005f;

        public override void Tick()
        {
            base.Tick();
            updateCounter++;
            // Update every 60 ticks (~1 second)
            if (updateCounter < 60)
                return;
            updateCounter = 0;

            if (pawn != null && pawn.needs != null)
            {
                // Process rest need logic
                Need_Rest restNeed = pawn.needs.TryGetNeed<Need_Rest>();
                if (restNeed != null)
                {
                    // Only check for the main gene, not the VREA_ variant
                    bool hasRequiredGene = pawn.genes != null && pawn.genes.HasActiveGene(MD_DefOf.MD_MemorySleepProcessing);

                    // If the gene is missing, keep the rest need full so it never drains.
                    if (!hasRequiredGene)
                    {
                        restNeed.CurLevel = restNeed.MaxLevel;
                    }
                }

                // --- Reactor Power Need Logic ---
                var reactorNeed = pawn.needs.TryGetNeed<VREAndroids.Need_ReactorPower>();
                if (reactorNeed != null)
                {
                    bool isSleeping = pawn.CurJob != null &&
                        pawn.CurJob.def.defName.IndexOf("Sleep", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (isSleeping)
                    {
                        float regenAmount = reactorNeed.MaxLevel * SleepRegenPercentage;
                        reactorNeed.CurLevel = Math.Min(reactorNeed.CurLevel + regenAmount, reactorNeed.MaxLevel);
                    }
                }
            }
        }
    }
}
