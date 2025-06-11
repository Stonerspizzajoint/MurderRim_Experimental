// WorkerDronesMod.Patches.CE.Patch_VerbShootCE_OnCastSuccessful.cs
using System.Reflection;
using HarmonyLib;
using Verse;

namespace WorkerDronesMod.Patches.CE
{
    [HarmonyPatch]  // no typeof(…) here
    public static class Patch_VerbShootCE_OnCastSuccessful
    {
        // Only patch if CE is loaded
        static MethodBase TargetMethod()
        {
            var ceType = AccessTools.TypeByName("CombatExtended.Verb_ShootCE");
            if (ceType == null) return null;
            return AccessTools.Method(ceType, "OnCastSuccessful");
        }

        // __instance arrives as object; we use reflection to grab EquipmentSource
        static void Postfix(object __instance)
        {
            // get the EquipmentSource property
            var prop = __instance.GetType().GetProperty("EquipmentSource", BindingFlags.Public | BindingFlags.Instance);
            var eqSource = prop?.GetValue(__instance) as ThingWithComps;
            if (eqSource == null) return;

            // apply your heat‐per‐shot
            var heatComp = eqSource.TryGetComp<CompHeatPerShot>();
            if (heatComp != null)
            {
                heatComp.AddHeatOnShot();
                // or: heatComp.AddHeatOnShotGradual(yourValue);
            }
        }
    }
}

