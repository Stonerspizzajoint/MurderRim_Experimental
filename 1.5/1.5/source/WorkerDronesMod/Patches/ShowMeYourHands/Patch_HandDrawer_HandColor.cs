using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace WorkerDronesMod.Patches.ShowMeYourHands
{
    [HarmonyPatch]
    static class Patch_HandDrawer_get_HandColor
    {
        // Cached reflection targets
        static readonly Type HandDrawerType;
        static readonly FieldInfo ParentField;
        static readonly MethodInfo OriginalGetter;
        static readonly Type ShowMainType;
        static readonly FieldInfo MainDictField;
        static readonly FieldInfo OffDictField;

        // Initialize all reflection just once
        static Patch_HandDrawer_get_HandColor()
        {
            HandDrawerType = Type.GetType("ShowMeYourHands.HandDrawer, ShowMeYourHands");
            if (HandDrawerType != null)
            {
                ParentField = HandDrawerType.GetField("parent", BindingFlags.Public | BindingFlags.Instance);
                OriginalGetter = AccessTools.Method(
                    HandDrawerType,
                    "getHandColor",
                    new[] { typeof(Pawn), typeof(bool).MakeByRefType(), typeof(Color).MakeByRefType() }
                );
            }

            ShowMainType = AccessTools.TypeByName("ShowMeYourHands.ShowMeYourHandsMain");
            if (ShowMainType != null)
            {
                MainDictField = ShowMainType.GetField("mainHandGraphics", BindingFlags.Public | BindingFlags.Static);
                OffDictField = ShowMainType.GetField("offHandGraphics", BindingFlags.Public | BindingFlags.Static);
            }
        }

        static bool Prepare()
        {
            // Only if our types and methods were resolved
            return HandDrawerType != null
                && ParentField != null
                && OriginalGetter != null
                && ShowMainType != null
                && MainDictField != null
                && OffDictField != null;
        }

        static MethodBase TargetMethod() => AccessTools.PropertyGetter(HandDrawerType, "HandColor");

        static bool Prefix(object __instance, ref Color __result)
        {
            // 1) Get the pawn quickly
            var pawn = ParentField.GetValue(__instance) as Pawn;
            if (pawn == null)
                return true; // fall back to original

            // 2) Call original getHandColor(Pawn,out bool,out Color)
            object[] args = { pawn, false, default(Color) };
            var baseHandColor = (Color)OriginalGetter.Invoke(__instance, args);
            bool flag = (bool)args[1];
            Color secondary = (Color)args[2];

            // 3) Fast loop lookup for first HandTextureExtension
            HandTextureExtension ext = null;
            if (pawn.genes?.GenesListForReading != null)
            {
                foreach (var g in pawn.genes.GenesListForReading)
                {
                    ext = g.def.GetModExtension<HandTextureExtension>();
                    if (ext != null) break;
                }
            }

            // 4) Determine shader and texture paths
            var shader = ext?.shaderType?.Shader ?? (flag ? ShaderDatabase.CutoutComplex : ShaderDatabase.Cutout);
            var mainTex = ext != null
                ? (flag ? ext.mainCleanTexturePath : ext.mainTexturePath)
                : (flag ? "HandClean" : "Hand");
            var offTex = flag
                ? (ext?.offCleanTexturePath ?? "OffHandClean")
                : (ext?.offTexturePath ?? "OffHand");

            // 5) Build and cache main graphic
            var mainGraphic = GraphicDatabase.Get<Graphic_Single>(
                mainTex, shader, Vector2.one, baseHandColor, baseHandColor
            );
            if (ext?.mainMaskPath != null)
            {
                var mask = ContentFinder<Texture2D>.Get(ext.mainMaskPath, true);
                mainGraphic.MatSingle.SetTexture("_MaskTex", mask);
            }
            var mainDict = MainDictField.GetValue(null) as IDictionary<Pawn, Graphic>;
            if (mainDict != null) mainDict[pawn] = mainGraphic;

            // 6) Build and cache off-hand graphic
            var offColor = flag ? baseHandColor : (secondary == default(Color) ? baseHandColor : secondary);
            var offGraphic = GraphicDatabase.Get<Graphic_Single>(
                offTex, shader, Vector2.one, offColor, offColor
            );
            if (ext?.offMaskPath != null)
            {
                var mask = ContentFinder<Texture2D>.Get(ext.offMaskPath, true);
                offGraphic.MatSingle.SetTexture("_MaskTex", mask);
            }
            var offDict = OffDictField.GetValue(null) as IDictionary<Pawn, Graphic>;
            if (offDict != null) offDict[pawn] = offGraphic;

            // 7) Return our base hand color and skip the original
            __result = baseHandColor;
            return false;
        }
    }
}






