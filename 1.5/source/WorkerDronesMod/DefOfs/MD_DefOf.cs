using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    [DefOf]
    public static class MD_DefOf
    {
        //------------------ThingDefs------------------
        public static ThingDef Neutroamine;

        public static ThingDef MD_FilthNeutroamineOil;

        public static ThingDef SunLamp;

        //==================RaceDefs==================
        public static ThingDef MD_CoreHeartRace;
        //====================================MD_Mote_RailgunLaserBase

        //==================MoteDefs==================
        public static ThingDef MD_Mote_RailgunLaserBase;

        public static ThingDef MD_Mote_DroneOverHeating;
        //============================================

        //------------------------------------

        //------------------EffectorDefs------------------
        public static EffecterDef MD_UVOverHeating;

        //------------------PawnKindDefs------------------
        public static PawnKindDef MD_DisassemblyDrone;

        public static PawnKindDef MD_DisassemblyDroneSquadLeader;

        public static PawnKindDef MD_WorkerDrone;

        public static PawnKindDef MD_CoreHeartBasic;
        //------------------------------------

        //------------------FactionDefs------------------
        public static FactionDef MD_DisassemblyDronesFaction;
        //------------------------------------

        //------------------SoundDefs------------------
        public static SoundDef PredatorLarge_Eat;

        public static SoundDef MD_BreakingRibcage;

        public static SoundDef MD_Railgun_PowerDown;
        //------------------------------------

        //------------------HediffDefs------------------
        public static HediffDef VREA_Overheating;

        public static HediffDef VREA_NeutroLoss;

        public static HediffDef MD_OilLoss;

        public static HediffDef MD_NaniteAcidBuildup;

        public static HediffDef MD_NaniteAcidBurn;

        public static HediffDef MD_NaniteAcidSting;

        public static HediffDef MD_DigitalLobotomy;

        public static HediffDef MD_RoboticReconstruction;

        public static HediffDef MD_interchangeable_ClawHands;

        public static HediffDef MD_SolverDeathPrevention;

        public static HediffDef MD_FleshyPart;
        //------------------------------------

        //------------------DamageDefs------------------
        public static DamageDef MD_OverHeating_Burn;

        public static DamageDef MD_OverHeating;

        public static DamageDef MD_NaniteAcid;
        //------------------------------------

        //------------------DamageArmorCategoryDefs------------------
        public static DamageArmorCategoryDef Heat;
        //------------------------------------

        //------------------AbilityDefs------------------
        public static AbilityDef MD_WingPoweredFlight;

        public static AbilityDef MD_Dismisswings;

        public static AbilityDef MD_InterchangeableHandsAbility;

        public static AbilityDef MD_InterchangeableHandsAbility_Ranged;
        //------------------------------------

        //------------------JobDefs------------------
        public static JobDef MD_Job_RefuelWithNeutroamine;

        public static JobDef MD_Job_FeedOil;

        public static JobDef MD_Job_DeliverNeutroamine;

        public static JobDef MD_Job_RefuelWithCorpse;

        public static JobDef MD_Job_BootupIdle;

        public static JobDef WatchTelevision;
        //------------------------------------

        //------------------RecipeDefs------------------

        public static RecipeDef MD_ExtractNeutroamine;

        //-------------------------------------

        //------------------MentalStateDefs------------------
        public static MentalStateDef MD_RefuelMadness;

        public static MentalStateDef MD_RecoverAndBootUp;
        //-------------------------------------

        //------------------ThoughtDefs------------------

        public static ThoughtDef MD_ConsumedCorpseNeutroamineOil_Happy;

        public static ThoughtDef MD_ConsumedNeutroamineOil_Happy;

        //-----------------------------------------------

        //------------------GeneDefs------------------
        public static GeneDef MD_BasicSolver;

        public static GeneDef MD_NeutroamineOil;

        public static GeneDef MD_MemorySleepProcessing;

        public static GeneDef VREA_EMPVulnerability;

        //public static GeneDef MD_FacialRecognitionDisabled;
        //------------------------------------

        //------------------BodypartDefs------------------
        public static BodyPartDef Stomach;

        public static BodyPartDef Head;

        public static BodyPartDef Torso;

        public static BodyPartDef Hand;

        public static BodyPartDef Brain;
        //------------------------------------

    }
}
