using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;
using System.Collections.Generic;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(StatWorker), "GetExplanationUnfinalized")]
    public static class Patch_StatWorker_GetExplanationUnfinalized
    {
        private static readonly FieldInfo statField = typeof(StatWorker).GetField("stat", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void Postfix(StatRequest req, ToStringNumberSense numberSense, ref string __result, StatWorker __instance)
        {
            if (req.HasThing && req.Thing is Pawn pawn && ExtraSolverUtils.HasSolver(pawn))
            {
                var stat = (StatDef)statField.GetValue(__instance);
                string extra = SolverTraitEffectManager.GetSolverTraitStatExplanation(pawn, stat);
                if (!string.IsNullOrEmpty(extra))
                {
                    __result += "\n" + extra;
                }
            }
        }
    }
}

