using Verse;

namespace WorkerDronesMod
{
    public class Verb_ShootBeam_Heat : Verb_ShootBeam
    {
        // Multiplier to adjust how much heat is added per tick.
        private const float gradualHeatMultiplier = 0.1f;

        public override void BurstingTick()
        {
            // Call the original behavior.
            base.BurstingTick();

            // Gradually add heat each tick while the burst is active.
            Pawn shooter = caster as Pawn;
            if (shooter != null && shooter.equipment != null)
            {
                foreach (ThingWithComps weapon in shooter.equipment.AllEquipmentListForReading)
                {
                    CompHeatPerShot comp = weapon.TryGetComp<CompHeatPerShot>();
                    if (comp != null)
                    {
                        // Calculate the per-tick heat increment.
                        float heatIncrement = comp.Props.heatPerShot * gradualHeatMultiplier;
                        comp.AddHeatOnShotGradual(heatIncrement);
                    }
                }
            }
        }
    }
}



