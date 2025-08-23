using HarmonyLib;
using RimWorld;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(PawnRelationWorker_Child), "CreateRelation")]
    public static class Patch_ChildRelation_CreateRelation
    {
        public static bool Prefix(Pawn generated, Pawn other, ref PawnGenerationRequest request)
        {
            // Only patch for androids
            if (Utils.IsAndroid(generated) && Utils.IsAndroid(other))
            {
                // Set as parent regardless of gender
                generated.relations.AddDirectRelation(PawnRelationDefOf.Parent, other);

                // Optionally set as mother/father if not already set
                if (generated.GetMother() == null)
                    generated.SetMother(other);
                else if (generated.GetFather() == null)
                    generated.SetFather(other);

                // Skip vanilla logic
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PawnRelationWorker_Child), "InRelation")]
    public static class Patch_ChildRelation_InRelation
    {
        public static bool Prefix(Pawn me, Pawn other, ref bool __result)
        {
            // For androids, check Parent relation instead of just Mother/Father
            if (Utils.IsAndroid(me) && Utils.IsAndroid(other))
            {
                __result = me != other && other.relations.DirectRelationExists(PawnRelationDefOf.Parent, me);
                return false;
            }
            return true;
        }
    }
}
