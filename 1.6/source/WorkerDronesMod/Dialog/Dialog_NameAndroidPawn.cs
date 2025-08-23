using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    public class Dialog_NameAndroidPawn : Dialog_NamePawn
    {
        public Dialog_NameAndroidPawn(Pawn pawn, List<Pawn> parents, NameFilter visibleNames, NameFilter editableNames, Dictionary<NameFilter, List<string>> suggestedNames, string initialFirstNameOverride = null, string initialNickNameOverride = null, string initialLastNameOverride = null, string initialTitleOverride = null)
            : base(pawn, visibleNames, editableNames, suggestedNames, initialFirstNameOverride, initialNickNameOverride, initialLastNameOverride, initialTitleOverride)
        {
            // Build a custom description string in the same style as vanilla
            TaggedString customDescription = BuildParentsDescription(parents);

            // Use reflection to set the private field 'descriptionText' in the base class
            var field = typeof(Dialog_NamePawn).GetField("descriptionText", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(this, customDescription);
            }
        }

        private TaggedString BuildParentsDescription(List<Pawn> parents)
        {
            if (parents == null || parents.Count == 0)
                return "No parents".Colorize(Color.gray);

            List<string> lines = new List<string>();
            foreach (var parent in parents)
            {
                if (parent == null) continue;
                string role;
                switch (parent.gender)
                {
                    case Gender.Female: role = "Mother".Translate().CapitalizeFirst(); break;
                    case Gender.Male: role = "Father".Translate().CapitalizeFirst(); break;
                    default: role = "Parent".Translate().CapitalizeFirst(); break;
                }
                lines.Add($"{role}: {DescribePawn(parent)}");
            }
            return string.Join("\n", lines);
        }

        private TaggedString DescribePawn(Pawn pawn)
        {
            if (pawn != null)
            {
                return pawn.FactionDesc(pawn.NameFullColored, false, pawn.NameFullColored, pawn.gender.GetLabel(pawn.RaceProps.Animal)).Resolve();
            }
            return "Unknown".Translate().Colorize(Color.gray);
        }
    }
}

