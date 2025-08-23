using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(StartingPawnUtility), nameof(StartingPawnUtility.DrawSkillSummaries))]
    public static class Patch_StartingPawnUtility_DrawSkillSummaries
    {
        // Cache the private static field for SkillsPerColumn
        private static readonly FieldInfo skillsPerColumnField = typeof(StartingPawnUtility).GetField("SkillsPerColumn", BindingFlags.NonPublic | BindingFlags.Static);

        // Cache the private static method for FindBestSkillOwner
        private static readonly MethodInfo findBestSkillOwnerMethod = typeof(StartingPawnUtility).GetMethod("FindBestSkillOwner", BindingFlags.NonPublic | BindingFlags.Static);

        static bool Prefix(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            rect = rect.ContractedBy(10f);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.min, new Vector2(rect.width, 45f)), "TeamSkills".Translate());
            Text.Font = GameFont.Small;
            rect.yMin += 45f;
            rect = rect.LeftPart(0.25f);
            rect.height = 27f;
            rect.y -= 4f;

            // Filter out SolverControl
            List<SkillDef> allDefsListForReading = DefDatabase<SkillDef>.AllDefsListForReading;
            List<SkillDef> filteredSkills = new List<SkillDef>();
            foreach (var skillDef in allDefsListForReading)
            {
                if (skillDef == MD_DefOf.SolverControl)
                    continue;
                filteredSkills.Add(skillDef);
            }

            // Calculate skills per column as vanilla does
            int skillsPerColumn = (int)skillsPerColumnField.GetValue(null);
            if (skillsPerColumn < 0)
            {
                int count = 0;
                foreach (var sd in filteredSkills)
                    if (sd.pawnCreatorSummaryVisible)
                        count++;
                skillsPerColumn = Mathf.CeilToInt(count / 4f);
                skillsPerColumnField.SetValue(null, skillsPerColumn);
            }

            int num = 0;
            for (int i = 0; i < filteredSkills.Count; i++)
            {
                SkillDef skillDef = filteredSkills[i];
                if (skillDef.pawnCreatorSummaryVisible)
                {
                    Rect r = rect;
                    r.x = rect.x + r.width * (float)(num / skillsPerColumn);
                    r.y = rect.y + r.height * (float)(num % skillsPerColumn);
                    r.height = 24f;
                    r.width -= 4f;
                    Pawn pawn = (Pawn)findBestSkillOwnerMethod.Invoke(null, new object[] { skillDef });
                    SkillUI.DrawSkill(pawn.skills.GetSkill(skillDef), r.Rounded(), SkillUI.SkillDrawMode.Menu, pawn.Name.ToString().Colorize(ColoredText.TipSectionTitleColor));
                    num++;
                }
            }
            return false; // Skip original
        }
    }
}


