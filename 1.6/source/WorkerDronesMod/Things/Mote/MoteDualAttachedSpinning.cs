using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    public class MoteDualAttachedSpinning : MoteDualAttached
    {
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (this.Destroyed)
                return;

            // Increment rotation based on rotationRate and age
            float angle = (this.AgeSecs * rotationRate) % 360f;
            this.exactRotation = angle;

            base.DrawAt(drawLoc, flip);
        }
    }
}

