using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class Comp_HatsOnly : ThingComp
    {
        public CompProperties_HatsOnly Props => (CompProperties_HatsOnly)this.props;

        // Run our check every 60 ticks.
        public override void CompTick()
        {
            base.CompTick();

            if (Find.TickManager.TicksGame % 60 != 0)
                return;

            Pawn pawn = this.parent as Pawn;
            if (pawn == null || pawn.apparel == null)
                return;

            // Iterate backwards through the list to safely remove items while iterating.
            for (int i = pawn.apparel.WornApparel.Count - 1; i >= 0; i--)
            {
                Apparel apparel = pawn.apparel.WornApparel[i];
                if (apparel == null)
                    continue;

                if (Props.onlyAllowHats)
                {
                    // Determine if the apparel qualifies as a hat.
                    // We consider it a hat if every defined body part group is either UpperHead or FullHead.
                    bool isHat = true;
                    if (apparel.def.apparel == null ||
                        apparel.def.apparel.bodyPartGroups == null ||
                        apparel.def.apparel.bodyPartGroups.Count == 0)
                    {
                        isHat = false;
                    }
                    else
                    {
                        foreach (var group in apparel.def.apparel.bodyPartGroups)
                        {
                            if (!(group.defName == "UpperHead" || group.defName == "FullHead" || group.defName == "Neck" || group.defName == "Shoulders"))
                            {
                                isHat = false;
                                break;
                            }
                        }
                    }

                    // If the apparel does not qualify as a hat, remove and drop it.
                    if (!isHat)
                    {
                        pawn.apparel.Remove(apparel);
                        // Drop the apparel on the ground near the pawn.
                        GenPlace.TryPlaceThing(apparel, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                    }
                }
            }
        }
    }
}
