using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using VREAndroids;

namespace WorkerDronesMod
{
    public static class SolverGeneUtility
    {
        #region Gene Initialization & Retrieval

        /// <summary>
        /// Retrieves the first active gene of type Gene_HeatBuildup on the pawn.
        /// Returns null if not found.
        /// </summary>
        public static Gene_HeatBuildup GetHeatGene(Pawn pawn)
        {
            return pawn.genes?.GetFirstGeneOfType<Gene_HeatBuildup>();
        }

        #endregion

        #region Status Checks & Critical Damage
        /// <summary>
        /// Checks if the pawn is overheating.
        /// </summary>
        public static bool IsOverheating(Pawn pawn)
        {
            return pawn.health.hediffSet.HasHediff(MD_DefOf.MD_Overheating);
        }

        /// <summary>
        /// Checks if the pawn suffers from Nanite Acid Buildup.
        /// </summary>
        public static bool IsNaniteAcidBuildupActive(Pawn pawn)
        {
            return pawn.health.hediffSet.HasHediff(MD_DefOf.MD_NaniteAcidBuildup);
        }

        /// <summary>
        /// Checks if the pawn is suffering from any healing-affecting hediffs. (IsNaniteAcidBuildupActive, IsOverheating, etc)
        /// </summary>
        public static bool HasHealingAffectingHediff(Pawn pawn)
        {
            if (pawn == null)
                return false;

            // Returns true if either condition is true
            return IsNaniteAcidBuildupActive(pawn) || IsOverheating(pawn);
        }

        /// <summary>
        /// Determines if the pawn is critically damaged.
        /// Critical damage occurs if the pawn is largely missing vital body parts,
        /// such as the stomach, or if less than 20% of all body parts remain intact.
        /// Additionally, if the pawn is missing its head, there is a 20% chance it is considered critically damaged.
        /// </summary>
        public static bool IsCriticallyDamaged(Pawn pawn)
        {
            if (pawn == null)
                return false;

            // If we previously flagged a safe removal, but the pawn now has a stomach again, clear the flag:
            if (SurgerySafetyUtility.HasSafeStomachRemoval(pawn))
            {
                bool nowHasStomach = pawn.health.hediffSet.GetNotMissingParts()
                    .Any(p => p.def == MD_DefOf.Stomach);
                if (nowHasStomach)
                {
                    SurgerySafetyUtility.ClearSafeStomachRemoval(pawn);
                }
            }

            // 1) Stomach‐missing test (skipped if still flagged):
            if (!SurgerySafetyUtility.HasSafeStomachRemoval(pawn))
            {
                bool hasStomach = pawn.health.hediffSet.GetNotMissingParts()
                    .Any(p => p.def == MD_DefOf.Stomach);
                if (!hasStomach)
                    return true;
            }

            // 2) Head‐missing test…
            var missingHead = pawn.health.hediffSet.hediffs
                .OfType<Hediff_MissingPart>()
                .FirstOrDefault(h => h.Part.def == BodyPartDefOf.Head);
            if (missingHead != null && !missingHead.IsFresh && Rand.Value < 0.2f)
                return true;

            // 3) Overall integrity…
            int intact = pawn.health.hediffSet.GetNotMissingParts()
                    .Count(part => !pawn.health.hediffSet.hediffs.Any(h => h.Part == part && h.def == MD_DefOf.MD_FleshyPart));
            int total = pawn.RaceProps.body.AllParts.Count;
            return total > 0 && ((float)intact / total) < 0.2f;
        }



