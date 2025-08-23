using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(SkillUI), nameof(SkillUI.DrawSkillsOf))]
    public static class Patch_SkillUI_DrawSkillsOf_AndroidBaby
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn p, Vector2 offset, SkillUI.SkillDrawMode mode, Rect container)
        {
            if (!p.DevelopmentalStage.Baby())
                return true;

            if (!BabyAndroidUtil.IsBabyAndroid(p))
                return true;

            Text.Font = GameFont.Small;
            var allDefsListForReading = DefDatabase<SkillDef>.AllDefsListForReading;

            // Reflection for private static fields
            var levelLabelWidthField = typeof(SkillUI).GetField("levelLabelWidth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var skillDefsField = typeof(SkillUI).GetField("skillDefsInListOrderCached", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            float levelLabelWidth = (float)levelLabelWidthField.GetValue(null);
            var skillDefsInListOrderCached = (List<SkillDef>)skillDefsField.GetValue(null);

            if (skillDefsInListOrderCached == null)
                return false;

            for (int i = 0; i < allDefsListForReading.Count; i++)
            {
                float x = Text.CalcSize(allDefsListForReading[i].skillLabel.CapitalizeFirst()).x;
                if (x > levelLabelWidth)
                {
                    levelLabelWidth = x;
                }
            }
            levelLabelWidthField.SetValue(null, levelLabelWidth);

            for (int j = 0; j < skillDefsInListOrderCached.Count; j++)
            {
                SkillDef skillDef = skillDefsInListOrderCached[j];
                float y = (float)j * 27f + offset.y;
                SkillUI.DrawSkill(p.skills.GetSkill(skillDef), new Vector2(offset.x, y), mode, "");
            }

            return false;
        }
    }
}

