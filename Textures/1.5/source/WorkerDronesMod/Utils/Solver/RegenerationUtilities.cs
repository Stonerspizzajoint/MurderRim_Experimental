using System.Collections.Generic;
using CombatExtended; // Only needed if CE is installed
using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    public static class RegenerationUtilities
    {

        /// <summary>
        /// Gradually heals wounds on a pawn by reducing the severity of each injury,
        /// but only processes the injuries provided by the filtered list.
        /// This overload is intended for cases where only injuries that are ready (per your timers)
        /// should be healed, while others remain delayed.
        /// </summary>
        /// <param name="pawn">The pawn to heal.</param>
        /// <param name="regenAmount">The base amount to reduce the severity of each injury.</param>
        /// <param name="injuriesToHeal">A filtered list of injuries that are ready to be healed.</param>
        public static void RegenerateWounds(Pawn pawn, float regenAmount, List<Hediff_Injury> injuriesToHeal)
        {
            if (pawn == null || pawn.Dead)
                return;

            if (injuriesToHeal == null || injuriesToHeal.Count == 0)
                return;

            // Obtain the mod extension from the pawn's Neutroamine Oil gene.
            SolverRegenerationModExtension modExt = null;
            var gene = pawn.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
            if (gene != null)
                modExt = gene.def.GetModExtension<SolverRegenerationModExtension>();

            float regenSpeedMultiplier = modExt != null ? modExt.regenSpeedMultiplier : 10f;
            float minHealingFactor = modExt != null ? modExt.minHealingFactor : 0.2f;

            int woundCount = injuriesToHeal.Count;
            float healingFactor = 1f / woundCount;
            healingFactor = Mathf.Max(healingFactor, minHealingFactor);

            foreach (Hediff_Injury injury in injuriesToHeal)
            {
                float adjustedRegen = regenAmount * regenSpeedMultiplier * healingFactor;
                injury.Severity -= adjustedRegen;
                if (injury.Severity <= 0f)
                    pawn.health.RemoveHediff(injury);
            }
        }

        /// <summary>
        /// Regenerates armor durability using custom logic.
        /// This method first checks if Combat Extended is active before doing anything.
        /// </summary>
        /// <param name="armor">The armor (ThingWithComps) to regenerate.</param>
        public static void RegenerateArmorDurability(ThingWithComps armor)
        {
            if (!ModLister.HasActiveModWithName("Combat Extended"))
            {
                return;
            }

            SolverRegenerationModExtension modExt = armor.def.GetModExtension<SolverRegenerationModExtension>();
            float regenSpeedMultiplier = modExt != null ? modExt.regenSpeedMultiplier : 10f;

            CompArmorDurability comp = armor.TryGetComp<CompArmorDurability>();
            if (comp == null || !comp.durabilityProps.Regenerates)
            {
                return;
            }

            if (comp.curDurability < comp.maxDurability)
            {
                float customRegen = comp.durabilityProps.RegenValue * regenSpeedMultiplier;
                float delta = Mathf.Min(customRegen, comp.maxDurability - comp.curDurability);
                comp.curDurability += delta;
            }
        }
    }
}


