using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    public class Designator_Cells_ShadeAreaExpand : Designator_Cells
    {
        public Designator_Cells_ShadeAreaExpand()
        {
            defaultLabel = "Designate Shade Area";
            defaultDesc = "Designate an area as shade (this area will be used to restrict pawn movement indoors).";
            icon = ContentFinder<Texture2D>.Get("UI/Designators/DesignateShadeAreaExpand", false);
            hotKey = KeyBindingDefOf.Misc6; // Adjust as needed.

            this.soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
            this.soundDragChanged = SoundDefOf.Designate_DragZone_Changed;
            this.soundSucceeded = SoundDefOf.Designate_ZoneAdd;
        }

        public override int DraggableDimensions => 2;

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(Map))
                return "Out of bounds.";
            // Require the cell to be roofed (optional).
            if (!c.Roofed(Map))
                return "Cell is not roofed.";
            return true;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            Area_Shade shadeArea = GetShadeArea();
            if (shadeArea == null)
            {
                shadeArea = new Area_Shade(Map.areaManager);
                Map.areaManager.AllAreas.Add(shadeArea);
            }
            shadeArea[c] = true;
        }

        public override void SelectedUpdate()
        {
            // When selected, show the zone overlay.
            if (Map != null)
            {
                Area_Shade shadeArea = GetShadeArea();
                if (shadeArea != null)
                {
                    shadeArea.MarkForDraw();
                }
            }
        }

        private Area_Shade GetShadeArea()
        {
            return Map.areaManager.AllAreas.OfType<Area_Shade>().FirstOrDefault();
        }
    }
}





