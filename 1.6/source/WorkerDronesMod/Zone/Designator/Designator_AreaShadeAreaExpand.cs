using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;


namespace WorkerDronesMod
{
    public class Designator_Cells_ShadeAreaExpand : Designator_Cells
    {
        public override bool DragDrawMeasurements => true;

        public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Areas;

        public Designator_Cells_ShadeAreaExpand()
        {
            defaultLabel = "MD.DesignateShadeArea_Label".Translate();
            defaultDesc = "MD.DesignateShadeArea_Desc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/DesignateShadeAreaExpand", false);
            hotKey = KeyBindingDefOf.Misc6;

            this.soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
            this.soundDragChanged = SoundDefOf.Designate_DragZone_Changed;
            this.soundSucceeded = SoundDefOf.Designate_ZoneAdd;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(Map))
                return "OutOfBounds".Translate();
            if (!c.Roofed(Map))
                return "MD.DesignateShadeArea_NotRoofed".Translate();
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

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            Area_Shade shadeArea = GetShadeArea();
            if (shadeArea == null)
            {
                shadeArea = new Area_Shade(Map.areaManager);
                Map.areaManager.AllAreas.Add(shadeArea);
            }
            foreach (var c in cells)
            {
                if (CanDesignateCell(c).Accepted)
                {
                    shadeArea[c] = true;
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





