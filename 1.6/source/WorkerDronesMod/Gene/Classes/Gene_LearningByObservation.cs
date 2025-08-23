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
        private const float AndroidBabyLearningMultiplier = 2.0f; // Baby androids learn twice as fast

        // Store the TV learning session for each pawn
        private static readonly Dictionary<Pawn, TVLearningSession> TVLearningSessions = new Dictionary<Pawn, TVLearningSession>();

        public override void Tick()
        {
            base.Tick();

            if (!pawn.IsHashIntervalTick(GetCheckInterval())) return;
            if (pawn.Dead) return;

            // Clean up expired sessions
            CleanupTVSessions();

            // Skip if pawn is actively training a skill through work (except for baby androids)
            if (!BabyAndroidUtil.IsBabyAndroid(pawn) && IsPawnActivelyTrainingSkill(pawn)) return;

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
                    if (BabyAndroidUtil.IsBabyAndroid(pawn))
                    {
                        // Learn from all valid targets and learn faster
                        var targets = FindAllObservationTargets(pawn);
                        foreach (var target in targets)
                        {
                            TryLearnFromTarget(pawn, target, AndroidBabyLearningMultiplier);
                        }
                    }
                    else
                    {
                        Pawn target = FindBestObservationTarget(pawn);
                        if (target != null)
                        {
                            TryLearnFromTarget(pawn, target);
                        }
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
            if (pawn.CurJob == null || pawn.CurJob.workGiverDef == null) return false;

            WorkTypeDef workType = pawn.CurJob.workGiverDef.workType;
            if (workType == null || workType.relevantSkills.Count == 0) return false;

            foreach (SkillDef skillDef in workType.relevantSkills)
            {
                SkillRecord skill = pawn.skills?.GetSkill(skillDef);
                if (skill != null && skill.Level < 20)
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
            if (!TVLearningSessions.TryGetValue(learner, out TVLearningSession session))
            {
                StartNewTVSession(learner);
                return;
            }
            ContinueTVSession(learner, session);
        }

        private void StartNewTVSession(Pawn learner)
        {
            List<SkillRecord> skills = learner.skills?.skills;
            if (skills == null || skills.Count == 0) return;

            List<SkillRecord> eligibleSkills = skills
                .Where(s => s.Level < GetTVMaxSkillLevel())
                .ToList();

            if (eligibleSkills.Count == 0) return;

            SkillDef chosenSkill = eligibleSkills.RandomElement().def;

            var nearbyWatchers = GetNearbyTVWatchers(learner);
            foreach (var watcher in nearbyWatchers)
            {
                if (TVLearningSessions.TryGetValue(watcher, out TVLearningSession existingSession))
                {
                    chosenSkill = existingSession.learnedSkill;
                    break;
                }
            }

            var newSession = new TVLearningSession
            {
                learnedSkill = chosenSkill,
                startTick = Find.TickManager.TicksGame,
                expiryTick = Find.TickManager.TicksGame + 2500
            };

            TVLearningSessions[learner] = newSession;
            ApplyTVLearning(learner, chosenSkill);
        }

        private void ContinueTVSession(Pawn learner, TVLearningSession session)
        {
            SkillRecord skillRec = learner.skills?.GetSkill(session.learnedSkill);
            if (skillRec == null || skillRec.Level >= GetTVMaxSkillLevel())
            {
                TVLearningSessions.Remove(learner);
                StartNewTVSession(learner);
                return;
            }
            ApplyTVLearning(learner, session.learnedSkill);
            session.expiryTick = Find.TickManager.TicksGame + 2500;
        }

        private void ApplyTVLearning(Pawn learner, SkillDef skill)
        {
            SkillRecord skillRec = learner.skills?.GetSkill(skill);
            if (skillRec == null || skillRec.Level >= GetTVMaxSkillLevel()) return;

            float baseXP = GetTVXPGainAmount();
            float learningRateFactor = learner.GetStatValue(StatDefOf.LearningRateFactor);
            float adjustedXP = baseXP * learningRateFactor;

            skillRec.Learn(adjustedXP);
        }

        private bool TryLearnFromTVObserver(Pawn learner)
        {
            // Allow baby androids to learn even if doing a job
            if (!BabyAndroidUtil.IsBabyAndroid(learner) && !IsFreeToLearnByObservation(learner))
                return false;

            var tvWatchers = GetNearbyTVWatchers(learner);
            if (tvWatchers.Count == 0) return false;

            foreach (Pawn tvWatcher in tvWatchers.OrderBy(p => p.Position.DistanceToSquared(learner.Position)))
            {
                if (TVLearningSessions.TryGetValue(tvWatcher, out TVLearningSession session))
                {
                    SkillRecord skillRec = learner.skills?.GetSkill(session.learnedSkill);
                    if (skillRec != null && skillRec.Level < GetTVMaxSkillLevel())
                    {
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

        // --- Updated: Use effective position/map for held pawns ---
        private bool TryGetEffectivePositionAndMap(Pawn pawn, out IntVec3 pos, out Map map)
        {
            if (pawn.Spawned)
            {
                pos = pawn.Position;
                map = pawn.Map;
                return true;
            }
            // If being carried by another pawn, use the carrier's position/map
            if (pawn.ParentHolder is Pawn_CarryTracker carryTracker && carryTracker.pawn != null && carryTracker.pawn.Spawned)
            {
                pos = carryTracker.pawn.Position;
                map = carryTracker.pawn.Map;
                return true;
            }

            pos = IntVec3.Invalid;
            map = null;
            return false;
        }

        private List<Pawn> GetNearbyTVWatchers(Pawn centerPawn)
        {
            IntVec3 pos;
            Map map;
            if (!TryGetEffectivePositionAndMap(centerPawn, out pos, out map)) return new List<Pawn>();
            float radius = GetObservationRadius();

            return GenRadial.RadialDistinctThingsAround(pos, map, radius, true)
                .OfType<Pawn>()
                .Where(p => p != centerPawn &&
                       p.RaceProps.Humanlike &&
                       p.Faction == centerPawn.Faction &&
                       !p.Dead &&
                       IsWatchingTelevision(p) &&
                       GenSight.LineOfSight(pos, p.Position, map))
                .ToList();
        }

        private Pawn FindBestObservationTarget(Pawn observer)
        {
            IntVec3 pos;
            Map map;
            if (!TryGetEffectivePositionAndMap(observer, out pos, out map)) return null;
            float radius = GetObservationRadius();

            return GenRadial.RadialDistinctThingsAround(pos, map, radius, true)
                .OfType<Pawn>()
                .Where(p => IsValidTarget(observer, p))
                .OrderBy(p => p.Position.DistanceToSquared(pos))
                .FirstOrDefault();
        }

        private List<Pawn> FindAllObservationTargets(Pawn observer)
        {
            IntVec3 pos;
            Map map;
            if (!TryGetEffectivePositionAndMap(observer, out pos, out map)) return new List<Pawn>();
            float radius = GetObservationRadius();

            return GenRadial.RadialDistinctThingsAround(pos, map, radius, true)
                .OfType<Pawn>()
                .Where(p => IsValidTarget(observer, p))
                .ToList();
        }

        private bool IsValidTarget(Pawn observer, Pawn target)
        {
            // Get effective positions and maps for both pawns
            IntVec3 observerPos, targetPos;
            Map observerMap, targetMap;

            if (!TryGetEffectivePositionAndMap(observer, out observerPos, out observerMap))
                return false;
            if (!TryGetEffectivePositionAndMap(target, out targetPos, out targetMap))
                return false;

            // Only check line of sight if both are on the same map
            if (observerMap != targetMap)
                return false;

            return target != observer &&
                   target.RaceProps.Humanlike &&
                   target.Faction == observer.Faction &&
                   !target.Dead &&
                   target.Awake() &&
                   target.CurJob != null &&
                   target.CurJob.workGiverDef != null &&
                   !IsWatchingTelevision(target) &&
                   GenSight.LineOfSight(observerPos, targetPos, observerMap);
        }

        private void TryLearnFromTarget(Pawn observer, Pawn target, float xpMultiplier = 1f)
        {
            // Allow baby androids to learn even if doing a job
            if (!BabyAndroidUtil.IsBabyAndroid(observer) && !IsFreeToLearnByObservation(observer))
                return;

            WorkTypeDef workType = target.CurJob.workGiverDef.workType;
            if (workType == null || workType.relevantSkills.Count == 0) return;

            SkillDef targetSkill = workType.relevantSkills[0];
            SkillRecord observerSkill = observer.skills?.GetSkill(targetSkill);
            SkillRecord targetSkillRecord = target.skills?.GetSkill(targetSkill);

            if (observerSkill == null || targetSkillRecord == null) return;
            if (observerSkill.Level >= targetSkillRecord.Level) return;

            float baseXP = GetXPGainAmount() * xpMultiplier;
            float learningRateFactor = observer.GetStatValue(StatDefOf.LearningRateFactor);
            float adjustedXP = baseXP * learningRateFactor;

            observerSkill.Learn(adjustedXP);
        }

        // Utility: Only allow learning if not doing a job that uses a skill
        private bool IsFreeToLearnByObservation(Pawn pawn)
        {
            if (pawn.CurJob == null || pawn.CurJob.workGiverDef == null)
                return true;

            var workType = pawn.CurJob.workGiverDef.workType;
            if (workType == null || workType.relevantSkills == null || workType.relevantSkills.Count == 0)
                return true;

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
