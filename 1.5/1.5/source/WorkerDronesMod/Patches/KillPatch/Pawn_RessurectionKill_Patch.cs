using System.Linq;
using HarmonyLib;
using Verse;
using Verse.AI;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn), "Kill", new System.Type[] { typeof(DamageInfo?), typeof(Hediff) })]
    public static class Pawn_ResurrectionKill_Patch
    {
        static bool Prefix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
        {
            // If the pawn is already dead, cancel any further Kill() logic.
            if (__instance.Dead)
            {
                return false;
            }

            // Helper: if death is about to occur, remove stasis hediff (if present).
            void RemoveStasisIfPresent(Pawn pawn)
            {
                var stasisDef = MD_DefOf.MD_ResurrectionStasis;
                if (stasisDef != null && pawn.health.hediffSet.HasHediff(stasisDef))
                {
                    pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(stasisDef));
                    Log.Message($"[Pawn_ResurrectionKill_Patch] {pawn.LabelShort}: MD_ResurrectionStasis removed before death.");
                }
            }

            // Only androids with the solver gene get special treatment.
            var neutroamineGene = __instance.genes?.GetFirstGeneOfType<Gene_NeutroamineOil>();
            if (neutroamineGene == null || !neutroamineGene.HasSolver || !__instance.IsAndroid())
            {
                RemoveStasisIfPresent(__instance);
                return true; // proceed with normal death
            }

            // CoreHeart race: let them die naturally (but drop stasis first)
            if (neutroamineGene.IsACoreHeart)
            {
                RemoveStasisIfPresent(__instance);
                return true;
            }

            // Digital lobotomy: let them die
            if (__instance.health.hediffSet.HasHediff(MD_DefOf.MD_DigitalLobotomy))
            {
                RemoveStasisIfPresent(__instance);
                return true;
            }

            // Nanite Acid Buildup: let them die
            if (SolverGeneUtility.IsNaniteAcidBuildupActive(__instance))
            {
                RemoveStasisIfPresent(__instance);
                return true;
            }

            // Overheating above threshold: let them die
            var heatGene = __instance.genes?.GetFirstGeneOfType<Gene_HeatBuildup>();
            if (heatGene != null)
            {
                float heatPercent = heatGene.Value / heatGene.InitialResourceMax;
                bool isOverheating = __instance.health.hediffSet.HasHediff(MD_DefOf.MD_Overheating);
                if (isOverheating && heatPercent >= 0.6f)
                {
                    RemoveStasisIfPresent(__instance);
                    return true;
                }
            }

            // Missing stomach: natural death
            var stomach = __instance.health.hediffSet.GetNotMissingParts()
                .FirstOrDefault(p => p.def == MD_DefOf.Stomach);
            if (stomach == null)
            {
                RemoveStasisIfPresent(__instance);
                return true;
            }

            // Missing reactor on stomach: natural death
            bool reactorOnStomach = __instance.health.hediffSet.hediffs.Any(
                h => h.def == VREA_DefOf.VREA_Reactor && h.Part == stomach);
            if (!reactorOnStomach)
            {
                RemoveStasisIfPresent(__instance);
                return true;
            }

            // Too many missing parts: natural death
            int intactParts = __instance.health.hediffSet.GetNotMissingParts()
                .Count(part => !__instance.health.hediffSet.hediffs.Any(h => h.Part == part && h.def == MD_DefOf.MD_FleshyPart));
            int totalParts = __instance.RaceProps.body.AllParts.Count;
            if (totalParts > 0 && ((float)intactParts / totalParts) < 0.2f)
            {
                RemoveStasisIfPresent(__instance);
                return true;
            }

            // Critically damaged by utility check: natural death
            if (SolverGeneUtility.IsCriticallyDamaged(__instance))
            {
                RemoveStasisIfPresent(__instance);
                return true;
            }

            // Otherwise—pawn meets rescue criteria—apply stasis and cancel Kill()
            var stasisDefFinal = MD_DefOf.MD_ResurrectionStasis;
            if (stasisDefFinal != null && !__instance.health.hediffSet.HasHediff(stasisDefFinal))
            {
                __instance.health.AddHediff(stasisDefFinal);
                Log.Message($"[Pawn_ResurrectionKill_Patch] {__instance.LabelShort} prevented from dying and placed in Resurrection Stasis.");
            }
            else
            {
                Log.Message($"[Pawn_ResurrectionKill_Patch] {__instance.LabelShort} is already in stasis; kill canceled.");
            }

            // End current job and undraft if needed
            __instance.jobs.EndCurrentJob(JobCondition.InterruptForced);
            if (__instance.drafter != null)
                __instance.drafter.Drafted = false;

            return false; // skip the original Kill()
        }
    }
}



