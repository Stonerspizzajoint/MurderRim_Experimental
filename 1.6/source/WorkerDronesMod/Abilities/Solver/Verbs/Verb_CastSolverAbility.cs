using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace WorkerDronesMod
{
    public class Verb_CastSolverAbility : Verb_CastAbility
    {
        // Optional: Override this to customize the cast action
        protected override bool TryCastShot()
        {
            // You can add custom logic here before/after the base call
            // For example, play a custom sound, spawn effects, etc.

            // Example: Log when casting
            Log.Message($"{CasterPawn} is casting a Solver ability: {Ability?.def?.defName}");

            // Call base to perform the actual ability effect
            return base.TryCastShot();
        }

        // Optional: Override for custom warmup, targeting, etc.
        // public override void WarmupComplete() { ... }
        // public override void OrderForceTarget(LocalTargetInfo target) { ... }
    }
}

