using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace WorkerDronesMod
{
    // This ability effect component toggles hediff(s) on the caster's specified body parts.
    public class Comp_ToggleHediffEffect : CompAbilityEffect
    {
        public CompProperties_ToggleHediffEffect Props => (CompProperties_ToggleHediffEffect)this.props;

        // Tracks whether the toggled hediff(s) are currently active.
        public bool toggledOn = true;

        /// <summary>
        /// Called when the ability is cast on local targets.
        /// </summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Toggle();
        }

        /// <summary>
        /// Called when the ability is cast on a global target.
        /// </summary>
        public override void Apply(GlobalTargetInfo target)
        {
            Toggle();
        }

        /// <summary>
        /// Toggles the configured hediff(s) on or off on the caster pawn.
        /// If toggled off, adds the hediff(s) to each allowed body part (or defaults to torso).
        /// If toggled on, removes them.
        /// </summary>
        public void Toggle()
        {
            // Get the caster pawn from the ability's verb.
            Pawn pawn = parent.verb.CasterPawn;
            if (pawn == null)
                return;

            // Determine target body parts.
            List<BodyPartRecord> targetParts = new List<BodyPartRecord>();
            if (Props.allowedBodyPartDefs != null && Props.allowedBodyPartDefs.Count > 0)
            {
                foreach (string partDef in Props.allowedBodyPartDefs)
                {
                    BodyPartRecord bp = pawn.health.hediffSet.GetNotMissingParts()
                        .FirstOrDefault(x => x.def.defName == partDef);
                    if (bp != null)
                        targetParts.Add(bp);
                }
            }
            else
            {
                BodyPartRecord torso = pawn.health.hediffSet.GetNotMissingParts()
                    .FirstOrDefault(x => x.def == MD_DefOf.Torso);
                if (torso != null)
                    targetParts.Add(torso);
            }

            if (!toggledOn)
            {
                // Toggle on: add each hediff on every target part if not already present.
                foreach (string defName in Props.hediffDefsToToggle)
                {
                    HediffDef hdDef = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
                    if (hdDef == null)
                        continue;

                    foreach (BodyPartRecord part in targetParts)
                    {
                        if (!pawn.health.hediffSet.hediffs.Any(x => x.def == hdDef && x.Part == part))
                        {
                            Hediff newHd = HediffMaker.MakeHediff(hdDef, pawn, part);
                            newHd.Severity = Props.defaultSeverity;
                            pawn.health.AddHediff(newHd);
                        }
                    }
                }
                toggledOn = true;
            }
            else
            {
                // Toggle off: remove all instances of each toggled hediff from each target part.
                foreach (string defName in Props.hediffDefsToToggle)
                {
                    HediffDef hdDef = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
                    if (hdDef == null)
                        continue;

                    foreach (BodyPartRecord part in targetParts)
                    {
                        List<Hediff> toRemove = pawn.health.hediffSet.hediffs
                            .Where(x => x.def == hdDef && x.Part == part).ToList();
                        foreach (Hediff hd in toRemove)
                        {
                            pawn.health.RemoveHediff(hd);
                        }
                    }
                }
                toggledOn = false;
            }
        }
    }
}
















