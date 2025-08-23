using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public enum SolverCategory
    {
        Absolute,
        Mutation,
        Corruption
    }

    public class SolverTraitDef : Def
    {
        // UI/Window properties
        public string label;
        public string description;
        public string iconPath;
        public string highlightColor;
        public string tooltipExtra;
        public SolverCategory solverCategory = SolverCategory.Absolute;
        public SoundDef unlockSound;
        public float defaultX;
        public float defaultY;
        public float size = 1f; // Node size in grid units (square, supports fractions)
        public string color;
        public int tierLevel = 1; // Tier or row in the skill tree

        // Gameplay properties
        public AbilityDef GivenAbility;
        public HediffDef GivenHediff;
        public List<StatModifier> statOffsets;
        public List<StatModifier> statFactors;

        // Dependencies
        public List<SolverTraitDef> requiredSolverTraits;
        public bool requireOnlyOneTrait = false;

        // Skill point cost to unlock
        public int skillPointCost = 1;

        // Minimum tier required to unlock this trait
        public int requiredTierLevel = 0;

        // Should this trait be unlocked by default?
        public bool DefaultUnlocked = false;

        // Is this trait a core part for its tier?
        public bool CoreModule = false;

        // Should the icon glow in the UI?
        public bool GlowingIcon = false;

    }
}


