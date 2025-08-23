using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;
using System.Linq;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn_AgeTracker), "RecalculateLifeStageIndex")]
    public static class Patch_Pawn_AgeTracker_KeepBabyAndroidLifestage
    {
        // Cache FieldInfos and MethodInfo for performance
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_AgeTracker), "pawn");
        private static readonly FieldInfo GrowthField = AccessTools.Field(typeof(Pawn_AgeTracker), "growth");
        private static readonly FieldInfo CachedLifeStageIndexField = AccessTools.Field(typeof(Pawn_AgeTracker), "cachedLifeStageIndex");
        private static readonly FieldInfo LifeStageChangeField = AccessTools.Field(typeof(Pawn_AgeTracker), "lifeStageChange");
        private static readonly MethodInfo GetLifeStageAgeMethod = AccessTools.Method(typeof(Pawn_AgeTracker), "GetLifeStageAge");

        static bool Prefix(Pawn_AgeTracker __instance)
        {
            var pawn = PawnField.GetValue(__instance) as Pawn;
            if (BabyAndroidUtil.IsBabyAndroid(pawn))
            {
                // Find the baby life stage for this race
                var babyStage = pawn.RaceProps.lifeStageAges.FirstOrDefault(x => x.def == LifeStageDefOf.HumanlikeBaby);
                int babyIndex = pawn.RaceProps.lifeStageAges.IndexOf(babyStage);
                if (babyIndex >= 0)
                {
                    GrowthField.SetValue(__instance, 0f);
                    CachedLifeStageIndexField.SetValue(__instance, babyIndex);
                    LifeStageChangeField.SetValue(__instance, true);

                    LongEventHandler.ExecuteWhenFinished(delegate
                    {
                        pawn.Drawer?.renderer?.SetAllGraphicsDirty();
                        if (pawn.IsColonist)
                        {
                            PortraitsCache.SetDirty(pawn);
                        }
                    });
                    __instance.CheckChangePawnKindName();
                    LifeStageWorker worker = __instance.CurLifeStage.Worker;
                    var lifeStageAge = GetLifeStageAgeMethod.Invoke(__instance, new object[] { babyIndex }) as LifeStageAge;
                    worker.Notify_LifeStageStarted(pawn, lifeStageAge?.def);
                    if (pawn.SpawnedOrAnyParentSpawned)
                    {
                        PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, false);
                    }
                    return false; // Skip vanilla logic
                }
            }
            return true; // Use vanilla logic otherwise
        }
    }
}

