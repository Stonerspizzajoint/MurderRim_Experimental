using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    public class CompProperties_AnimalBedUser : CompProperties
    {
        /// <summary>
        /// When true, pawns with this component are allowed to use animal beds.
        /// </summary>
        public bool canUseAnimalBeds = true;

        /// <summary>
        /// The offset to apply when positioning a pawn on an animal bed.
        /// Editable via XML.
        /// </summary>
        public Vector3 sleepDrawOffset = new Vector3(0f, 0f, 0f);

        public CompProperties_AnimalBedUser()
        {
            this.compClass = typeof(CompAnimalBedUser);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            // Optionally warn if this comp is placed on a non‑humanlike pawn.
            if (parentDef.race != null && !parentDef.race.Humanlike)
            {
                yield return "CompProperties_AnimalBedUser is intended for humanlike pawns only.";
            }
        }
    }
}
