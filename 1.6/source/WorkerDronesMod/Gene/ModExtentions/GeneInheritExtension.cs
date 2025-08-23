using System.Collections.Generic;
using Verse;

namespace WorkerDronesMod
{
    public class GeneInheritExtension : DefModExtension
    {
        public bool AlwaysInherit = false; // Always inherit this gene if the parent has it.
        public float InheritChance = 1.0f; // Chance to inherit this gene (0 = never, 1 = always).
        public bool CannotInherit = false; // Prevent this gene from being inherited.
        public float? FavoredParentChance = null; // Base chance (0-1) for this pawn to be selected as favored parent. If null, use default.
    }
}