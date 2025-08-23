using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.HasPartsToWear))]
    public static class Patch_ApparelUtility_HasPartsToWear
    {
        static void Postfix(Pawn p, ThingDef apparel, ref bool __result)
        {
            if (p != null && p.def == MD_DefOf.MD_CoreHeartRace)
            {
                if (apparel.apparel.LastLayer != ApparelLayerDefOf.Overhead)
                {
                    __result = false;
                }
            }
        }
    }
}
