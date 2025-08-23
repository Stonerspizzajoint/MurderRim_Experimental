using RimWorld;
using Verse;
using Verse.AI;

namespace WorkerDronesMod
{
    [DefOf]
    public static class MD_DefOf
    {
        //------------------ThingDefs------------------
        public static ThingDef Neutroamine;

        public static ThingDef MD_FilthNeutroamineOil;

        public static ThingDef SunLamp;

        public static ThingDef MD_Headgear_Hardhat;

        //==================RaceDefs==================
        public static ThingDef MD_CoreHeartRace;

        public static ThingDef MD_DroneBabyRace;
        //====================================MD_Mote_RailgunLaserBase

        //==================MoteDefs==================
        public static ThingDef MD_Mote_RailgunLaserBase;

        public static ThingDef MD_Mote_DroneOverHeating;

        public static ThingDef Mote_SolverStun;
        //============================================

        //===================PROJECTILE===================
        public static ThingDef MD_TelekineticProjectile;
        //===========================================

        //------------------------------------

        //-------------------ToolCapacityDefs------------------
        public static ToolCapacityDef Stab;

        public static ToolCapacityDef Blunt;
        //-----------------------------------------------------

        //------------------EffectorDefs------------------
        public static EffecterDef MD_UVOverHeating;

        public static EffecterDef MD_CoreExit;

        //------------------PawnKindDefs------------------
        public static PawnKindDef MD_DisassemblyDrone;

        public static PawnKindDef MD_DisassemblyDroneSquadLeader;

        public static PawnKindDef MD_WorkerDrone;

        public static PawnKindDef MD_CoreHeartBasic;

        public static PawnKindDef MD_PillBabyPawn;

        public static PawnKindDef VREA_AndroidAwakened;
        //------------------------------------

        //------------------BackstoryDefs------------------
        public static BackstoryDef ColonyBirthedAndroid;
        //-------------------------------------------------

        //-------------------LetterDefs------------------
        public static LetterDef BabyAndroidBirth;
        //-----------------------------------------------

        //------------------FactionDefs------------------
        public static FactionDef MD_DisassemblyDronesFaction;
        //------------------------------------

        //------------------SoundDefs------------------
        public static SoundDef PredatorLarge_Eat;

        public static SoundDef Ingest_Drink;

        public static SoundDef MD_BreakingRibcage;

        public static SoundDef MD_Railgun_PowerDown;

        public static SoundDef MD_ControlLevelUp;

        public static SoundDef MD_CoreExitSound;

        public static SoundDef Longjump_Land;
        //------------------------------------

        //------------------HediffDefs------------------
        public static HediffDef VREA_Overheating;

        public static HediffDef VREA_NeutroLoss;

        public static HediffDef MD_OilLoss;

        public static HediffDef MD_NaniteAcidBuildup;

        public static HediffDef MD_NaniteAcidBurn;

        public static HediffDef MD_NaniteAcidSting;

        public static HediffDef MD_NaniteNeutralized;

        public static HediffDef MD_DigitalLobotomy;

        public static HediffDef MD_BootupComa;

        public static HediffDef MD_RoboticReconstruction;

        public static HediffDef MD_interchangeable_ClawHands;

        public static HediffDef MD_SolverDeathPrevention;

        public static HediffDef MD_FleshyPart;

        public static HediffDef MD_BladedWingsFolded;

        public static HediffDef MD_BladedWings;
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
        //------------------------------------

        //------------------JobDefs------------------
        public static JobDef MD_Job_RefuelWithNeutroamine;

        public static JobDef MD_Job_FeedOil;

        public static JobDef MD_Job_DeliverNeutroamine;

        public static JobDef MD_Job_RefuelWithCorpse;

        public static JobDef MD_Job_BootupIdle;

        public static JobDef WatchTelevision;

        public static JobDef ExitMapFlyingInFormation;
        //------------------------------------

        //------------------DutyDefs------------------
        public static DutyDef MD_ExitMapPanicFly;
        //--------------------------------------------

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

        public static GeneDef MD_AbsoluteSolver;

        public static GeneDef MD_NeutroamineOil;

        public static GeneDef MD_MemorySleepProcessing;

        public static GeneDef VREA_EMPVulnerability;

        public static GeneDef MD_DroneBody;

        public static GeneDef MD_MurderDroneBody;

        public static GeneDef MD_DisplayColor_Random;

        public static GeneDef MD_DisplayEyes;
        public static GeneDef MD_SolverDisplayEyes;
        public static GeneDef MD_DisplayEyes_DD;
        public static GeneDef MD_DisplayEyes_XX;
        public static GeneDef MD_DisplayEye;

        public static GeneDef MD_BabyDisplayEyes;

        //public static GeneDef MD_FacialRecognitionDisabled;
        //------------------------------------

        //------------------SkillDefs------------------
        public static SkillDef SolverControl;
        //--------------------------------------------

        //------------------NeedDefs------------------
        public static NeedDef Joy;

        public static NeedDef Beauty;

        public static NeedDef Comfort;

        public static NeedDef Play;
        //--------------------------------------------

        //------------------BackStoryDefs------------------
        public static BackstoryDef NewbornAndroid;
        //-------------------------------------------------

        //------------------StatDefs------------------
        public static StatDef MD_HeatGainMultiplier;

        public static StatDef MD_RegenSpeedMultiplier;

        public static StatDef MD_HeatPerSeverity;

        public static StatDef MD_SolarHeatMultiplier;

        public static StatDef MD_AbilityHeatGainMultiplier;

        public static StatDef MD_AbilitySuccessChanceMultiplier;

        public static StatDef MD_AbilityCooldownMultiplier;

        public static StatDef MD_CorruptionDrainMultiplier;

        public static StatDef MD_AbilityCorruptionGainMultiplier;
        //--------------------------------------------

        //------------------BodypartDefs------------------
        public static BodyPartDef Stomach;

        public static BodyPartDef Head;

        public static BodyPartDef Torso;

        public static BodyPartDef Hand;

        public static BodyPartDef Brain;
        //------------------------------------

        //------------------HeadTypeDefs------------------
        public static HeadTypeDef MD_Drone_Head;
        //------------------------------------------------

    }
}
