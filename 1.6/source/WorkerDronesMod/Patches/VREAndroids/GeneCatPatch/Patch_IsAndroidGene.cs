using HarmonyLib;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(VREAndroids.Utils), nameof(VREAndroids.Utils.IsAndroidGene))]
    public static class Patch_IsAndroidGene
    {
        static void Postfix(GeneDef geneDef, ref bool __result)
        {
            // If already true, leave as is
            if (__result) return;

            // Consider any gene with your custom category as an android gene
            if (geneDef.displayCategory is AndroidGeneCategoryDef)
            {
                __result = true;
            }
        }
    }
}

