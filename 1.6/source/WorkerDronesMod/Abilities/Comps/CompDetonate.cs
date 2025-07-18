using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    internal class CompExplode : CompAbilityEffect
    {
        public new CompProperties_Explode Props
        {
            get
            {
                return (CompProperties_Explode)this.props;
            }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            this.Detonate();
        }

        public void Detonate()
        {
            List<Thing> list = new List<Thing>();
            bool flag = !this.Props.damageUser;
            if (flag)
            {
                list.Add(this.parent.pawn);
            }
            GenExplosion.DoExplosion(this.parent.pawn.Position, this.parent.pawn.Map, this.Props.radius, this.Props.damageType, this.parent.pawn, this.Props.damageAmount, this.Props.damagePenetration, this.Props.soundCreated, null, null, null);
        }
    }
}
