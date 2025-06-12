using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(ThoughtUtility), "ThoughtNullified")]
    public static class Patch_ThoughtUtility_ThoughtNullified
    {
        // Define the set of negative clothing thought def names we want to nullify.
        // (You can add or remove entries if your ideology defines more.)
        static readonly HashSet<string> disapprovedClothingThoughtDefs = new HashSet<string>
        {
            "AnyBodyPartCovered_Disapproved_Male",
            "AnyBodyPartCovered_Disapproved_Female",
            "AnyBodyPartCovered_Disapproved_Social_Male",
            "AnyBodyPartCovered_Disapproved_Social_Female",
            "AnyBodyPartButGroinCovered_Disapproved_Male",
            "AnyBodyPartButGroinCovered_Disapproved_Female",
            "AnyBodyPartButGroinCovered_Disapproved_Social_Male",
            "AnyBodyPartButGroinCovered_Disapproved_Social_Female",
            "GroinUncovered_Disapproved_Male",
            "GroinUncovered_Disapproved_Female",
            "GroinUncovered_Disapproved_Social_Male",
            "GroinUncovered_Disapproved_Social_Female",
            "GroinOrChestUncovered_Disapproved_Male",
            "GroinOrChestUncovered_Disapproved_Female",
            "GroinOrChestUncovered_Disapproved_Social_Male",
            "GroinOrChestUncovered_Disapproved_Social_Female",
            "GroinChestOrHairUncovered_Disapproved_Male",
            "GroinChestOrHairUncovered_Disapproved_Female",
            "GroinChestOrHairUncovered_Disapproved_Social_Male",
            "GroinChestOrHairUncovered_Disapproved_Social_Female"
        };

        static void Postfix(Pawn pawn, ThoughtDef def, ref bool __result)
        {
            // If the pawn already would have the thought nullified, do nothing.
            // Otherwise, if the pawn has our hats-only component
            // and the thought in question is one of the disapproved clothing ones,
            // force the nullification.
            if (pawn != null && def != null
                && pawn.TryGetComp<Comp_HatsOnly>() != null
                && disapprovedClothingThoughtDefs.Contains(def.defName))
            {
                __result = true;
            }
        }
    }
}
