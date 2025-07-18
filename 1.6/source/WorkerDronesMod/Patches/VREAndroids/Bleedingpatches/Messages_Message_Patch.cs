using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld.Planet;
using Verse;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    // Token: 0x0200010C RID: 268
    [HarmonyPatch(typeof(Messages), "Message", new Type[]
    {
        typeof(string),
        typeof(LookTargets),
        typeof(MessageTypeDef),
        typeof(bool)
    })]
    public static class Messages_Message_Patch
    {
        public static void Prefix(ref string text, LookTargets lookTargets, MessageTypeDef def, bool historical = true)
        {
            object obj;
            if (lookTargets == null)
            {
                obj = null;
            }
            else
            {
                List<GlobalTargetInfo> targets = lookTargets.targets;
                obj = ((targets != null) ? targets.FirstOrDefault<GlobalTargetInfo>().Thing : null);
            }
            Pawn pawn = obj as Pawn;

            var neutroGenes = GeneDefHelper.GetGeneDefAndAlternative(
                MD_DefOf.MD_NeutroamineOil,
                MD_DefOf.VREA_MD_NeutroamineOil
            ).ToArray();

            if (pawn != null && GeneDefHelper.PawnHasAnyGene(pawn, neutroGenes)
                && text == "CannotRescue".Translate() + ": " + "NoNonPrisonerBed".Translate())
            {
                text = "CannotRescue".Translate() + ": " + "VREA.NoNeutroCasket".Translate();
            }
        }
    }
}
