using System.Collections.Generic;
using Verse;

namespace WorkerDronesMod
{
    /// <summary>
    /// Tracks progress of traits and skill points for a pawn.
    /// </summary>
    /// <remarks>
    /// This class is used to store which traits are unlocked and how many skill points are available.
    /// It is exposed for saving/loading game state.
    /// </remarks>
    [StaticConstructorOnStartup]
    public class SolverTraitProgress : IExposable
    {
        public HashSet<string> unlockedTraits = new HashSet<string>();
        public int unspentSkillPoints = 0;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref unlockedTraits, "unlockedTraits", LookMode.Value);
            Scribe_Values.Look(ref unspentSkillPoints, "unspentSkillPoints", 0);
        }
    }
}

