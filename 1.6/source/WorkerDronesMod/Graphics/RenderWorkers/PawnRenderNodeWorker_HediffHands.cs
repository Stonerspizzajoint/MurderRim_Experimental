using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WorkerDronesMod
{
    public class PawnRenderNodeWorker_HediffHands : PawnRenderNodeWorker_Body
    {
        /// <summary>
        /// Overrides the draw request append method.
        /// When there is only one hediff of a given type on the pawn, it will only be rendered if the pawn is facing
        /// the correct direction based on the hediff's custom label. For two hediffs, the same rule applies.
        /// Facing East should render the hediff with custom label "right hand", and facing West should render
        /// the hediff with custom label "left hand".
        /// </summary>
        /// <param name="node">The render node for a specific hediff.</param>
        /// <param name="parms">Pawn drawing parameters.</param>
        /// <param name="requests">The list of graphic draw requests to append to.</param>
        public override void AppendDrawRequests(PawnRenderNode node, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
        {
            if (node.hediff != null && node.hediff.Part != null &&
                (parms.facing == Rot4.East || parms.facing == Rot4.West))
            {
                // Count the number of hediffs on the pawn that share the same definition.
                int count = parms.pawn.health.hediffSet.hediffs.Count(h => h.def == node.hediff.def);

                if (count == 1)
                {
                    // For a single hediff, allow drawing only if it matches the correct orientation.
                    if (string.Equals(node.hediff.Part.customLabel, "right hand", StringComparison.Ordinal))
                    {
                        // "right hand" should only draw when facing East.
                        if (parms.facing != Rot4.East)
                            return;
                    }
                    else if (string.Equals(node.hediff.Part.customLabel, "left hand", StringComparison.Ordinal))
                    {
                        // "left hand" should only draw when facing West.
                        if (parms.facing != Rot4.West)
                            return;
                    }
                }
                else if (count == 2)
                {
                    // For two hediffs, enforce the same logic:
                    // When facing East, render only the hediff with "right hand" custom label.
                    if (parms.facing == Rot4.East)
                    {
                        if (!string.Equals(node.hediff.Part.customLabel, "right hand", StringComparison.Ordinal))
                            return;
                    }
                    // When facing West, render only the hediff with "left hand" custom label.
                    else if (parms.facing == Rot4.West)
                    {
                        if (!string.Equals(node.hediff.Part.customLabel, "left hand", StringComparison.Ordinal))
                            return;
                    }
                }
            }

            // Default draw request processing.
            base.AppendDrawRequests(node, parms, requests);
        }
    }
}