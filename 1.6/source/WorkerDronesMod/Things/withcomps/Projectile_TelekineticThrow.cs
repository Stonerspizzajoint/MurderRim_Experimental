using RimWorld;
using Verse;
using UnityEngine;

namespace WorkerDronesMod
{
    public class Projectile_TelekineticThrow : Bullet
    {
        public Thing thrownThing;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref thrownThing, "thrownThing");
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = this.Map;
            IntVec3 cell = this.Position;

            // Always drop the thrown item at or near the impact cell
            if (thrownThing != null)
            {
                // If the item is already spawned, despawn it first to avoid double-spawn errors
                if (thrownThing.Spawned)
                {
                    thrownThing.DeSpawn();
                }

                // Try to place the item at the impact cell; RimWorld will automatically find a nearby cell if blocked
                GenPlace.TryPlaceThing(thrownThing, cell, map, ThingPlaceMode.Near);

                TelekinesisUtil.ApplyTelekineticImpact(thrownThing, cell, map);
            }

            // Call base to handle sound, motes, etc.
            base.Impact(hitThing, blockedByShield);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (thrownThing != null)
            {
                float angle = this.ExactRotation.eulerAngles.y;

                // For pawns, draw with apparel/armor
                if (thrownThing is Pawn pawn)
                {
                    pawn.Drawer.renderer.RenderPawnAt(drawLoc, Rot4.North);
                    return;
                }
                // For corpses, draw the inner pawn with apparel/armor
                else if (thrownThing is Corpse corpse && corpse.InnerPawn != null)
                {
                    corpse.InnerPawn.Drawer.renderer.RenderPawnAt(drawLoc, Rot4.North);
                    return;
                }
                // For items, use their graphic
                else
                {
                    Graphic graphic = thrownThing.Graphic;
                    if (graphic != null)
                    {
                        graphic.Draw(drawLoc, Rot4.North, thrownThing, angle);
                        return;
                    }
                }
            }

            // Fallback to default projectile drawing
            base.DrawAt(drawLoc, flip);
        }
    }
}


