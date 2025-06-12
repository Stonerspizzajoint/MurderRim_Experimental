using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(HealthCardUtility), "DrawHediffListing")]
    public static class HealthCardUtility_DrawHediffListing_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            List<CodeInstruction> list = codeInstructions.ToList();
            foreach (CodeInstruction code in list)
            {
                yield return code;
                if (code.opcode == OpCodes.Stloc_S)
                {
                    LocalBuilder localBuilder = code.operand as LocalBuilder;
                    if (localBuilder != null && localBuilder.LocalIndex == 9)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1, null);
                        yield return new CodeInstruction(OpCodes.Ldloca_S, 9);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HealthCardUtility_DrawHediffListing_Patch), "ReplaceBleedingTextIfAndroid", null, null));
                    }
                }
            }
            yield break;
        }

        public static void ReplaceBleedingTextIfAndroid(Pawn pawn, ref string text)
        {
            // Calculate the current bleeding rate.
            float bleedRateTotal = pawn.health.hediffSet.BleedRateTotal;

            if (pawn.HasActiveGene(MD_DefOf.MD_NeutroamineOil))
            {
                Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(VREA_DefOf.VREA_NeutroLoss, false);
                if (firstHediffOfDef != null && firstHediffOfDef.Severity >= 1f)
                {
                    text = "VREA.NeutroamineLeakedOutCompletely".Translate();
                    return;
                }
                text = "VREA.NeutrolossRate".Translate() + ": " + bleedRateTotal.ToStringPercent() + "/" + "LetterDay".Translate();
                int numTicks = TicksUntilTotalNeutroloss(pawn);
                text += " (" + "VREA.TotalNeutroLoss".Translate(numTicks.ToStringTicksToPeriod(true, false, true, true, false)) + ")";
            }

            if (pawn.HasActiveGene(MD_DefOf.MD_WeakenedSolver))
            {

                // Consume oil based on how much the pawn is bleeding.
                Gene_NeutroamineOil oilGene = pawn.genes.GetFirstGeneOfType<Gene_NeutroamineOil>();
                if (oilGene != null)
                {
                    // Define how much oil is lost per unit bleed.
                    // Adjust oilLossMultiplier to balance the consumption rate.
                    const float oilLossMultiplier = 0.001f;
                    oilGene.Value = Mathf.Max(oilGene.Value - (bleedRateTotal * oilLossMultiplier), 0f);
                }

                Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(MD_DefOf.MD_OilLoss, false);
                if (firstHediffOfDef != null && firstHediffOfDef.Severity >= 1f)
                {
                    text = "MD.OilLeakedOutCompletely".Translate();
                    return;
                }
                text = "MD.OillossRate".Translate() + ": " + bleedRateTotal.ToStringPercent() + "/" + "LetterDay".Translate();
                int numTicks = TicksUntilTotalNeutroloss(pawn);
                text += " (" + "MD.TotalOilLoss".Translate(numTicks.ToStringTicksToPeriod(true, false, true, true, false)) + ")";
            }
        }

        public static int TicksUntilTotalNeutroloss(Pawn pawn)
        {
            float bleedRateTotal = pawn.health.hediffSet.BleedRateTotal;
            if (bleedRateTotal < 0.0001f)
            {
                return int.MaxValue;
            }

            // Get severity for VREA_NeutroLoss.
            Hediff neutroLossHediff = pawn.health.hediffSet.GetFirstHediffOfDef(VREA_DefOf.VREA_NeutroLoss, false);
            float neutroLossSeverity = neutroLossHediff != null ? neutroLossHediff.Severity : 0f;

            // Get severity for MD_OilLoss.
            Hediff oilLossHediff = pawn.health.hediffSet.GetFirstHediffOfDef(MD_DefOf.MD_OilLoss, false);
            float oilLossSeverity = oilLossHediff != null ? oilLossHediff.Severity : 0f;

            // Combine both severities.
            float totalSeverity = neutroLossSeverity + oilLossSeverity;
            // Clamp the total severity between 0 and 1 so it doesn't exceed full loss.
            totalSeverity = Mathf.Clamp(totalSeverity, 0f, 1f);

            // Calculate the remaining fraction until total loss (1.0).
            float remainingFraction = 1f - totalSeverity;

            // Convert that remaining fraction into ticks.
            return (int)((remainingFraction / bleedRateTotal) * 60000f);
        }
    }
}
