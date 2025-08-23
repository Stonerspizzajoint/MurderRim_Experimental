using RimWorld;
using Verse;
using Verse.AI.Group;

namespace WorkerDronesMod
{
    public class DeathActionWorker_SpawnCoreHeart : DeathActionWorker
    {
        public override void PawnDied(Corpse corpse, Lord prevLord)
        {
            if (corpse != null && corpse.Spawned && corpse.Map != null)
            {
                CoreHeartUtils.SpawnCoreHeartCopyFromCorpse(corpse);
            }
        }
    }
}

