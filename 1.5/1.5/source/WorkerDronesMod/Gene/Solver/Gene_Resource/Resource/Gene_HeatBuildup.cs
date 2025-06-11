using RimWorld;
using UnityEngine;

namespace WorkerDronesMod
{
    public class Gene_HeatBuildup : Gene_Resource
    {
        public override float InitialResourceMax => 100f;
        protected override Color BarColor => Color.red;
        protected override Color BarHighlightColor => Color.white;
        public override float MinLevelForAlert => 0f;

        public override void Tick()
        {
            base.Tick();  // Heat only changes through explicit methods
        }

        public override void Reset()
        {
            base.Reset();
            this.Value = 0f;
        }

        public override int PostProcessValue(float value) => Mathf.RoundToInt(value);

        public void IncreaseHeat(float amount)
        {
            this.Value = Mathf.Min(InitialResourceMax, this.Value + amount);
        }

        public void ReduceHeat(float amount)
        {
            this.Value = Mathf.Max(0f, this.Value - amount);
        }
    }
}












