using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VREAndroids;

namespace WorkerDronesMod
{
    [HarmonyPatch]
    public static class CharacterCardUtility_DoLeftSection_Adulthood_Postfix
    {
        public static MethodBase TargetMethod()
        {
            foreach (var nt in typeof(CharacterCardUtility).GetNestedTypes(AccessTools.all))
                foreach (var mi in nt.GetMethods(AccessTools.all))
                    if (mi.Name.Contains("<DoLeftSection>")
                        && mi.GetParameters().Length == 1
                        && mi.GetParameters()[0].ParameterType == typeof(Rect))
                        return mi;
            return null;
        }

        public static void Postfix(Pawn ___pawn, Rect ___leftRect, Rect sectionRect)
        {
            // only androids, only if they actually have an adulthood backstory
            if (___pawn == null || ___pawn.story == null || !___pawn.IsAndroid())
                return;

            try
            {
                var adulthood = ___pawn.story.GetBackstory(BackstorySlot.Adulthood);
                if (adulthood == null)
                    return;

                // compute Y offset below any childhood block
                float y = sectionRect.y;
                if (___pawn.story.GetBackstory(BackstorySlot.Childhood) != null)
                    y += 22f + 4f;

                Text.Font = GameFont.Small;

                // header label
                var labelRect = new Rect(sectionRect.x, y, ___leftRect.width, 22f);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, "MD.Transformed".Translate());
                Text.Anchor = TextAnchor.UpperLeft;

                // title background + text
                string title = adulthood.TitleCapFor(___pawn.gender);
                var titleRect = new Rect(labelRect)
                {
                    x = labelRect.x + 90f,
                    width = Text.CalcSize(title).x + 10f
                };
                var prevColor = GUI.color;
                GUI.color = CharacterCardUtility.StackElementBackground;
                GUI.DrawTexture(titleRect, BaseContent.WhiteTex);
                GUI.color = prevColor;

                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(titleRect, title.Truncate(titleRect.width, null));
                Text.Anchor = TextAnchor.UpperLeft;

                if (Mouse.IsOver(titleRect))
                {
                    Widgets.DrawHighlight(titleRect);
                    TooltipHandler.TipRegion(titleRect, adulthood.FullDescriptionFor(___pawn).Resolve());
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"[WorkerDronesMod] Error drawing Adult slot: {ex}", 42424242);
            }
        }
    }
}




