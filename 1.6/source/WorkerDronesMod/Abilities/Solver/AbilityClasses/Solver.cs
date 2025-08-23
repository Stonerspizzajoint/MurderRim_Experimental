using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class Solver : Ability
    {
        public Solver() : base() { }
        public Solver(Pawn pawn) : base(pawn) { }
        public Solver(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public bool HasCooldownTicksRange
        {
            get
            {
                return def != null && def.cooldownTicksRange != default(IntRange);
            }
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
                if (gene != null && HeatUtil.IsOverheating(gene.Heat, gene.InitialResourceMax))
                {
                    return "CannotCastSolverAbility_Overheating".Translate();
                }
                return base.CanCast;
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target)
        {
            var gene = pawn.genes?.GetFirstGeneOfType<Gene_BasicSolver>();
            if (gene != null && HeatUtil.IsOverheating(gene.Heat, gene.InitialResourceMax))
            {
                return false;
            }
            return base.CanApplyOn(target);
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            bool result = base.Activate(target, dest);

            // Calculate custom cooldown
            int baseCooldown = def?.cooldownTicksRange.TrueMin ?? 600;
            float multiplier = 1f;
            if (pawn != null)
            {
                multiplier = pawn.GetStatValue(MD_DefOf.MD_AbilityCooldownMultiplier, true);
            }
            int customCooldown = (int)(baseCooldown * multiplier);

            // Start custom cooldown
            this.StartCooldown(customCooldown);

            return result;
        }
    }
}

