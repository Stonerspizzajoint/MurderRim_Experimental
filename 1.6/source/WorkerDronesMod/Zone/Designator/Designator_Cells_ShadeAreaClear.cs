using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;


namespace WorkerDronesMod
{
    public class Designator_Cells_ShadeAreaClear : Designator_Cells
    {
        public override bool DragDrawMeasurements => true;

        public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Areas;

        public Designator_Cells_ShadeAreaClear()
        {
            defaultLabel = "Clear Shade Area";
            defaultDesc = "Clear cells from the designated shade area.";
            icon = ContentFinder<Texture2D>.Get("UI/Designators/DesignateShadeAreaClear", false);
            hotKey = KeyBindingDefOf.Misc7; // Adjust as needed.

            this.soundDragSustain = SoundDefOf.Designate_DragAreaDelete;
            this.soundDragChanged = null;
            this.soundSucceeded = SoundDefOf.Designate_ZoneDelete;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(Map))
                return "Out of bounds.";
            return true;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            Area_Shade shadeArea = GetShadeArea();
            if (shadeArea != null)
            {
                shadeArea[c] = false;
            }
        }

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            Area_Shade shadeArea = GetShadeArea();
            if (shadeArea != null)
            {
                foreach (var c in cells)
                {
                    if (CanDesignateCell(c).Accepted)
                    {
                        shadeArea[c] = false;
                    }
                }
            }
        }

        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
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