        /// <summary>
        /// Enforces critical damage by killing the pawn if it is critically damaged.
        /// If the pawn is a core heart and has MD_ResurrectionStasis applied,
        /// the stasis hediff is removed immediately before the pawn is killed.
        /// </summary>
        public static void EnforceCriticalDamage(Pawn pawn)
        {
            // First, bail out if not critically damaged or already dead.
            if (!IsCriticallyDamaged(pawn) || pawn.Dead)
                return;

            if (DebugSettings.godMode)
                Log.Message($"[SolverGeneUtility] Enforcing critical damage on {pawn.LabelShort}.");

            // Helper to remove Resurrection Stasis if present.
            void RemoveStasis()
            {
                var stasis = pawn.health.hediffSet.hediffs
                    .FirstOrDefault(h => h.def == MD_DefOf.MD_ResurrectionStasis);
                if (stasis != null)
                {
                    pawn.health.RemoveHediff(stasis);
                    if (DebugSettings.godMode)
                        Log.Message($"[SolverGeneUtility] Removed MD_ResurrectionStasis from {pawn.LabelShort}.");
                }
            }

            // CoreHeart pawns should drop out of stasis before dying.
            if (IsACoreHeart(pawn))
            {
                if (DebugSettings.godMode)
                    Log.Message($"[SolverGeneUtility] {pawn.LabelShort} is a core heart; checking for MD_ResurrectionStasis.");
                RemoveStasis();
            }
            else
            {
                // Non–CoreHeart also drop stasis if it’s there.
                RemoveStasis();
            }

            // Finally, kill—but only if still alive
            if (!pawn.Dead)
                pawn.Kill(null, null);
        }

        #endregion

        #region Overheating & Notification

        /// <summary>
        /// Returns true if the pawn should be notified about overheating.
        /// </summary>
        public static bool ShouldNotifyPlayer(Pawn pawn)
        {
            return pawn.IsColonistPlayerControlled || pawn.Faction == Faction.OfPlayer;
        }

        /// <summary>
        /// Applies an overheating effect to the pawn using the specified hediff and severity.
        /// </summary>
        public static void ApplyOverheatingEffect(Pawn pawn, HediffDef def, float severity)
        {
            if (pawn.health.hediffSet.HasHediff(def))
                return;
            Hediff hediff = HediffMaker.MakeHediff(def, pawn);
            hediff.Severity = severity;
            pawn.health.AddHediff(hediff);
            Messages.Message($"{pawn.LabelShort} is overheating!", MessageTypeDefOf.ThreatSmall);
        }

        /// <summary>
        /// Tries to apply an overheating effect based on the pawn's heat above a given threshold.
        /// </summary>
        public static void TryApplyOverheating(Pawn pawn, Gene_HeatBuildup heatGene, float heatPercent, HediffDef def, float thresholdPercent)
        {
            float chance = (heatPercent - thresholdPercent) / (1f - thresholdPercent);
            if (Rand.Value < chance)
                ApplyOverheatingEffect(pawn, def, heatPercent);
        }

        /// <summary>
        /// Determines whether the pawn is safe from the sun.
        /// A pawn is considered safe if it is on a map and the current sky glow is 0.5 or lower.
        /// </summary>
        public static bool IsSafeFromSun(Pawn pawn)
        {
            return pawn != null && pawn.Map != null && pawn.Map.skyManager.CurSkyGlow <= 0.5f;
        }

        /// <summary>
        /// Determines whether the map is safe from the sun.
        /// </summary>
        public static bool IsMapSafeFromSun(Map map)
        {
            // A map is considered safe from the sun if the current sky glow is 0.5 or lower.
            return map != null && map.skyManager.CurSkyGlow <= 0.5f;
        }

        public static bool IsThingSafeFromSun(Thing thing)
        {
            if (thing == null || thing.Map == null) return false;
            IntVec3 pos = thing.Position;
            Map map = thing.Map;
            if (!pos.InBounds(map)) return false;

            // Safe if roofed or not in direct sunlight
            return pos.Roofed(map) || !pos.InSunlight(map);
        }

        public static bool IsValidConsumptionTarget(Thing t)
        {
            return !t.IsForbidden(Faction.OfPlayer) // Assuming you don't need a specific pawn
                && !t.IsBurning()
                && !t.Position.Fogged(t.Map)
                && (t.Position.Roofed(t.Map) || SolverGeneUtility.IsThingSafeFromSun(t));
        }

        #endregion

        #region Interchangeable Parts Checks

