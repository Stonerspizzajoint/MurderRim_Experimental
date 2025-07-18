using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    public class Area_Shade : Area
    {
        public Area_Shade() { }

        public Area_Shade(AreaManager areaManager) : base(areaManager)
        {
        }

        // Display name of the zone.
        public override string Label
        {
            get { return "Shaded Shelter"; }
        }

        // Color used when drawing the zone on the map.
        public override Color Color
        {
            get { return new Color(0.2f, 0.8f, 0.2f); }
        }

        // Priority in the list (you can adjust this value if needed).
        public override int ListPriority
        {
            get { return 9000; }
        }

        // Determines whether this area can be assigned as the allowed area.
        public override bool AssignableAsAllowed()
        {
            return true;
        }

        // Unique load ID so that the area is saved and loaded correctly.
        public override string GetUniqueLoadID()
        {
            return "WorkerDronesMod.Area_Shade_" + this.ID;
        }

        // When a cell is set in the area, you could trigger notifications if needed.
        protected override void Set(IntVec3 c, bool val)
        {
            if (base[c] == val)
                return;
            base.Set(c, val);
            // Optionally, if you need to notify any systems when your zone changes, do it here.
            // For example, you might call:
            // base.Map.listerFilthInHomeArea.Notify_HomeAreaChanged(c);
        }
    }
}





