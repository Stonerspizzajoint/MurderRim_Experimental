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
        // Token: 0x06000288 RID: 648 RVA: 0x0000EBCC File Offset: 0x0000CDCC
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
            if (pawn != null && pawn.HasActiveGene(MD_DefOf.MD_NeutroamineOil) && text == "CannotRescue".Translate() + ": " + "NoNonPrisonerBed".Translate())
            {
                text = "CannotRescue".Translate() + ": " + "VREA.NoNeutroCasket".Translate();
            }
        }
    }
}