        /// <summary>
        /// Checks whether the pawn has any hediff whose defName starts with "MD_interchangeable"
        /// but not with "MD_interchangeableRanged_". This indicates interchangeable melee parts.
        /// </summary>
        public static bool HasInterchangeableMelee(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
                return false;

            // If the pawn has any ranged-type hediff, do not count melee.
            if (pawn.health.hediffSet.hediffs.Any(h => h.def.defName.StartsWith("MD_interchangeableRanged_")))
                return false;

            return pawn.health.hediffSet.hediffs.Any(h =>
                h.def.defName.StartsWith("MD_interchangeable") &&
                !h.def.defName.StartsWith("MD_interchangeableRanged_"));
        }

        /// <summary>
        /// Checks whether the pawn has any hediff whose defName starts with "MD_interchangeableRanged_".
        /// This indicates interchangeable ranged parts.
        /// </summary>
        public static bool HasInterchangeableRanged(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
                return false;
            return pawn.health.hediffSet.hediffs.Any(h =>
                h.def.defName.StartsWith("MD_interchangeableRanged_"));
        }

        /// <summary>
        /// Returns true if the pawn's oil resource (via its Neutroamine Oil gene) is empty.
        /// This is determined by checking if the gene's value is less than or equal to zero.
        /// </summary>
        /// <param name="pawn">The pawn to check.</param>
        /// <returns>True if oil is empty, false otherwise.</returns>
        public static bool IsOilEmpty(Pawn pawn)
        {
            if (pawn == null || pawn.genes == null)
            {
                return false;
            }

            // Try to get the Neutroamine Oil gene from the pawn.
            // Replace Gene_NeutroamineOil with the actual class name of your oil gene.
            var oilGene = pawn.genes.GetFirstGeneOfType<Gene_NeutroamineOil>();

            // If the pawn doesn't have an oil gene, we assume oil doesn't run out.
            if (oilGene == null)
            {
                return false;
            }

            // You can adjust the threshold here if needed.
            return oilGene.Value <= 0;
        }


        #endregion

        #region Solver Checks

        /// <summary>
        /// Determines whether the given pawn's race is MD_CoreHeartRace.
        /// </summary>
        public static bool IsACoreHeart(Pawn pawn)
        {
            return pawn != null && pawn.def == MD_DefOf.MD_CoreHeartRace;
        }

        /// <summary>
        /// Checks if the given pawn has the MD_WeakenedSolver gene.
        /// </summary>
        public static bool HasSolver(Pawn pawn)
        {
            return pawn != null && pawn.HasActiveGene(MD_DefOf.MD_WeakenedSolver);
        }

        #endregion

        #region Wound Healing Warmup Timer

        // A small class to store warmup timer data per injury.
        public class WarmupData
        {
            public int ticksRemaining;
        }

        /// <summary>
        /// For a given pawn and injury, checks (and updates) the warmup timer for that individual injury.
        /// When an injury is first observed, if no other injuries are tracked it uses the initial warmup value;
        /// otherwise (if some injuries are already being tracked) it uses the cooldown value.
        /// Once a timer is set, it is not reset even if new injuries appear—it is simply allowed to tick down.
        /// Returns true when the injury’s timer expires (allowing that wound to heal).
        /// </summary>
        /// <param name="pawn">The pawn for which to check the injury.</param>
        /// <param name="injury">The specific injury (Hediff_Injury) to check.</param>
        /// <param name="initialWarmupTicks">Initial warmup ticks value from the mod extension.</param>
        /// <param name="cooldownTicks">Cooldown ticks to assign for new injuries when other wounds are already tracked.</param>
        public static bool IsInjuryReadyForHealing(Pawn pawn, Hediff_Injury injury, int initialDelay, int additionalDelay)
        {
            if (pawn == null || injury == null)
                return true;

            // Get (or create) the pawn's dictionary for injury timer data.
            if (!pawnInjuryTimers.TryGetValue(pawn, out Dictionary<Hediff_Injury, InjuryTimerData> timers))
            {
                timers = new Dictionary<Hediff_Injury, InjuryTimerData>();
                pawnInjuryTimers[pawn] = timers;
            }

            // If this injury is new, assign it the initial delay and record its current severity.
            if (!timers.ContainsKey(injury))
            {
                InjuryTimerData data = new InjuryTimerData();
                data.remainingDelay = initialDelay;
                data.lastSeverity = injury.Severity;
                data.delayCompleted = false;
                timers[injury] = data;
                return false;
            }
            else
            {
                InjuryTimerData data = timers[injury];

                // Check if new damage has increased the injury's severity.
                // Only reapply the delay if the injury hasn't already finished delaying.
                if (!data.delayCompleted && injury.Severity > data.lastSeverity)
                {
                    // Reset the delay using the additional delay value.
                    data.remainingDelay = additionalDelay;
                }

                // Update the last known severity.
                data.lastSeverity = injury.Severity;

                // If delay is not yet completed, decrement the timer.
                if (!data.delayCompleted)
                {
                    data.remainingDelay--;
                    if (data.remainingDelay <= 0)
                    {
                        data.delayCompleted = true;
                        // Once the delay is complete, we could optionally leave the data
                        // (so that subsequent ticks automatically return true) or remove it.
                        // Here we mark it complete so that healing happens every tick after the pause.
                    }
                }

                return data.delayCompleted;
            }
        }

