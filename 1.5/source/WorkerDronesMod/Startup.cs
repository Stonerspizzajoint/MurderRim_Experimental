using System.Collections.Generic;
using System.Linq;
using Verse;
using WorkerDronesMod;

[StaticConstructorOnStartup]
public static class Startup
{
    // Token: 0x060002BB RID: 699 RVA: 0x00011470 File Offset: 0x0000F670
    static Startup()
    {
        foreach (ThingDef thingDef in Enumerable.Where<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading, (ThingDef x) => x.race != null && x.race.Humanlike))
        {
            ThingDef thingDef3;
            ThingDef thingDef2 = thingDef3 = thingDef;
            if (thingDef3.recipes == null)
            {
                thingDef3.recipes = new List<RecipeDef>();
            }
            thingDef2.recipes.Add(MD_DefOf.MD_ExtractNeutroamine);
        }
    }
}
