using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace WorkerDronesMod
{
    internal static class BabyAndroidUtil
    {

        /// <summary>
        /// Spawns a baby android pawn at the correct location based on parent or container.
        /// </summary>
        /// <param name="babyPawn">The baby pawn to spawn.</param>
        /// <param name="parentOrContainer">The parent pawn or container (e.g., vat).</param>
        /// <param name="positionOverride">Optional position to spawn at.</param>
        /// <returns>True if spawned or tracked successfully.</returns>
        public static bool TrySpawnAndroidBabyPawn(Pawn babyPawn, Thing parentOrContainer, IntVec3? positionOverride = null)
        {
            if (parentOrContainer.SpawnedOrAnyParentSpawned)
            {
                return GenSpawn.Spawn(babyPawn, positionOverride ?? parentOrContainer.PositionHeld, parentOrContainer.MapHeld, WipeMode.Vanish) != null;
            }

            Pawn parentPawn = parentOrContainer as Pawn;
            if (parentPawn != null)
            {
                if (parentPawn.IsCaravanMember())
                {
                    parentPawn.GetCaravan().AddPawn(babyPawn, true);
                    Find.WorldPawns.PassToWorld(babyPawn, PawnDiscardDecideMode.Decide);
                    return true;
                }
                if (parentPawn.IsWorldPawn())
                {
                    Find.WorldPawns.PassToWorld(babyPawn, PawnDiscardDecideMode.Decide);
                    return true;
                }
            }
            else if (parentOrContainer.ParentHolder != null)
            {
                Pawn_InventoryTracker invTracker = parentOrContainer.ParentHolder as Pawn_InventoryTracker;
                if (invTracker != null)
                {
                    if (invTracker.pawn.IsCaravanMember())
                    {
                        invTracker.pawn.GetCaravan().AddPawn(babyPawn, true);
                        Find.WorldPawns.PassToWorld(babyPawn, PawnDiscardDecideMode.Decide);
                        return true;
                    }
                    if (invTracker.pawn.IsWorldPawn())
                    {
                        Find.WorldPawns.PassToWorld(babyPawn, PawnDiscardDecideMode.Decide);
                        return true;
                    }
                }
            }
            return false;
        }

        public static Pawn CreateBabyPawnWithParents(Pawn parentA, Pawn parentB, List<GeneDef> inheritedGenes)
        {
            PawnKindDef babyKind = MD_DefOf.MD_PillBabyPawn;
            Faction faction = parentA?.Faction ?? parentB?.Faction ?? Faction.OfPlayer;

            Pawn babyPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind: babyKind,
                faction: faction,
                context: PawnGenerationContext.PlayerStarter,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: true,
                canGeneratePawnRelations: false
            ));

            // Remove all genes and reset xenotype using utility
            GeneInheritanceSimpleUtil.RemoveAllGenesAndResetXenotype(babyPawn);

            // Set age to 0
            babyPawn.ageTracker.AgeBiologicalTicks = 0;
            babyPawn.ageTracker.AgeChronologicalTicks = 0;

            // Set parent relationships
            if (parentA != null)
                babyPawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, parentA);
            if (parentB != null)
                babyPawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, parentB);

            // Apply fixed genes from mod extension
            ApplyFixedGenesToBaby(babyPawn);

            // Apply inherited skin color (blend 50/50)
            if (parentA != null && parentB != null)
            {
                GeneInheritanceSimpleUtil.ApplyInheritedSkinColor(
                    babyPawn,
                    parentA.story?.skinColorOverride ?? parentA.story?.SkinColor ?? UnityEngine.Color.white,
                    parentB.story?.skinColorOverride ?? parentB.story?.SkinColor ?? UnityEngine.Color.white,
                    0.5f
                );
            }

            // Mark as baby android in your memory component
            BabyAndroidGeneMemoryComponent.Instance.MarkAsBornAsBabyAndroid(babyPawn.thingIDNumber);

            // Send choice letter for naming
            var letter = LetterMaker.MakeLetter(
                "Baby Android Born".Translate(), // label
                "A new baby android has been born. You may choose a name for them.", // text
                MD_DefOf.BabyAndroidBirth, // your custom LetterDef
                babyPawn, // look target
                null, // related faction
                null, // quest
                null // questPart
            ) as ChoiceLetter_BabyAndroidBirth;

            if (letter != null)
            {
                letter.Init(babyPawn, parentA, parentB);
                Find.LetterStack.ReceiveLetter(letter);
            }



            return babyPawn;
        }

        public static void ApplyFixedGenesToBaby(Pawn pawn)
        {
            if (pawn?.genes == null)
                return;

            var ext = pawn.kindDef.GetModExtension<FixedAndroidBabyGenesExtension>();
            if (ext?.fixedGenes == null)
                return;

            foreach (var geneDef in ext.fixedGenes)
            {
                if (geneDef == null)
                {
                    Log.Error($"[WorkerDronesMod] Null GeneDef found in FixedAndroidBabyGenesExtension for {pawn.kindDef.defName}");
                    continue;
                }
                if (!pawn.genes.HasActiveGene(geneDef))
                {
                    try
                    {
                        pawn.genes.AddGene(geneDef, false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[WorkerDronesMod] Failed to add gene {geneDef.defName} to pawn {pawn.Name}: {ex}");
                    }
                }
            }
        }
        public static bool IsBabyAndroid(Pawn pawn)
        {
            return pawn?.def == MD_DefOf.MD_DroneBabyRace;
        }
    }
}
