using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    [DefOf]
    public static class Regen_DefOf
    {
        public static HediffDef MD_RoboticReconstruction;

        static Regen_DefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(Regen_DefOf));
    }
}

