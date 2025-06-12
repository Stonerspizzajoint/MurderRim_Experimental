using System.Collections.Generic;
using Verse;

namespace WorkerDronesMod
{
    public static class SurgerySafetyUtility
    {
        private static readonly HashSet<Pawn> safeStomachRemoval = new HashSet<Pawn>();

        public static void MarkStomachRemoved(Pawn pawn)
            => safeStomachRemoval.Add(pawn);

        public static bool HasSafeStomachRemoval(Pawn pawn)
            => pawn != null && safeStomachRemoval.Contains(pawn);

        public static void ClearSafeStomachRemoval(Pawn pawn)
            => safeStomachRemoval.Remove(pawn);
    }

}

