using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;           // for LoadedModManager, Log

[StaticConstructorOnStartup]
public static class HarmonyPatches
{
    static HarmonyPatches()
    {
        var harmony = new Harmony("stonerspizzajoint.murderdronesmod");
        var asm = Assembly.GetExecutingAssembly();

        // ─── 1) VANILLA PATCHES (always) ────────────────────────────────
        foreach (var patchType in asm.GetTypes()
                                     .Where(t => t.Namespace == "WorkerDronesMod.Patches"))
        {
            harmony.CreateClassProcessor(patchType).Patch();
        }

        // ─── 2) CE PATCHES (only if the CE mod is installed & active) ────
        bool ceActive = LoadedModManager.RunningModsListForReading
                           .Any(mod => mod.PackageId.Equals("ceteam.combatextended", StringComparison.OrdinalIgnoreCase));

        if (ceActive)
        {
            Log.Message("[WorkerDronesMod] Combat Extended detected → applying CE patches.");
            foreach (var patchType in asm.GetTypes()
                                         .Where(t => t.Namespace == "WorkerDronesMod.Patches.CE"))
            {
                harmony.CreateClassProcessor(patchType).Patch();
            }
        }
        else
        {
            Log.Message("[WorkerDronesMod] Combat Extended not detected → skipping CE patches.");
        }

        // ─── 3) VAGUE PATCHES (only if the VAGUE mod is installed & active) ────
        bool vagueActive = LoadedModManager.RunningModsListForReading
                            .Any(mod => mod.PackageId.Equals("vanillaandroidsunofficialexpansion.genes", StringComparison.OrdinalIgnoreCase));

        if (vagueActive)
        {
            Log.Message("[WorkerDronesMod] VAGUE mod detected → applying VAGUE patches.");
            foreach (var patchType in asm.GetTypes()
                                         .Where(t => t.Namespace == "WorkerDronesMod.Patches.VAGUE"))
            {
                harmony.CreateClassProcessor(patchType).Patch();
            }
        }
        else
        {
            Log.Message("[WorkerDronesMod] VAGUE mod not detected → skipping VAGUE patches.");
        }

        // ─── 3) ShowMeYourHands PATCHES (only if the ShowMeYourHands mod is installed & active) ────
        bool ShowMeYourHandsActive = LoadedModManager.RunningModsListForReading
                            .Any(mod => mod.PackageId.Equals("mlie.showmeyourhands", StringComparison.OrdinalIgnoreCase));

        if (ShowMeYourHandsActive)
        {
            Log.Message("[WorkerDronesMod] ShowMeYourHands mod detected → applying ShowMeYourHands patches.");
            foreach (var patchType in asm.GetTypes()
                                         .Where(t => t.Namespace == "WorkerDronesMod.Patches.ShowMeYourHands"))
            {
                harmony.CreateClassProcessor(patchType).Patch();
            }
        }
        else
        {
            Log.Message("[WorkerDronesMod] ShowMeYourHands mod not detected → skipping ShowMeYourHands patches.");
        }
    }
}


