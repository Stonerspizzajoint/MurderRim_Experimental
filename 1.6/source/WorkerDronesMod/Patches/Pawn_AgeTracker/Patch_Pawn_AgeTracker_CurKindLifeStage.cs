using HarmonyLib;
using Verse;
using RimWorld;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn_AgeTracker), "CurKindLifeStage", MethodType.Getter)]
    public static class Patch_Pawn_AgeTracker_CurKindLifeStage
    {
        static bool Prefix(Pawn_AgeTracker __instance, ref PawnKindLifeStage __result)
        {
            Pawn pawn = AccessTools.Field(typeof(Pawn_AgeTracker), "pawn").GetValue(__instance) as Pawn;
            if (pawn != null && pawn.RaceProps.Humanlike && BabyAndroidUtil.IsBabyAndroid(pawn))
            {
                // Allow lookup for baby androids
                int curLifeStageIndex = __instance.CurLifeStageIndex;
                if (pawn.kindDef != null && pawn.kindDef.lifeStages != null && curLifeStageIndex >= 0 && curLifeStageIndex < pawn.kindDef.lifeStages.Count)
                {
                    __result = pawn.kindDef.lifeStages[curLifeStageIndex];
                    return false; // Skip original getter
                }
            }
            // Let vanilla code run for all other cases
            return true;
        }
    }
}

