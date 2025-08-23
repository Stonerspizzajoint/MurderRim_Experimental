using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WorkerDronesMod
{
    public class CompAbilityEffect_FleckOnCaster : CompAbilityEffect
    {
        public new CompProperties_AbilityFleckOnCaster Props => (CompProperties_AbilityFleckOnCaster)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (Props.preCastTicks <= 0)
            {
                // Optionally play a sound here if you add a sound property
                SpawnAllOnCaster();
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
                        SpawnAllOnCaster();
                    },
                    ticksAwayFromCast = Props.preCastTicks
                };
            }
        }

        private void SpawnAllOnCaster()
        {
            Pawn caster = this.parent.pawn;
            if (Props.fleckDefs != null && Props.fleckDefs.Count > 0)
            {
                foreach (var def in Props.fleckDefs)
                {
                    SpawnFleckOnCaster(def, caster);
                }
            }
            else if (Props.fleckDef != null)
            {
                SpawnFleckOnCaster(Props.fleckDef, caster);
            }
        }

        private void SpawnFleckOnCaster(FleckDef def, Pawn caster)
        {
            Color? color = null;
            if (Props.UseSkinColor && caster?.story != null)
            {
                color = caster.story.SkinColor;
            }

            Map map = caster?.Map;
            if (caster != null && map != null)
            {
                Vector3 pos = caster.Position.ToVector3Shifted();
                Log.Message($"[MD] Spawning fleck {def?.defName} at {pos} for {caster.LabelShortCap}");

                // Use FleckMaker.GetDataStatic to create a colored fleck
                FleckCreationData data = FleckMaker.GetDataStatic(pos, map, def, Props.scale);
                if (color.HasValue)
                    data.instanceColor = color.Value;

                map.flecks.CreateFleck(data);
            }
            else
            {
                Log.Warning("[MD] Caster or map is null, cannot spawn fleck.");
            }
        }
    }
}
