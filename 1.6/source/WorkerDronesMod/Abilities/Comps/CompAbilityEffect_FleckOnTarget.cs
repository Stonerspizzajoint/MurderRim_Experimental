using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WorkerDronesMod
{
    public class CompAbilityEffect_FleckOnTarget : CompAbilityEffect
    {
        public new CompProperties_AbilityFleckOnTarget Props => (CompProperties_AbilityFleckOnTarget)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (Props.preCastTicks <= 0)
            {
                SpawnAllOnTarget(target);
            }
        }

        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            if (Props.preCastTicks > 0)
            {
                yield return new PreCastAction
                {
                    action = delegate (LocalTargetInfo t, LocalTargetInfo d)
                    {
                        SpawnAllOnTarget(t);
                    },
                    ticksAwayFromCast = Props.preCastTicks
                };
            }
        }

        private void SpawnAllOnTarget(LocalTargetInfo target)
        {
            if (Props.fleckDefs != null && Props.fleckDefs.Count > 0)
            {
                foreach (var def in Props.fleckDefs)
                {
                    SpawnFleckOnTarget(def, target);
                }
            }
            else if (Props.fleckDef != null)
            {
                SpawnFleckOnTarget(Props.fleckDef, target);
            }
        }

        private void SpawnFleckOnTarget(FleckDef def, LocalTargetInfo target)
        {
            Color? color = null;
            if (Props.UseSkinColor && parent.pawn?.story != null)
            {
                color = parent.pawn.story.SkinColor;
            }

            Map map = parent.pawn?.Map;
            if (map == null) return;

            Vector3 pos;
            if (target.HasThing && target.Thing.Spawned)
            {
                pos = target.Thing.DrawPos;
            }
            else
            {
                pos = target.Cell.ToVector3Shifted();
            }

            Log.Message($"[MD] Spawning fleck {def?.defName} at {pos} for target {target}");

            FleckCreationData data = FleckMaker.GetDataStatic(pos, map, def, Props.scale);
            if (color.HasValue)
                data.instanceColor = color.Value;

            map.flecks.CreateFleck(data);
        }
    }
}

