using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace WorkerDronesMod
{
    public class ScenPart_CustomXenotypeOverrides : ScenPart_ConfigPage_ConfigureStartingPawns_Xenotypes
    {
        public new List<XenotypePawnKindWithFlag> overrideKinds = new List<XenotypePawnKindWithFlag>();

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            base.DoEditInterface(listing);
            if (overrideKinds != null)
            {
                foreach (var overrideKind in overrideKinds)
                {
                    string label = $"Allow only one for {overrideKind.xenotype?.label ?? "?"}/{overrideKind.pawnKind?.label ?? "?"}";
                    listing.CheckboxLabeled(label, ref overrideKind.IsLeader);
                }
            }
        }

        protected override void GenerateStartingPawns()
        {
            // Clear any existing pawns
            StartingPawnUtility.ClearAllStartingPawns();

            // Find the leader and regular overrideKinds
            var leaderOverride = overrideKinds.FirstOrDefault(x => x.IsLeader);
            var regularOverride = overrideKinds.FirstOrDefault(x => !x.IsLeader);

            // Find the xenotypeCount for the relevant xenotype
            foreach (var xenotypeCount in xenotypeCounts)
            {
                int total = xenotypeCount.count;
                int leaderCount = (leaderOverride != null && leaderOverride.xenotype == xenotypeCount.xenotype) ? 1 : 0;
                int regularCount = total - leaderCount;

                int pawnIndex = 0;

                // Generate leader pawn if needed
                if (leaderCount == 1)
                {
                    var req = StartingPawnUtility.GetGenerationRequest(pawnIndex);
                    req.ForcedXenotype = leaderOverride.xenotype;
                    req.PawnKindDefGetter = _ => leaderOverride.pawnKind;
                    StartingPawnUtility.SetGenerationRequest(pawnIndex, req);
                    StartingPawnUtility.AddNewPawn(pawnIndex);
                    pawnIndex++;
                }

                // Generate regular pawns
                for (int i = 0; i < regularCount; i++)
                {
                    var req = StartingPawnUtility.GetGenerationRequest(pawnIndex);
                    req.ForcedXenotype = xenotypeCount.xenotype;
                    if (regularOverride != null && regularOverride.xenotype == xenotypeCount.xenotype)
                        req.PawnKindDefGetter = _ => regularOverride.pawnKind;
                    else
                        req.PawnKindDefGetter = null; // fallback to default
                    StartingPawnUtility.SetGenerationRequest(pawnIndex, req);
                    StartingPawnUtility.AddNewPawn(pawnIndex);
                    pawnIndex++;
                }
            }
        }


        public class XenotypePawnKindWithFlag : XenotypePawnKind, IExposable
        {
            public bool IsLeader;

            public void ExposeData()
            {
                Scribe_Defs.Look(ref xenotype, "xenotype");
                Scribe_Defs.Look(ref pawnKind, "pawnKind");
                Scribe_Values.Look(ref IsLeader, "IsLeader", false);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref overrideKinds, "overrideKinds", LookMode.Deep);
        }
    }
}

