using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using Verse.Sound;

namespace WorkerDronesMod
{
    /// <summary>
    /// A basic solver gene resource that tracks Oil and Heat levels for Worker Drones.
    /// </summary>
    public class Gene_BasicSolver : Gene_Resource
    {
        public float Oil;
        public float heat;
        public float Heat
        {
            get => heat;
            set => heat = Mathf.Clamp(value, 0f, InitialResourceMax * 1.2f); // Cap at 120%
        }

        public float corruption;
        public float Corruption
        {
            get => corruption;
            set => corruption = Mathf.Clamp01(value); // 0 to 1
        }
        public SolverTraitProgress solverTraitProgress = new SolverTraitProgress();


        public override float InitialResourceMax => 100f;
        protected override Color BarColor => Color.yellow;
        protected override Color BarHighlightColor => Color.red;
        public override float MinLevelForAlert => 0.2f;

        public bool Overheatprotection = true;
        public bool RestrictToRoofedAreas = true;
        public Area lastNonShelterArea;
        public bool isNerfedSolver;
        public SolverGeneExtension ext;
        private int lastSolverControlLevel = -1;


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Oil, "Oil", InitialResourceMax);
            Scribe_Values.Look(ref heat, "Heat", 0f);
            Scribe_Values.Look(ref corruption, "corruption", 0f);
            Scribe_Values.Look(ref RestrictToRoofedAreas, "RestrictToRoofedAreas");
            Scribe_References.Look(ref lastNonShelterArea, "lastNonShelterArea");
            Scribe_Values.Look(ref isNerfedSolver, "isNerfedSolver");
            Scribe_Deep.Look(ref solverTraitProgress, "solverTraitProgress");
        }

        public Gene_BasicSolver()
        {
            Oil = InitialResourceMax;
            Heat = 0f;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            // 1) yield any existing gizmos (e.g. from base Gene_Resource)
            foreach (var g in base.GetGizmos())
                yield return g;

            // 2) debug buttons for all solvers
            if (Prefs.DevMode && DebugSettings.godMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: +10 Heat",
                    defaultDesc = "Adds 10 heat to this solver.",
                    icon = TexCommand.ForbidOn,
                    action = () =>
                    {
                        Heat = Mathf.Min(Heat + 10f, 100f);
                        Messages.Message("Added 10 Heat", MessageTypeDefOf.TaskCompletion, false);
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: -10 Heat",
                    defaultDesc = "Removes 10 heat from this solver.",
                    icon = TexCommand.ForbidOff,
                    action = () =>
                    {
                        Heat = Mathf.Max(Heat - 10f, 0f);
                        Messages.Message("Removed 10 Heat", MessageTypeDefOf.TaskCompletion, false);
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: +10 Oil",
                    defaultDesc = "Adds 10 oil to this solver.",
                    icon = TexCommand.ForbidOn,
                    action = () =>
                    {
                        Oil = Mathf.Min(Oil + 10f, InitialResourceMax);
                        Messages.Message("Added 10 Oil", MessageTypeDefOf.TaskCompletion, false);
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: -10 Oil",
                    defaultDesc = "Removes 10 oil from this solver.",
                    icon = TexCommand.ForbidOff,
                    action = () =>
                    {
                        Oil = Mathf.Max(Oil - 10f, 0f);
                        Messages.Message("Removed 10 Oil", MessageTypeDefOf.TaskCompletion, false);
                    }
                };

                // 3) corruption debug buttons only for non-nerfed solvers
                if (Prefs.DevMode && DebugSettings.godMode && !isNerfedSolver)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "DEBUG: +0.1 Corruption",
                        defaultDesc = "Adds 0.1 corruption to this solver.",
                        icon = TexCommand.ForbidOn,
                        action = () =>
                        {
                            Corruption = Mathf.Clamp01(Corruption + 0.1f);
                            Messages.Message("Added 0.1 Corruption", MessageTypeDefOf.TaskCompletion, false);
                        }
                    };
                    yield return new Command_Action
                    {
                        defaultLabel = "DEBUG: -0.1 Corruption",
                        defaultDesc = "Removes 0.1 corruption from this solver.",
                        icon = TexCommand.ForbidOff,
                        action = () =>
                        {
                            Corruption = Mathf.Clamp01(Corruption - 0.1f);
                            Messages.Message("Removed 0.1 Corruption", MessageTypeDefOf.TaskCompletion, false);
                        }
                    };
                }
            }
        }


        public override void Reset()
        {
            base.Reset();
            Oil = InitialResourceMax;
            Heat = 0f;
        }


        public override void PostAdd()
        {
            base.PostAdd();
            ext = def.GetModExtension<SolverGeneExtension>();
            if (ext != null)
                isNerfedSolver = ext.isNerfedSolver;

            // Set initial values for Oil and Heat
            Oil = InitialResourceMax;
            Heat = 0f;

            // Add the death prevention hediff if not already present
            if (pawn != null && !pawn.health.hediffSet.HasHediff(MD_DefOf.MD_SolverDeathPrevention))
            {
                pawn.health.AddHediff(MD_DefOf.MD_SolverDeathPrevention);
            }
        }

        public override void PostRemove()
        {
            base.PostRemove();
            // Remove the death prevention hediff if present
            if (pawn != null)
            {
                var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(MD_DefOf.MD_SolverDeathPrevention);
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }


        public override void Tick()
        {
            base.Tick();
            if (pawn == null || pawn.Dead || !pawn.Spawned)
                return;

            // Cache extension once, only update if def changes
            if (ext == null)
                ext = def.GetModExtension<SolverGeneExtension>();
            if (ext == null) return;

            int currentTick = Find.TickManager.TicksGame;

            // --- Fast logic: runs every tick ---
            float ambientDelta = HeatUtil.CalculateAmbientDelta(pawn, Heat, ext);
            HeatUtil.CalculateAndApplySolarHeatGain(pawn, ext);
            HeatUtil.AddHeat(pawn, ambientDelta, ext);

            OilUtil.HandleOilCooling(this, pawn, ext);
            OilUtil.HandleOilLossHediff(this, pawn);
            HeatUtil.HandleOverheating(this, pawn);

            if (!HeatUtil.IsOverheating(Heat, InitialResourceMax))
                HeatUtil.ClearOverheatWarning(this);

            // --- Regeneration logic: now runs every tick ---
            if (!SolarUtil.IsExtremeAmbientTemperature(pawn) && !HeatUtil.IsOverheating(Heat, InitialResourceMax))
                SolverRegenerationUtil.HandleHealingAndRegeneration(pawn, this, ext.regenOptions);

            ExtraSolverUtils.HandleAutoSheltering(this);
            SolverRegenerationUtil.TryBootUpIfBrainMissing(pawn);

            // --- Skill point awarding logic (runs every tick, but is cheap) ---
            int currentLevel = SolverLevelingUtil.GetSolverControlLevel(pawn);
            if (lastSolverControlLevel == -1)
                lastSolverControlLevel = currentLevel; // Initialize on first tick

            if (currentLevel > lastSolverControlLevel)
            {
                int gained = SolverLevelingUtil.SkillPointsGainedBetweenLevels(lastSolverControlLevel, currentLevel);
                solverTraitProgress.unspentSkillPoints += gained;
                Messages.Message($"{pawn.LabelShort} gained {gained} Solver skill point(s)!", MessageTypeDefOf.PositiveEvent, false);
                MD_DefOf.MD_ControlLevelUp.PlayOneShotOnCamera();
                lastSolverControlLevel = currentLevel;
            }
            else if (currentLevel < lastSolverControlLevel)
            {
                lastSolverControlLevel = currentLevel;
            }

            // --- Passive corruption drain and checks (cheap, runs every tick) ---
            if (!ext.isNerfedSolver)
            {
                SolverCorruptionUtil.DrainCorruption(pawn, 0.0001f);

                if (SolverCorruptionUtil.IsCorruptionHigh(pawn))
                {
                    // Optional: handle high corruption
                }
                if (SolverCorruptionUtil.IsCorruptionMaxed(pawn))
                {
                    // Optional: handle maxed corruption
                }
            }

            // --- Radiated temperature calculation (cheap, runs every tick) ---
            float radiatedTemp = 21f + (Heat * 0.5f);
        }

        public float OilRefuelThreshold = 0.5f; // Default to 50%
    }
}



