using Verse;

namespace WorkerDronesMod
{
    // The hediff comp that gives the pawn a weapon and monitors it.
    public class HediffComp_GunHand : HediffComp
    {
        // Convenience property for our custom properties.
        public HediffCompProperties_GunHand Props => (HediffCompProperties_GunHand)props;

        // Store the spawned weapon instance.
        internal ThingWithComps gunHandWeapon;

        // When the hediff is applied, create and equip the weapon.
        public override void CompPostMake()
        {
            base.CompPostMake();
            Pawn pawn = parent.pawn;
            if (pawn.equipment != null && Props.weaponDef != null)
            {
                gunHandWeapon = ThingMaker.MakeThing(Props.weaponDef) as ThingWithComps;
                if (gunHandWeapon != null)
                {
                    pawn.equipment.AddEquipment(gunHandWeapon);
                }
            }
        }

        // Every tick, check if the pawn's primary weapon is still the one added by the hediff.
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = parent.pawn;
            if (pawn != null && gunHandWeapon != null)
            {
                // If the pawn's primary equipment is not our special weapon...
                if (pawn.equipment.Primary != gunHandWeapon)
                {
                    // Optionally, if our weapon is still spawned somewhere, destroy it.
                    if (gunHandWeapon.Spawned)
                    {
                        gunHandWeapon.Destroy();
                    }
                    // Remove the hediff (which will trigger our cleanup).
                    pawn.health.RemoveHediff(parent);
                }
            }
        }

        // Cleanup when the hediff is removed.
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            Pawn pawn = parent.pawn;
            if (pawn.equipment != null && gunHandWeapon != null)
            {
                // Only remove our weapon if it is present.
                if (pawn.equipment.AllEquipmentListForReading.Contains(gunHandWeapon))
                {
                    pawn.equipment.Remove(gunHandWeapon);
                }
                if (gunHandWeapon.Spawned)
                {
                    gunHandWeapon.Destroy();
                }
                gunHandWeapon = null;
            }
        }

        // On pawn death, remove the hediff so that the weapon is also cleaned up.
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (parent?.pawn != null)
            {
                parent.pawn.health.RemoveHediff(parent);
            }
        }
    }
}
