using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    /// <summary>
    /// This hediff represents a stasis state applied to an Android pawn after custom resurrection.
    /// While in stasis, the pawn is immobilized and considered "deadlike" until its critical components 
    /// regenerate. While this hediff is active, the pawn’s Comp_DisassemblyDrone flag 
    /// (isInResurrectionStasis) is set to true. Once the pawn is fully healed (as determined by a healing 
    /// heuristic) and remains so for a set delay, the hediff then removes itself. After removal, the pawn 
    /// enters a confused wandering mental break.
    /// </summary>
    public class Hediff_ResurrectionStasis : HediffWithComps
    {
        // Label shown in the pawn's health tab.
        public override string LabelBase => "Resurrection Stasis";

        // Tooltip describing the stasis state.
        public override string TipStringExtra =>
            "This Android is in stasis while its critical components regenerate. " +
            "It is immobilized until fully restored.";

        // Check our healing status every 60 ticks (roughly once per second).
        private const int TickCheckInterval = 60;
        private int lastTickChecked = 0;

        // Delay parameters: once fully healed, wait this many ticks before removing the hediff.
        private const int FullyHealedDelayTicks = 600;  // About 5 seconds.
        private int ticksSinceFullyHealed = 0;

        // Boolean flag for tracking mental break (ConfusedWander) state.
        private bool isConfused = false;

        /// <summary>
        /// Sets the confusion state flag.
        /// </summary>
        public void SetConfusionState(bool state)
        {
            isConfused = state;
            Log.Message($"[Hediff_ResurrectionStasis] {pawn.LabelShort} confusion state set to {state}.");
        }

        public override void Tick()
        {
            base.Tick();
            if (pawn == null)
                return;

            // Simulate PostAdd behavior: on the first tick, set the stasis flag in the pawn's DisassemblyDrone comp.
            var comp = pawn.TryGetComp<Comp_DisassemblyDrone>();
            if (comp != null && !comp.isInResurrectionStasis)
            {
                comp.isInResurrectionStasis = true;
                Log.Message($"[Hediff_ResurrectionStasis] {pawn.LabelShort}: isInResurrectionStasis flag set.");
            }

            // Only check the full healing condition every TickCheckInterval ticks.
            if (Find.TickManager.TicksGame - lastTickChecked < TickCheckInterval)
                return;
            lastTickChecked = Find.TickManager.TicksGame;

            bool fullyHealed = false;
            // If the pawn has Gene_NeutroamineOil, use its IsFullyHealed() method.
            var oilGene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
            if (oilGene != null)
            {
                fullyHealed = oilGene.IsFullyHealed();
            }
            else
            {
                // Fallback: use a simple heuristic based on impairing hediffs.
                fullyHealed = GetApparentHealthPercent(pawn) >= 0.95f;
            }

            if (fullyHealed)
            {
                // Increment the counter while the pawn remains fully healed.
                ticksSinceFullyHealed += TickCheckInterval;
                Log.Message($"[Hediff_ResurrectionStasis] {pawn.LabelShort} fully healed for {ticksSinceFullyHealed} ticks.");
            }
            else
            {
                // Reset the counter if the pawn is not fully healed.
                ticksSinceFullyHealed = 0;
            }

            // Once the pawn has remained fully healed for the required delay, adjust heat and remove the hediff.
            if (ticksSinceFullyHealed >= FullyHealedDelayTicks)
            {
                if (comp != null)
                {
                    comp.isInResurrectionStasis = false;
                }

                // Ensure the pawn's heat is at least 70% after stasis is removed.
                var heatGene = pawn.genes?.GetFirstGeneOfType<Gene_HeatBuildup>();
                if (heatGene != null)
                {
                    float targetHeat = 0.70f * heatGene.InitialResourceMax;
                    if (heatGene.Value < targetHeat)
                    {
                        float neededHeat = targetHeat - heatGene.Value;
                        heatGene.IncreaseHeat(neededHeat);
                        Log.Message($"[Hediff_ResurrectionStasis] Increased heat by {neededHeat} to reach 70% for {pawn.LabelShortCap}.");
                    }
                }
                else
                {
                    Log.Warning($"[Hediff_ResurrectionStasis] {pawn.LabelShortCap} has no Gene_HeatBuildup.");
                }

                Log.Message($"[Hediff_ResurrectionStasis] {pawn.LabelShort} is fully restored for the delay period. Removing stasis.");
                pawn.health.RemoveHediff(this);

                // Trigger the confused wandering mental break after stasis removal.
                TriggerConfusedMentalBreak();
            }
        }

        /// <summary>
        /// A fallback helper method to approximate the pawn's overall health as a fraction (0.0 to 1.0).
        /// Each "bad" hediff (excluding this stasis hediff itself) is assumed to subtract 5% from full health.
        /// Adjust this heuristic as needed.
        /// </summary>
        private float GetApparentHealthPercent(Pawn pawn)
        {
            if (pawn == null || pawn.health == null)
                return 1f;
            int impairingCount = pawn.health.hediffSet.hediffs.Count(
                h => h.def.isBad && h.def != MD_DefOf.MD_ResurrectionStasis);
            float percent = 1f - (impairingCount * 0.05f);
            return Mathf.Clamp01(percent);
        }

        /// <summary>
        /// Triggers the custom confused wandering mental break after stasis is removed.
        /// Here, we also assign a temporary wait job to prevent any exit-map jobs from being assigned immediately.
        /// </summary>
        private void TriggerConfusedMentalBreak()
        {
            MentalStateDef confusedWanderDef = MD_DefOf.MD_ConfusedWander;
            if (confusedWanderDef == null)
            {
                Log.Error($"[Hediff_ResurrectionStasis] MentalStateDef 'MD_ConfusedWander' not found. Cannot trigger mental break for {pawn.LabelShort}.");
                return;
            }

            if (!pawn.InMentalState)
            {
                // Set confusion flag and start the mental state.
                SetConfusionState(true);
                pawn.mindState.mentalStateHandler.TryStartMentalState(confusedWanderDef, "Recovered from stasis", false);
                Log.Message($"[Hediff_ResurrectionStasis] {pawn.LabelShort} has entered confused wandering mental break.");

                // Prevent immediate exit-map behavior by assigning a temporary wait job.
                Job waitJob = new Job(JobDefOf.Wait, 600); // 600 ticks ~ 10 seconds
                pawn.jobs.StartJob(waitJob, JobCondition.InterruptForced);
                Log.Message($"[Hediff_ResurrectionStasis] {pawn.LabelShort} assigned a temporary wait job to delay exit-map actions.");
            }
        }
    }
}