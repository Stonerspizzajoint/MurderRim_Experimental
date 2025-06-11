using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using VREAndroids;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    public static class FloatMenuConsumePatch
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (pawn.genes == null || !pawn.genes.HasActiveGene(MD_DefOf.MD_WeakenedSolver))
                return;
            if (pawn.Downed || pawn.InMentalState || !pawn.IsColonist || pawn.Map == null)
                return;

            var neutroGene = pawn.genes.GetFirstGeneOfType<Gene_NeutroamineOil>();
            IntVec3 cell = IntVec3.FromVector3(clickPos);
            if (!cell.InBounds(pawn.Map))
                return;

            List<Thing> things = pawn.Map.thingGrid.ThingsListAtFast(cell);
            foreach (Thing thing in things)
            {
                // --- Corpse consumption ---
                if (thing is Corpse corpse)
                {
                    if (!SolverGeneUtility.IsThingSafeFromSun(thing))
                    {
                        opts.Add(new FloatMenuOption(
                            "MD.NotSafeToConsumeSunlight".Translate(),
                            null, MenuOptionPriority.DisabledOption, null, thing));
                        continue;
                    }

                    Pawn inner = corpse.InnerPawn;
                    if (!inner.IsAndroid()) continue;

                    if (neutroGene == null || !neutroGene.neutroamineAllowed)
                    {
                        opts.Add(new FloatMenuOption(
                            neutroGene == null
                                ? "MD.NoNeutroGene".Translate()
                                : "MD.NeutroamineConsumptionDisabled".Translate(),
                            null, MenuOptionPriority.DisabledOption, null, thing));
                        continue;
                    }

                    float needed = neutroGene.InitialResourceMax - neutroGene.Value;
                    if (needed < 2f)
                    {
                        // Not enough room to ingest even a small part
                        opts.Add(new FloatMenuOption(
                            "MD.CannotConsumeCorpseWithoutOverfill".Translate(),
                            null, MenuOptionPriority.DisabledOption, null, thing));
                        continue;
                    }

                    // Build list of (part, yield)
                    var parts = inner.health.hediffSet.GetNotMissingParts();
                    var validOrgans = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                        { "Heart", "Liver", "Kidney", "Lung", "Stomach" };
                    var yields = parts
                        .Select(p => (part: p, yield: validOrgans.Contains(p.def.label) ? 10 : 2))
                        .OrderByDescending(x => x.yield)
                        .ToList();

                    // Greedily pick parts that fit without overshoot
                    float remain = needed;
                    var chosen = new List<(BodyPartRecord part, int yield)>();
                    while (remain >= 2f && yields.Count > 0)
                    {
                        // pick highest-yield <= remain
                        var fit = yields.Where(x => x.yield <= remain).FirstOrDefault();
                        if (fit.Equals(default((BodyPartRecord, int)))) break;
                        chosen.Add(fit);
                        remain -= fit.yield;
                        yields.Remove(fit);
                    }

                    if (chosen.Count == 0)
                    {
                        opts.Add(new FloatMenuOption(
                            "MD.CannotConsumeCorpseWithoutOverfill".Translate(),
                            null, MenuOptionPriority.DisabledOption, null, thing));
                    }
                    else
                    {
                        opts.Add(new FloatMenuOption(
                            "MD.ConsumeAndroidCorpse".Translate(),
                            () =>
                            {
                                Job job = JobMaker.MakeJob(MD_DefOf.MD_Job_RefuelWithCorpse, corpse);
                                job.count = chosen.Count;
                                pawn.jobs.TryTakeOrderedJob(job);
                            },
                            MenuOptionPriority.High,
                            null,
                            thing
                        ));
                    }
                }
                // --- Neutroamine consumption ---
                else if (thing.def == MD_DefOf.Neutroamine && neutroGene != null)
                {
                    if (!SolverGeneUtility.IsThingSafeFromSun(thing))
                    {
                        opts.Add(new FloatMenuOption(
                            "MD.NotSafeToConsumeSunlight".Translate(),
                            null, MenuOptionPriority.DisabledOption, null, thing));
                        continue;
                    }

                    if (!neutroGene.neutroamineAllowed)
                    {
                        opts.Add(new FloatMenuOption(
                            "MD.NeutroamineConsumptionDisabled".Translate(),
                            null, MenuOptionPriority.DisabledOption, null, thing));
                    }
                    else
                    {
                        float needed = neutroGene.InitialResourceMax - neutroGene.Value;
                        // require at least one full neutroamine (10 units) to show the option
                        if (needed < 10f)
                        {
                            opts.Add(new FloatMenuOption(
                                "MD.CannotConsumeNeutroamine".Translate(),
                                null, MenuOptionPriority.DisabledOption, null, thing));
                        }
                        else
                        {
                            int toConsume = Mathf.FloorToInt(needed / 10f);
                            // If exact division yields zero (shouldn't here), skip
                            if (toConsume < 1)
                            {
                                opts.Add(new FloatMenuOption(
                                    "MD.CannotConsumeNeutroamine".Translate(),
                                    null, MenuOptionPriority.DisabledOption, null, thing));
                            }
                            else
                            {
                                opts.Add(new FloatMenuOption(
                                    "MD.ConsumeNeutroamine".Translate(),
                                    () =>
                                    {
                                        Job job = JobMaker.MakeJob(MD_DefOf.MD_Job_RefuelWithNeutroamine, thing);
                                        job.count = Math.Min(thing.stackCount, toConsume);
                                        pawn.jobs.TryTakeOrderedJob(job);
                                    },
                                    MenuOptionPriority.High,
                                    null,
                                    thing
                                ));
                            }
                        }
                    }
                }
            }
        }
    }
}



