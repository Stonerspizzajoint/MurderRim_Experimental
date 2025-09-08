using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), "PostApplyDamage")]
    public static class Patch_PreventDeath
    {
        static void Prefix(Pawn_HealthTracker __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null)
                return;

            // Only for pawns with Gene_BasicSolver
            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene == null)
                return;

            var ext = gene.def.GetModExtension<SolverGeneExtension>();
            var deathPreventionDef = MD_DefOf.MD_SolverDeathPrevention;

            // If death should NOT be prevented, remove the hediff now
            if (!SolverRegenerationUtil.CanDeathBePrevented(pawn, ext, gene))
            {
                var hediffToRemove = pawn.health.hediffSet.GetFirstHediffOfDef(deathPreventionDef);
                if (hediffToRemove != null)
                {
                    pawn.health.RemoveHediff(hediffToRemove);
                }
            }
        }
    }
}
