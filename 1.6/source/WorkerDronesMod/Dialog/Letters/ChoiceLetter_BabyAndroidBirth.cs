using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace WorkerDronesMod
{
    public class ChoiceLetter_BabyAndroidBirth : ChoiceLetter
    {
        private Pawn babyPawn;
        private Pawn parentA;
        private Pawn parentB;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                yield return new DiaOption("Name Baby Android".Translate().CapitalizeFirst())
                {
                    action = () =>
                    {
                        // Get parent's last name for default
                        string defaultLastName = null;
                        if (parentA?.Name is NameTriple nameA && !string.IsNullOrEmpty(nameA.Last))
                            defaultLastName = nameA.Last;
                        else if (parentB?.Name is NameTriple nameB && !string.IsNullOrEmpty(nameB.Last))
                            defaultLastName = nameB.Last;

                        Find.WindowStack.Add(new Dialog_NameAndroidPawn(
                            babyPawn,
                            new List<Pawn> { parentA, parentB },
                            NameFilter.First | NameFilter.Nick | NameFilter.Last | NameFilter.Title,
                            NameFilter.First | NameFilter.Nick | NameFilter.Last | NameFilter.Title,
                            null,
                            null,
                            null,
                            defaultLastName
                        ));
                    },
                    resolveTree = true
                };

                yield return base.Option_Close;
            }
        }

        // Only set references and lookTargets here
        public void Init(Pawn babyPawn, Pawn parentA, Pawn parentB)
        {
            this.babyPawn = babyPawn;
            this.parentA = parentA;
            this.parentB = parentB;
            this.lookTargets = new LookTargets(babyPawn);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref babyPawn, "babyPawn");
            Scribe_References.Look(ref parentA, "parentA");
            Scribe_References.Look(ref parentB, "parentB");
        }
    }
}

