using Verse;

namespace WorkerDronesMod
{
    // The component that forces the pawn to drop its weapon.
    public class HediffComp_ForceDropWeapon : HediffComp
    {
        // Shortcut to conveniently access custom properties.
        public HediffCompProperties_ForceDropWeapon Props => (HediffCompProperties_ForceDropWeapon)this.props;

        // Called every tick. We check periodically (every 'checkIntervalTicks') if the pawn holds a weapon.
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            // Use the global tick counter for timing.
            if (Find.TickManager.TicksGame % Props.checkIntervalTicks == 0)
            {
                Pawn pawn = this.parent.pawn;
                if (pawn != null && pawn.equipment != null && pawn.equipment.Primary != null)
                {
                    // Attempt to force the pawn to drop its primary weapon.
                    pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out ThingWithComps droppedWeapon, pawn.Position);
                }
            }
        }
    }
}

