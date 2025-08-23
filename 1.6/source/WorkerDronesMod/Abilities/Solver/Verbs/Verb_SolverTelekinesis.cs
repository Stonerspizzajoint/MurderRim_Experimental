using RimWorld;
using Verse;
using Verse.AI;
using System.Linq;

namespace WorkerDronesMod
{
    public class Verb_SolverTelekinesis : Verb_CastAbility
    {
        private Comp_AbilityTelekinesisEffect TelekinesisComp
        {
            get
            {
                return this.Ability?.comps?.OfType<Comp_AbilityTelekinesisEffect>().FirstOrDefault();
            }
        }
        // Allow targeting pawns or haulable items as the first target
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            var telekinesisComp = TelekinesisComp;

            if (telekinesisComp == null)
                return false;

            if (!telekinesisComp.IsHoldingThing)
            {
                // Not holding anything: only allow haulable items (not pawns)
                if (target.Thing != null && target.Thing.def.EverHaulable)
                    return base.CanHitTargetFrom(root, target);
                return false;
            }
            else
            {
                // Holding something: allow any valid cell (location) or pawn
                if (target.Thing is Pawn)
                    return base.CanHitTargetFrom(root, target);

                // Accept any valid cell (even if no thing is present)
                if (target.Cell.InBounds(CasterPawn.Map) && target.Cell.Walkable(CasterPawn.Map))
                    return base.CanHitTargetFrom(root, target);

                return false;
            }
        }
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            var telekinesisComp = TelekinesisComp;

            if (telekinesisComp == null)
                return false;

            if (!telekinesisComp.IsHoldingThing)
            {
                // Not holding anything: only allow haulable items (not pawns, not cells)
                return target.Thing != null && target.Thing.def.EverHaulable;
            }
            else
            {
                // Holding something: allow pawns or valid cells
                if (target.Thing is Pawn)
                    return true;

                if (target.Cell.IsValid && target.Cell.InBounds(CasterPawn.Map) && target.Cell.Walkable(CasterPawn.Map))
                    return true;

                return false;
            }
        }
    }
}


