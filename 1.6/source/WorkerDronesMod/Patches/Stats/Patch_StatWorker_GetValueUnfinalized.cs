using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;


namespace WorkerDronesMod.Patches
{

    [HarmonyPatch(typeof(StatWorker), "GetValueUnfinalized")]
    public static class Patch_StatWorker_GetValueUnfinalized
    {
        private static readonly FieldInfo statField = typeof(StatWorker).GetField("stat", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Postfix(StatRequest req, bool applyPostProcess, ref float __result, StatWorker __instance)
        {
            if (req.HasThing && req.Thing is Pawn pawn && ExtraSolverUtils.HasSolver(pawn))
            {
                var stat = (StatDef)statField.GetValue(__instance);
                __result = SolverTraitEffectManager.ApplySolverTraitStatEffects(pawn, stat, __result);
            }
        }
    }
}
