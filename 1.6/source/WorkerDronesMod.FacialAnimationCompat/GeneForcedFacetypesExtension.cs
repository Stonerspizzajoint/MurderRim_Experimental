using System.Collections.Generic;
using FacialAnimation;
using Verse;

namespace WorkerDronesMod.FacialAnimationCompat
{
    public enum FacePartType { Head, Brow, Mouth, Eye, Lid, LidOption, Skin }

    public class RaceTagOption : IExposable
    {
        public string raceTag;
        public FacePartType facePart;
        public bool overrideOthers;

        public void ExposeData()
        {
            Scribe_Values.Look(ref raceTag, "raceTag");
            Scribe_Values.Look(ref facePart, "facePart");
            Scribe_Values.Look(ref overrideOthers, "overrideOthers");
        }
    }

    public class GeneForcedFacetypesExtension : DefModExtension
    {
        public List<FacialAnimation.HeadTypeDef> forcedHeadTypes = new List<FacialAnimation.HeadTypeDef>();
        public List<BrowTypeDef> forcedBrowTypes = new List<BrowTypeDef>();
        public List<MouthTypeDef> forcedMouthTypes = new List<MouthTypeDef>();
        public List<EyeballTypeDef> forcedEyeTypes = new List<EyeballTypeDef>();
        public List<LidTypeDef> forcedLidTypes = new List<LidTypeDef>();
        public List<LidOptionTypeDef> forcedLidOptionTypes = new List<LidOptionTypeDef>();
        public List<SkinTypeDef> forcedSkinTypes = new List<SkinTypeDef>();

        public bool EyeColorMatchesSkinColor = false;
        public bool SkinsColorMatchesSkinColor = false;
        public bool forceMouthColorWhite;
        public bool hideFaceOnDeath;

        public List<string> raceTags = new List<string>();
        public List<RaceTagOption> raceTagOptions = new List<RaceTagOption>();
    }
}
