using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Lord), "Notify_PawnLost")]
    public static class Patch_PreventRaidRemoval
    {
        static bool Prefix(Lord __instance, Pawn pawn, PawnLostCondition cond, DamageInfo? dinfo = null)
        {
            // Only block removal if pawn is downed (incapped) and has the solver gene
            if (cond == PawnLostCondition.Incapped)
            {
                var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
                if (gene != null)
                {
                    // Prevent removal from the Lord (raid group)
                    return false; // Skip original method
                }
            }
            return true; // Allow normal behavior otherwise
        }
    }
}

