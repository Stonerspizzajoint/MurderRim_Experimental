using Verse;
using RimWorld;

namespace WorkerDronesMod
{
    public class Gene_DroneHeadTypeSwitcher : Gene
    {
        private HeadTypeDef previousHeadType;

        public override void PostAdd()
        {
            base.PostAdd();

            var pawn = this.pawn;
            if (pawn?.story != null)
            {
                if (pawn.story.headType != MD_DefOf.MD_Drone_Head)
                {
                    previousHeadType = pawn.story.headType;
                }
                // Store previous head type
                previousHeadType = pawn.story.headType;

                // Apply drone head type
                if (MD_DefOf.MD_Drone_Head != null)
                {
                    pawn.story.headType = MD_DefOf.MD_Drone_Head;
                }
            }
        }

        public override void PostRemove()
        {
            base.PostRemove();

            var pawn = this.pawn;
            if (pawn?.story != null && previousHeadType != null)
            {
                // Restore previous head type
                pawn.story.headType = previousHeadType;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref previousHeadType, "previousHeadType");
        }
    }
}

