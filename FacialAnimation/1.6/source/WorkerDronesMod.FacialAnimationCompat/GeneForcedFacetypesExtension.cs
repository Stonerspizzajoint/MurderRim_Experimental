using System.Collections.Generic;
using FacialAnimation;
using Verse;

namespace WorkerDronesMod.FacialAnimationCompat
{

    public class GeneForcedFacetypesExtension : DefModExtension
    {
        public bool EyeColorMatchesSkinColor = false;
        public bool forceMouthColorWhite;
        public bool hideFaceOnDeath;

        public List<string> raceTags = new List<string>();
    }
}
