using Verse;
using UnityEngine;

namespace WorkerDronesMod
{
    public class SubEffecter_BubbleDirectional : SubEffecter_DrifterEmoteChance
    {
        public SubEffecter_BubbleDirectional(SubEffecterDef def, Effecter parent) : base(def, parent)
        {
        }

        public override void SubEffectTick(TargetInfo A, TargetInfo B)
        {
            if (Rand.Chance(this.def.chancePerTick))
            {
                Pawn pawn = A.Thing as Pawn;
                if (pawn != null)
                {
                    int burst = this.def.burstCount.RandomInRange;
                    for (int i = 0; i < burst; i++)
                    {
                        Vector3 facing = pawn.Rotation.FacingCell.ToVector3();
                        facing.y = 0f;

                        ThingDef moteDef = this.def.moteDef;
                        if (moteDef != null)
                        {
                            MoteThrown mote = (MoteThrown)ThingMaker.MakeThing(moteDef);
                            mote.Scale = Rand.Range(this.def.scale.min, this.def.scale.max);
                            Vector3 headOffset = new Vector3(0f, 1.2f, 0f);
                            mote.exactPosition = pawn.DrawPos + headOffset + facing * 0.4f;

                            float angleSpread = Rand.Range(-20f, 20f);
                            float finalAngle = pawn.Rotation.AsAngle + angleSpread;

                            mote.SetVelocity(finalAngle, Rand.Range(this.def.speed.min, this.def.speed.max));
                            mote.rotationRate = Rand.Range(this.def.rotationRate.min, this.def.rotationRate.max);
                            GenSpawn.Spawn(mote, mote.exactPosition.ToIntVec3(), pawn.Map);
                        }
                    }
                }
                else
                {
                    base.SubEffectTick(A, B);
                }
            }
        }
    }
}