        public class InjuryTimerData
        {
            // The number of ticks left before healing begins.
            public int remainingDelay;

            // The last recorded severity for detecting new damage.
            public float lastSeverity;

            // Once set to true, the delay has completed and healing can go on continuously.
            public bool delayCompleted;
        }

        // Dictionary to track injury timers per pawn.
        private static Dictionary<Pawn, Dictionary<Hediff_Injury, InjuryTimerData>> pawnInjuryTimers =
            new Dictionary<Pawn, Dictionary<Hediff_Injury, InjuryTimerData>>();

        #endregion

        #region Missing Part Warmup Timer

        // A small class to store warmup timer data for a missing part.
        public class MissingPartTimerData
        {
            // The number of ticks left before regeneration begins.
            public int remainingDelay;

            // Once set to true, the delay has finished so that regeneration can proceed continuously.
            public bool delayCompleted;
        }

        // Dictionary to track warmup data for missing parts per pawn.
        private static Dictionary<Pawn, Dictionary<Hediff_MissingPart, MissingPartTimerData>> pawnMissingPartTimers =
            new Dictionary<Pawn, Dictionary<Hediff_MissingPart, MissingPartTimerData>>();

        /// <summary>
        /// Checks (and updates) whether a given missing part is ready for regeneration.
        /// If the missing part is new, it is assigned a delay: if it’s the only missing part then the initial delay is used,
        /// otherwise the cooldown value is used. Once assigned, the timer ticks down without being reset.
        /// Returns true when the timer is expired.
        /// </summary>
        public static bool IsMissingPartReadyForRegeneration(Pawn pawn, Hediff_MissingPart missingPart, int initialDelay, int additionalDelay)
        {
            if (pawn == null || missingPart == null)
                return true;

            // Get (or create) the dictionary for missing-part timers for this pawn.
            if (!pawnMissingPartTimers.TryGetValue(pawn, out Dictionary<Hediff_MissingPart, MissingPartTimerData> timers))
            {
                timers = new Dictionary<Hediff_MissingPart, MissingPartTimerData>();
                pawnMissingPartTimers[pawn] = timers;
            }

            // If this missing part is new, assign it the initial delay.
            if (!timers.ContainsKey(missingPart))
            {
                MissingPartTimerData data = new MissingPartTimerData();
                data.remainingDelay = initialDelay;
                data.delayCompleted = false;
                timers[missingPart] = data;
                return false;
            }
            else
            {
                MissingPartTimerData data = timers[missingPart];
                // Optionally, you could check for additional damage and then reset the delay using additionalDelay.
                // For missing parts, this is often not needed. Simply tick down the delay.
                if (!data.delayCompleted)
                {
                    data.remainingDelay--;
                    if (data.remainingDelay <= 0)
                    {
                        data.delayCompleted = true;
                    }
                }
                return data.delayCompleted;
            }
        }
        #endregion
    }
}