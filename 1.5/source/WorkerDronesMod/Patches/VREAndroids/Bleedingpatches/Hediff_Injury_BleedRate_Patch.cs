using HarmonyLib;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Hediff_Injury), "BleedRate", MethodType.Getter)]
    public static class Hediff_Injury_BleedRate_Patch
    {
        // Highest priority so we override everything
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(ref float __result, Hediff_Injury __instance)
        {
            if (__instance.pawn.HasActiveGene(MD_DefOf.MD_NeutroamineOil))
            {
                __result = CustomBleedRate(__instance);
                return false;
            }
            return true;
        }

        private static float CustomBleedRate(Hediff_Injury inj)
        {
            if (inj.pawn.Dead || inj.IsTended() || inj.IsPermanent())
                return 0f;

            float rate = inj.Severity * inj.def.injuryProps.bleedRate;
            if (inj.Part != null)
                rate *= inj.Part.def.bleedRate;

            return rate;
        }
    }
}


