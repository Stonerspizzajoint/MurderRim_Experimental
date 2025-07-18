using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class Gene_LearningByObservation : Gene
    {
        // Default values if ModExtension isn't configured
        private const float DefaultRadius = 10f;
        private const int DefaultInterval = 250;
        private const float DefaultXP = 50f;
        private const int DefaultTVMaxLevel = 10;
        private const float DefaultTVXP = 30f;

        // Store the TV learning session for each pawn
        private static readonly Dictionary<Pawn, TVLearningSession> TVLearningSessions = new Dictionary<Pawn, TVLearningSession>();

        public override void Tick()
        {
            base.Tick();

            if (!pawn.IsHashIntervalTick(GetCheckInterval())) return;
            if (pawn.Dead || !pawn.Spawned) return;

            // Clean up expired sessions
            CleanupTVSessions();

            // Skip if pawn is actively training a skill through work
            if (IsPawnActivelyTrainingSkill(pawn)) return;

            if (IsWatchingTelevision(pawn))
            {
                HandleTVSession(pawn);
            }
            else
            {
                // First try to learn from a TV-watching observer
                if (TryLearnFromTVObserver(pawn))
                {
                    // Successfully learned from TV observer
                }
                else
                {
                    // Otherwise try to learn from a working pawn
                    Pawn target = FindBestObservationTarget(pawn);
                    if (target != null)
                    {
                        TryLearnFromTarget(pawn, target);
                    }
                }
            }
        }

        private void CleanupTVSessions()
        {
            // Remove sessions for dead/despawned pawns or expired sessions
            var toRemove = TVLearningSessions
                .Where(kvp => kvp.Key.Dead || !kvp.Key.Spawned || Find.TickManager.TicksGame > kvp.Value.expiryTick)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                TVLearningSessions.Remove(key);
            }
        }

        private bool IsPawnActivelyTrainingSkill(Pawn pawn)
        {
            // Check if pawn is doing a job that would normally grant skill XP
            if (pawn.CurJob == null || pawn.CurJob.workGiverDef == null) return false;

            WorkTypeDef workType = pawn.CurJob.workGiverDef.workType;
            if (workType == null || workType.relevantSkills.Count == 0) return false;

            // Check if pawn is below max level in any relevant skill
            foreach (SkillDef skillDef in workType.relevantSkills)
            {
                SkillRecord skill = pawn.skills?.GetSkill(skillDef);
                if (skill != null && skill.Level < 20) // 20 is max skill level
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsWatchingTelevision(Pawn pawn)
        {
            return pawn.CurJobDef == MD_DefOf.WatchTelevision;
        }

        private void HandleTVSession(Pawn learner)
        {
            // Check if we need to start a new session
            if (!TVLearningSessions.TryGetValue(learner, out TVLearningSession session))
            {
                StartNewTVSession(learner);
                return;
            }

            // Continue existing session
            ContinueTVSession(learner, session);
        }

        private void StartNewTVSession(Pawn learner)
        {
            List<SkillRecord> skills = learner.skills?.skills;
            if (skills == null || skills.Count == 0) return;

            // Get eligible skills below TV cap
            List<SkillRecord> eligibleSkills = skills
                .Where(s => s.Level < GetTVMaxSkillLevel())
                .ToList();

            if (eligibleSkills.Count == 0) return;

            // Select a random skill for this session
            SkillDef chosenSkill = eligibleSkills.RandomElement().def;

            // Check if there's a nearby TV watcher to coordinate with
            var nearbyWatchers = GetNearbyTVWatchers(learner);
            foreach (var watcher in nearbyWatchers)
            {
                if (TVLearningSessions.TryGetValue(watcher, out TVLearningSession existingSession))
                {
                    // Coordinate with existing session
                    chosenSkill = existingSession.learnedSkill;
                    break;
                }
            }

            // Create new learning session
            var newSession = new TVLearningSession
            {
                learnedSkill = chosenSkill,
                startTick = Find.TickManager.TicksGame,
                expiryTick = Find.TickManager.TicksGame + 2500 // 60 seconds at normal speed
            };

            TVLearningSessions[learner] = newSession;

            // Apply initial learning
            ApplyTVLearning(learner, chosenSkill);
        }

        private void ContinueTVSession(Pawn learner, TVLearningSession session)
        {
            // Check if session is still valid
            SkillRecord skillRec = learner.skills?.GetSkill(session.learnedSkill);
            if (skillRec == null || skillRec.Level >= GetTVMaxSkillLevel())
            {
                // Skill capped or invalid - start new session
                TVLearningSessions.Remove(learner);
                StartNewTVSession(learner);
                return;
            }

            // Continue learning the same skill
            ApplyTVLearning(learner, session.learnedSkill);

            // Extend session duration
            session.expiryTick = Find.TickManager.TicksGame + 2500;
        }

        private void ApplyTVLearning(Pawn learner, SkillDef skill)
        {
            SkillRecord skillRec = learner.skills?.GetSkill(skill);
            if (skillRec == null || skillRec.Level >= GetTVMaxSkillLevel()) return;

            // Calculate XP with learning rate factor
            float baseXP = GetTVXPGainAmount();
            float learningRateFactor = learner.GetStatValue(StatDefOf.LearningRateFactor);
            float adjustedXP = baseXP * learningRateFactor;

            skillRec.Learn(adjustedXP);
        }

        private bool TryLearnFromTVObserver(Pawn learner)
        {
            // Only allow if learner is free to learn
            if (!IsFreeToLearnByObservation(learner))
                return false;

            // Find nearby pawns watching TV
            var tvWatchers = GetNearbyTVWatchers(learner);
            if (tvWatchers.Count == 0) return false;

            // Find a TV watcher with an active session
            foreach (Pawn tvWatcher in tvWatchers.OrderBy(p => p.Position.DistanceToSquared(learner.Position)))
            {
                if (TVLearningSessions.TryGetValue(tvWatcher, out TVLearningSession session))
                {
                    // Check if learner can learn this skill
                    SkillRecord skillRec = learner.skills?.GetSkill(session.learnedSkill);
                    if (skillRec != null && skillRec.Level < GetTVMaxSkillLevel())
                    {
                        // Calculate XP with learning rate factor
                        float baseXP = GetTVXPGainAmount();
                        float learningRateFactor = learner.GetStatValue(StatDefOf.LearningRateFactor);
                        float adjustedXP = baseXP * learningRateFactor;

                        skillRec.Learn(adjustedXP);
                        return true;
                    }
                }
            }

            return false;
        }

        private List<Pawn> GetNearbyTVWatchers(Pawn centerPawn)
        {
            if (centerPawn.Map == null) return new List<Pawn>();
            float radius = GetObservationRadius();

            return GenRadial.RadialDistinctThingsAround(centerPawn.Position, centerPawn.Map, radius, true)
                .OfType<Pawn>()
                .Where(p => p != centerPawn &&
                       p.RaceProps.Humanlike &&
                       p.Faction == centerPawn.Faction &&
                       !p.Dead &&
                       IsWatchingTelevision(p) &&
                       GenSight.LineOfSight(centerPawn.Position, p.Position, centerPawn.Map))
                .ToList();
        }

        private Pawn FindBestObservationTarget(Pawn observer)
        {
            if (observer.Map == null) return null;
            float radius = GetObservationRadius();

            return GenRadial.RadialDistinctThingsAround(observer.Position, observer.Map, radius, true)
                .OfType<Pawn>()
                .Where(p => IsValidTarget(observer, p))
                .OrderBy(p => p.Position.DistanceToSquared(observer.Position))
                .FirstOrDefault();
        }

        private bool IsValidTarget(Pawn observer, Pawn target)
        {
            return target != observer &&
                   target.RaceProps.Humanlike &&
                   target.Faction == observer.Faction &&
                   !target.Dead &&
                   target.Awake() &&
                   target.CurJob != null &&
                   target.CurJob.workGiverDef != null &&
                   !IsWatchingTelevision(target) && // Don't learn from TV watchers
                   GenSight.LineOfSight(observer.Position, target.Position, observer.Map);
        }

        private void TryLearnFromTarget(Pawn observer, Pawn target)
        {
            // Only allow if observer is free to learn
            if (!IsFreeToLearnByObservation(observer))
                return;

            WorkTypeDef workType = target.CurJob.workGiverDef.workType;
            if (workType == null || workType.relevantSkills.Count == 0) return;

            SkillDef targetSkill = workType.relevantSkills[0];
            SkillRecord observerSkill = observer.skills?.GetSkill(targetSkill);
            SkillRecord targetSkillRecord = target.skills?.GetSkill(targetSkill);

            if (observerSkill == null || targetSkillRecord == null) return;
            if (observerSkill.Level >= targetSkillRecord.Level) return;

            float baseXP = GetXPGainAmount();
            float learningRateFactor = observer.GetStatValue(StatDefOf.LearningRateFactor);
            float adjustedXP = baseXP * learningRateFactor;

            observerSkill.Learn(adjustedXP);
        }

        // Utility: Only allow learning if not doing a job that uses a skill
        private bool IsFreeToLearnByObservation(Pawn pawn)
        {
            // If the pawn is not doing any job, they are free to watch/learn
            if (pawn.CurJob == null || pawn.CurJob.workGiverDef == null)
                return true;

            // If the job's work type is null or has no relevant skills, they are free to learn
            var workType = pawn.CurJob.workGiverDef.workType;
            if (workType == null || workType.relevantSkills == null || workType.relevantSkills.Count == 0)
                return true;

            // Otherwise, the pawn is busy with a skill-using job
            return false;
        }


        // Configuration getters with ModExtension fallback
        private float GetObservationRadius()
        {
            var extension = def.GetModExtension<GeneExtension_Learning>();
            return extension?.observationRadius ?? DefaultRadius;
        }

        private int GetCheckInterval()
        {
            var extension = def.GetModExtension<GeneExtension_Learning>();
            return extension?.checkIntervalTicks ?? DefaultInterval;
        }

        private float GetXPGainAmount()
        {
            var extension = def.GetModExtension<GeneExtension_Learning>();
            return extension?.xpPerObservation ?? DefaultXP;
        }

        private float GetTVXPGainAmount()
        {
            var extension = def.GetModExtension<GeneExtension_Learning>();
            return extension?.xpPerTVObservation ?? DefaultTVXP;
        }

        private int GetTVMaxSkillLevel()
        {
            var extension = def.GetModExtension<GeneExtension_Learning>();
            return extension?.tvMaxSkillLevel ?? DefaultTVMaxLevel;
        }

        // TV Learning Session structure
        private class TVLearningSession
        {
            public SkillDef learnedSkill;
            public int startTick;
            public int expiryTick;
        }
    }
}
