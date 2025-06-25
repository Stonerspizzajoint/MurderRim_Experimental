using System.Collections.Generic;
using FacialAnimation;
using Verse;

namespace WorkerDronesMod
{
    /// <summary>
    /// A mod extension for gene defs that forces a pawn to use specific face types from provided lists.
    /// </summary>
    public class GeneForcedFacetypesExtension : DefModExtension
    {
        /// <summary>
        /// If set, any pawn having a gene with this extension will randomly use one of these head types.
        /// </summary>
        public List<FacialAnimation.HeadTypeDef> forcedHeadTypes = new List<FacialAnimation.HeadTypeDef>();

        /// <summary>
        /// Randomly use one of these brow types.
        /// </summary>
        public List<BrowTypeDef> forcedBrowTypes = new List<BrowTypeDef>();

        /// <summary>
        /// Randomly use one of these mouth types.
        /// </summary>
        public List<MouthTypeDef> forcedMouthTypes = new List<MouthTypeDef>();

        /// <summary>
        /// Randomly use one of these eye types.
        /// </summary>
        public List<EyeballTypeDef> forcedEyeTypes = new List<EyeballTypeDef>();

        /// <summary>
        /// Randomly use one of these lid types.
        /// </summary>
        public List<LidTypeDef> forcedLidTypes = new List<LidTypeDef>();

        public bool EyeColorMatchesSkinColor = false;
        public bool BrowColorMatchesSkinColor = false;
        public bool LidColorMatchesSkinColor = false;
    }
}

