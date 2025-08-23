using System.Collections.Generic;
using Verse;
using System;
using System.Reflection;
using FacialAnimation;
using HarmonyLib;

namespace WorkerDronesMod.FacialAnimationCompat
{
    public static class FacialAnimationBatcher
    {
        private static readonly Queue<Pawn> animationRebuildQueue = new Queue<Pawn>();
        private const int MaxPerTick = 8; // Tune this for your needs

        private static readonly Type FacType = AccessTools.TypeByName("FacialAnimation.FacialAnimationControllerComp");
        private static readonly Type FaHelperType = AccessTools.TypeByName("FacialAnimation.FAHelper");
        private static readonly FieldInfo AnimDictField = FacType?.GetField("animationDict", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly MethodInfo CreateAnimDict = FaHelperType?.GetMethod("CreateAnimationDict", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        public static void QueueAnimationRebuild(Pawn pawn)
        {
            if (pawn == null || pawn.DestroyedOrNull()) return;
            if (!animationRebuildQueue.Contains(pawn))
                animationRebuildQueue.Enqueue(pawn);
        }

        public static void ProcessQueue()
        {
            int count = 0;
            while (animationRebuildQueue.Count > 0 && count < MaxPerTick)
            {
                var pawn = animationRebuildQueue.Dequeue();
                TryRebuildAnimDict(pawn);
                count++;
            }
        }

        private static void TryRebuildAnimDict(Pawn pawn)
        {
            if (FacType == null || AnimDictField == null || CreateAnimDict == null) return;
            var facComp = pawn.AllComps?.FirstOrDefault(c => FacType.IsInstanceOfType(c));
            if (facComp == null) return;
            var animDict = AnimDictField.GetValue(facComp) as Dictionary<string, List<FaceAnimation>>;
            if (animDict == null) return;
            object[] parameters = new object[] { pawn, Find.TickManager.TicksGame, animDict };
            CreateAnimDict.Invoke(null, parameters);
            if (parameters[2] != null)
                AnimDictField.SetValue(facComp, parameters[2]);
        }
    }
}

