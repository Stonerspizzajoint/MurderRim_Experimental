using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(SkillUI), nameof(SkillUI.DrawSkillsOf))]
    public static class Patch_SkillUI_DrawSkillsOf
    {
        private static readonly FieldInfo levelLabelWidthField = typeof(SkillUI).GetField("levelLabelWidth", BindingFlags.NonPublic | BindingFlags.Static);

        static bool Prefix(Pawn p, Vector2 offset, SkillUI.SkillDrawMode mode, Rect container)
        {
            if (p.DevelopmentalStage.Baby())
                return true;

            List<SkillDef> allDefsListForReading = DefDatabase<SkillDef>.AllDefsListForReading;
            List<SkillDef> filteredSkills = new List<SkillDef>(allDefsListForReading.Count);
            foreach (var skillDef in allDefsListForReading)
            {
                if (skillDef == MD_DefOf.SolverControl)
                {
                    // Only show if pawn has a non-nerfed solver gene
                    if (!ExtraSolverUtils.HasSolver(p))
                        continue;

                    // Find the first active solver gene (Basic or Absolute)
                    Gene solverGene = p.genes?.GetGene(MD_DefOf.MD_BasicSolver) ?? p.genes?.GetGene(MD_DefOf.MD_AbsoluteSolver);
                    var ext = solverGene?.def.GetModExtension<SolverGeneExtension>();
                    if (ext == null || ext.isNerfedSolver)
                        continue;
                }
                filteredSkills.Add(skillDef);
            }


            // Use reflection to get/set levelLabelWidth
            float levelLabelWidth = (float)levelLabelWidthField.GetValue(null);
            for (int i = 0; i < filteredSkills.Count; i++)
            {
                float x = Text.CalcSize(filteredSkills[i].skillLabel.CapitalizeFirst()).x;
                if (x > levelLabelWidth)
                {
                    levelLabelWidth = x;
                }
            }
            levelLabelWidthField.SetValue(null, levelLabelWidth);

            for (int j = 0; j < filteredSkills.Count; j++)
            {
                SkillDef skillDef = filteredSkills[j];
                float y = (float)j * 27f + offset.y;
                SkillUI.DrawSkill(p.skills.GetSkill(skillDef), new Vector2(offset.x, y), mode, "");
            }

            return false;
        }
    }
}

