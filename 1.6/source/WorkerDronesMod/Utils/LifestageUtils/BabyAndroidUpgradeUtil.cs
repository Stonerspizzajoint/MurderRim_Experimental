using System.Collections.Generic;
using Verse;
using RimWorld;

namespace WorkerDronesMod
{
    public static class BabyAndroidUpgradeUtil
    {
        /// <summary>
        /// Upgrades a baby android pawn to a normal android pawn, inheriting genes from parents.
        /// </summary>
        /// <param name="babyPawn">The baby android pawn to upgrade.</param>
        /// <param name="parentA">First parent pawn.</param>
        /// <param name="parentB">Second parent pawn.</param>
        /// <param name="newKind">The PawnKindDef for the grown android.</param>
        /// <param name="newRace">The ThingDef for the grown android race.</param>
        public static void UpgradeToNormalAndroid(
            Pawn babyPawn,
            List<GeneDef> inheritedGenes,
            PawnKindDef newKind,
            ThingDef newRace)
        {
            if (babyPawn == null || inheritedGenes == null || newKind == null || newRace == null)
                return;

            // Cache the current skin color
            var previousColor = babyPawn.story?.skinColorOverride;

            // Change kind and race
            babyPawn.kindDef = newKind;
            babyPawn.def = newRace;

            // Ensure body property is set
            if (babyPawn.RaceProps != null && BodyDefOf.Human != null)
                babyPawn.RaceProps.body = BodyDefOf.Human;

            // Remove all genes and reset xenotype
            GeneInheritanceSimpleUtil.RemoveAllGenesAndResetXenotype(babyPawn);

            // Apply inherited genes
            GeneInheritanceSimpleUtil.ApplyAssignedGenesToPawn(babyPawn, inheritedGenes);

            // Restore previous skin color
            if (babyPawn.story != null && previousColor != null)
                babyPawn.story.skinColorOverride = previousColor;

            // Apply colony birthed android backstory
            if (babyPawn.story != null && MD_DefOf.ColonyBirthedAndroid != null)
                babyPawn.story.Childhood = MD_DefOf.ColonyBirthedAndroid;

            // Assign ideoligion if Ideology is active and pawn is humanlike
            if (ModsConfig.IdeologyActive && babyPawn.RaceProps.Humanlike && babyPawn.ideo != null && babyPawn.ideo.Ideo == null)
            {
                Ideo playerIdeo = Faction.OfPlayer.ideos?.PrimaryIdeo;
                if (playerIdeo != null)
                {
                    babyPawn.ideo.SetIdeo(playerIdeo);
                }
                else if (Find.IdeoManager.IdeosListForReading.Count > 0)
                {
                    babyPawn.ideo.SetIdeo(Find.IdeoManager.IdeosListForReading[0]);
                }
            }

            // Attempt to rebuild verb list
            babyPawn.verbTracker?.InitVerbsFromZero();
        }
    }
}

