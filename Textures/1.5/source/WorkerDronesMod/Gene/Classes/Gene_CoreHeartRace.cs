using Verse;

namespace WorkerDronesMod
{
    public class Gene_CoreHeartRace : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();

            // Safety check to ensure we have a valid pawn.
            if (pawn == null)
            {
                Log.Error("[Gene_CoreHeartRace] Pawn is null when applying gene.");
                return;
            }

            // Avoid applying the change more than once.Gene_CoreHeartRaceGene_CoreHeartRace
            if (pawn.def == MD_DefOf.MD_CoreHeartRace)
                return;

            // Look up the MD_CoreHeartRace definition from the database.
            ThingDef newRace = MD_DefOf.MD_CoreHeartRace;
            if (newRace != null)
            {
                // Change the pawn's race by assigning the new ThingDef.
                pawn.def = newRace;
                Log.Message($"[Gene_CoreHeartRace] Changed pawn {pawn.LabelCap} race to MD_CoreHeartRace.");
            }
            else
            {
                Log.Error("[Gene_CoreHeartRace] Could not find ThingDef 'MD_CoreHeartRace' in the DefDatabase.");
            }
        }
    }
}

