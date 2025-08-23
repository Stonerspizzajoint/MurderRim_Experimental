using HarmonyLib;
using RimWorld;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(PawnRelationWorker_Parent), "CreateRelation")]
    public static class Patch_ParentRelation_CreateRelation
    {
        public static bool Prefix(Pawn generated, Pawn other, ref PawnGenerationRequest request)
        {
            // Only patch for androids
            if (Utils.IsAndroid(generated) && Utils.IsAndroid(other))
            {
                // Set both as parents, regardless of gender
                generated.relations.AddDirectRelation(PawnRelationDefOf.Parent, other);

                // If you want both to be set as "Mother" and "Father" (for UI), you can do:
                if (generated.GetMother() == null)
                    generated.SetMother(other);
                else if (generated.GetFather() == null)
                    generated.SetFather(other);

                // Optionally resolve name as in vanilla
                // You may want to call the vanilla ResolveMyName method here if needed

                // Skip original method
                return false;
            }
            // Otherwise, run vanilla logic
            return true;
        }
    }
}

