using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill_SpawnCoreHeart
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance.Corpse != null && __instance.Corpse.Spawned && __instance.Corpse.Map != null && ExtraSolverUtils.HasSolver(__instance))
            {
                // Call your worker directly
                new DeathActionWorker_SpawnCoreHeart().PawnDied(__instance.Corpse, __instance.GetLord());
            }
        }
    }
}
