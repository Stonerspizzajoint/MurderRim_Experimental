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
                    pawn.health.AddHediff(ext.hediffDef, part);
#if DEBUG
                    Log.Message($"[Gene_HediffApplier] Added {ext.hediffDef.defName} to {pawn.LabelShort}'s {part.def.defName}");
#endif
                }
            }
        }
    }
}

