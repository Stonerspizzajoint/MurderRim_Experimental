using System;
using System.Linq;
using Verse;
using VREAndroids;

namespace WorkerDronesMod
{
    public class Gene_HediffApplier : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();

            if (!pawn.IsAndroid()) return;

            var ext = def.GetModExtension<AndroidBodypartHediffExtension>();
            if (ext?.hediffDef == null || ext.bodyParts == null || ext.bodyParts.Count == 0)
                return;

            // Build a Lookup: defName → all BodyPartRecords with that name
            var lookup = pawn.health.hediffSet
                            .GetNotMissingParts()
                            .ToLookup(p => p.def.defName, StringComparer.OrdinalIgnoreCase);

            foreach (string partLabel in ext.bodyParts.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var parts = lookup[partLabel];
                if (!parts.Any())
                {
#if DEBUG
                    Log.Warning($"[Gene_HediffApplier] No part '{partLabel}' on {pawn.LabelShort}");
#endif
                    continue;
                }

                foreach (var part in parts)
                {
                    // Only add if the part is not missing and the hediff is not already present
                    bool alreadyPresent = pawn.health.hediffSet.hediffs.Any(h =>
                        h.def == ext.hediffDef && h.Part == part && !pawn.health.hediffSet.PartIsMissing(part));

                    if (!alreadyPresent)
                    {
                        pawn.health.AddHediff(ext.hediffDef, part);
#if DEBUG
                        Log.Message($"[Gene_HediffApplier] Added {ext.hediffDef.defName} to {pawn.LabelShort}'s {part.def.defName}");
#endif
                    }
                }
            }
        }
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);

            if (!pawn.IsAndroid()) return;

            var ext = def.GetModExtension<AndroidBodypartHediffExtension>();
            if (ext?.hediffDef == null || ext.bodyParts == null || ext.bodyParts.Count == 0)
                return;

            // Build a Lookup: defName → all BodyPartRecords with that name
            var lookup = pawn.health.hediffSet
                            .GetNotMissingParts()
                            .ToLookup(p => p.def.defName, StringComparer.OrdinalIgnoreCase);

            foreach (string partLabel in ext.bodyParts.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var parts = lookup[partLabel];
                if (!parts.Any())
                    continue;

                foreach (var part in parts)
                {
                    // Only add if the hediff is missing from this part
                    bool missingHediff = !pawn.health.hediffSet.hediffs.Any(h =>
                        h.def == ext.hediffDef && h.Part == part);

                    if (missingHediff)
                    {
                        pawn.health.AddHediff(ext.hediffDef, part);
#if DEBUG
                        Log.Message($"[Gene_HediffApplier] TickRare: Added {ext.hediffDef.defName} to {pawn.LabelShort}'s {part.def.defName}");
#endif
                    }
                }
            }
        }
    }
}

