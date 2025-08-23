using System;
using RimWorld;
using Verse;
using UnityEngine;

namespace WorkerDronesMod
{
    public class CompAbilityEffect_StunWithCustomMote : CompAbilityEffect_WithDuration
    {
        public CompProperties_AbilityEffectStunWithCustomMote Props => (CompProperties_AbilityEffectStunWithCustomMote)props;
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (target.HasThing)
            {
                base.Apply(target, dest);
                Pawn pawn = target.Thing as Pawn;
                if (pawn != null)
                {
                    // Suppress battle log and default stun mote
                    int stunTicks = base.GetDurationSeconds(pawn).SecondsToTicks();
                    pawn.stances.stunner.StunFor(
                        stunTicks,
                        this.parent.pawn,
                        addBattleLog: false,
                        showMote: false
                    );

                    // Spawn custom stun mote attached to the pawn
                    if (Props.customStunMoteDef != null)
                    {
                        var mote = MoteMaker.MakeAttachedOverlay(
                            pawn,
                            Props.customStunMoteDef,
                            Vector3.zero,
                            1f,
                            -1f
                        );
                        if (mote != null)
                        {
                            // Set the mote color to the caster's skin color
                            Color skinColor = parent.pawn?.story?.SkinColor ?? Color.white;
                            mote.instanceColor = skinColor;

                            Log.Message($"Spawned mote: {mote.GetType().Name}, rotationRate before: {mote.rotationRate}");
                            mote.solidTimeOverride = stunTicks / 60f;
                            mote.rotationRate = Props.stunMoteSpinSpeed;
                            Log.Message($"rotationRate after: {mote.rotationRate}");
                        }
                    }
                }
            }
        }
    }
}
