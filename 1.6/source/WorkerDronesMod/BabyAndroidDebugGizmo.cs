using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using VREAndroids;

namespace WorkerDronesMod
{
    public static class BabyAndroidDebugGizmo
    {
        public static IEnumerable<Gizmo> GetTestGizmos(Pawn pawn)
        {
            if (!Prefs.DevMode || !DebugSettings.godMode)
                yield break;

            // Gizmos for non-baby androids
            if (pawn.IsAndroid() && !BabyAndroidUtil.IsBabyAndroid(pawn))
            {
                yield return new Command_Action
                {
                    defaultLabel = "Test Gene Inheritance",
                    defaultDesc = "Tests gene inheritance with the closest other android.",
                    action = () =>
                    {
                        Pawn otherParent = FindClosestOtherAndroid(pawn);
                        if (otherParent == null)
                        {
                            Messages.Message("No other android found.", MessageTypeDefOf.RejectInput);
                            return;
                        }

                        var inheritedGenes = GeneInheritanceSimpleUtil.GetInheritedGenes(pawn, otherParent);
                        string geneList = string.Join(", ", inheritedGenes.Select(g => g.label));
                        Log.Message($"Inherited genes for child of {pawn.Name} and {otherParent.Name}: {geneList}");
                        Messages.Message("Gene inheritance tested. See log for results.", MessageTypeDefOf.TaskCompletion);
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = "Spawn Baby Android",
                    defaultDesc = "Spawns a baby android pawn with this pawn and the closest other android as parents.",
                    action = () =>
                    {
                        Pawn otherParent = FindClosestOtherAndroid(pawn);
                        if (otherParent == null)
                        {
                            Messages.Message("No other android found.", MessageTypeDefOf.RejectInput);
                            return;
                        }

                        var inheritedGenes = GeneInheritanceSimpleUtil.GetInheritedGenes(pawn, otherParent);
                        Pawn babyPawn = BabyAndroidUtil.CreateBabyPawnWithParents(pawn, otherParent, inheritedGenes);

                        // Save the gene list for this baby pawn
                        var geneSet = new InheritedGeneSet();
                        foreach (var gene in inheritedGenes)
                            geneSet.AddGene(gene);
                        BabyAndroidGeneMemoryComponent.Instance.SetGeneMemory(babyPawn.thingIDNumber, geneSet);

                        bool spawned = BabyAndroidUtil.TrySpawnAndroidBabyPawn(babyPawn, pawn, pawn.Position);
                        if (spawned)
                        {
                            Messages.Message($"Spawned baby android at {pawn.Position} with parents {pawn.Name} and {otherParent.Name}.", MessageTypeDefOf.TaskCompletion);
                        }
                        else
                        {
                            Messages.Message("Failed to spawn baby android.", MessageTypeDefOf.RejectInput);
                        }
                    }
                };
            }

            // Gizmo for baby androids only
            if (pawn.IsAndroid() && BabyAndroidUtil.IsBabyAndroid(pawn))
            {
                yield return new Command_Action
                {
                    defaultLabel = "Upgrade Baby Android",
                    defaultDesc = "Upgrades this baby android to a normal android pawn, using the originally inherited genes.",
                    action = () =>
                    {
                        var geneSet = BabyAndroidGeneMemoryComponent.Instance.GetGeneSet(pawn.thingIDNumber);
                        if (geneSet == null || geneSet.Genes == null || geneSet.Genes.Count == 0)
                        {
                            Messages.Message("No gene memory found for this baby android.", MessageTypeDefOf.RejectInput);
                            return;
                        }

                        var normalKind = MD_DefOf.VREA_AndroidAwakened;
                        var normalRace = ThingDefOf.Human;

                        BabyAndroidUpgradeUtil.UpgradeToNormalAndroid(
                            pawn,
                            geneSet.Genes.ToList(),
                            normalKind,
                            normalRace
                        );

                        Messages.Message($"Upgraded baby android {pawn.Name} to normal android.", MessageTypeDefOf.TaskCompletion);
                    }
                };
            }
        }

        private static Pawn FindClosestOtherAndroid(Pawn exclude)
        {
            Map map = exclude.Map;
            return map?.mapPawns?.AllPawnsSpawned
                .Where(p => p != exclude && p.IsAndroid())
                .OrderBy(p => p.Position.DistanceToSquared(exclude.Position))
                .FirstOrDefault();
        }
    }
}

