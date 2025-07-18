using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public static class SolarUtil
    {

        public static bool InTrueSunlight(Pawn pawn, SolverGeneExtension ext)
        {
            if (pawn?.Map == null || pawn.Position.Roofed(pawn.Map)) return false;
            return pawn.Map.skyManager.CurSkyGlow >= ext.heatOptions.dangrousSkyGlow
                && pawn.Position.InSunlight(pawn.Map);
        }

        public static bool IsSufficientlyCovered(Pawn pawn, float requiredCoverage = 0.7f)
        {
            if (pawn == null || pawn.apparel == null || pawn.apparel.WornApparelCount == 0)
                return false;

            // Get all natural, not-missing, external body parts
            var bodyParts = pawn.RaceProps.body.AllParts
                .Where(part => !pawn.health.hediffSet.PartIsMissing(part) && part.depth == BodyPartDepth.Outside)
                .ToList();

            if (bodyParts.Count == 0)
                return false;

            int coveredCount = 0;
            foreach (var part in bodyParts)
            {
                if (pawn.apparel.WornApparel.Any(apparel => apparel.def.apparel.CoversBodyPart(part)))
                    coveredCount++;
            }

            float coverage = (float)coveredCount / bodyParts.Count;
            return coverage >= requiredCoverage;
        }

        public static bool IsHeadCovered(Pawn pawn)
        {
            if (pawn == null || pawn.apparel == null)
                return false;

            // These are the standard RimWorld body part groups for head coverage
            var headGroups = new[]
            {
        BodyPartGroupDefOf.FullHead,
        BodyPartGroupDefOf.UpperHead
    };

            foreach (var apparel in pawn.apparel.WornApparel)
            {
                if (apparel.def.apparel.bodyPartGroups != null &&
                    apparel.def.apparel.bodyPartGroups.Any(g => headGroups.Contains(g)))
                {
                    return true;
                }
            }
            return false;
        }



        /// <summary>
        /// Returns true if the pawn is currently lit by any Sun Lamp.
        /// Fast‑rejects completely dark cells via glowGrid.GroundGlowAt,
        /// then checks each lamp with LoS.
        /// </summary>
        public static bool InSunLampLight(Pawn pawn)
        {
            if (pawn?.Map == null)
                return false;

            // 1) Fast check: is there literally any light on this cell?
            const float glowThreshold = 0.01f;
            if (pawn.Map.glowGrid.GroundGlowAt(pawn.Position) < glowThreshold)
                return false;

            // 2) If there is SOME light, confirm it comes from a Sun Lamp
            var def = MD_DefOf.SunLamp;
            var map = pawn.Map;

            // Check colonist lamps
            foreach (var lamp in map.listerBuildings.AllBuildingsColonistOfDef(def))
                if (IsLampIlluminatingPawn(lamp, pawn))
                    return true;

            // Check non‑colonist lamps
            foreach (var lamp in map.listerBuildings.AllBuildingsNonColonistOfDef(def))
                if (IsLampIlluminatingPawn(lamp, pawn))
                    return true;

            return false;
        }

        /// <summary>
        /// Returns true if the given lamp’s glow reaches the pawn
        /// (requires the lamp to be on, within radius, and line‑of‑sight).
        /// Uses integer math for distance checks to avoid Sqrt allocations.
        /// </summary>
        private static bool IsLampIlluminatingPawn(Building lamp, Pawn pawn)
        {
            if (lamp == null)
                return false;

            var glowComp = lamp.GetComp<CompGlower>();
            if (glowComp == null || !glowComp.Glows)
                return false;

            // Squared‑distance test
            int dx = pawn.Position.x - lamp.Position.x;
            int dz = pawn.Position.z - lamp.Position.z;
            float radius = glowComp.GlowRadius;
            if (dx * dx + dz * dz > radius * radius)
                return false;

            // Single line‑of‑sight check
            return GenSight.LineOfSight(lamp.Position, pawn.Position, pawn.Map, true);
        }

        public static bool IsInAnySun(Pawn pawn, SolverGeneExtension ext)
        {
            return InTrueSunlight(pawn, ext) || InSunLampLight(pawn);
        }

        public static bool IsOutsideSafe(Pawn pawn, SolverGeneExtension ext)
        {
            // Returns true if outside is safe for the pawn (skyglow < dangerous threshold)
            if (pawn?.Map == null || ext == null)
                return true; // If not on a map or no extension, treat as safe

            return pawn.Map.skyManager.CurSkyGlow < ext.heatOptions.dangrousSkyGlow;
        }

        public static bool IsMapSafeForSolvers(Map map, RaidRestrictions restrictions, SolverGeneExtension geneExt = null)
        {
            if (map == null)
                return true;

            // Use the gene extension's dangerous skyglow if available, otherwise fallback to restrictions or default
            float threshold = 0.6f;
            if (geneExt != null)
                threshold = geneExt.heatOptions.dangrousSkyGlow;
            else if (restrictions != null && restrictions.maxSkyGlow > 0f)
                threshold = restrictions.maxSkyGlow;

            float currentGlow = map.skyManager.CurSkyGlow;
            bool safe = currentGlow < threshold; // <-- changed from <= to <
            Log.Message($"[SolarUtil] CurSkyGlow={currentGlow}, threshold={threshold}, safe={safe}");
            return safe;
        }



        public static bool IsThingSafe(Thing thing, SolverGeneExtension ext)
        {
            if (thing?.Map == null)
                return true;
            if (thing.Position.Roofed(thing.Map))
                return true;
            if (ext == null)
                return thing.Map.skyManager.CurSkyGlow < 0.6f;
            return thing.Map.skyManager.CurSkyGlow < ext.heatOptions.dangrousSkyGlow;
        }

        public static bool IsExtremeAmbientTemperature(Pawn pawn)
        {
            if (pawn == null) return false;
            float ambient = pawn.AmbientTemperature;
            float min = pawn.GetStatValue(StatDefOf.ComfyTemperatureMin, true);
            float max = pawn.GetStatValue(StatDefOf.ComfyTemperatureMax, true);
            return ambient < min || ambient > max;
        }
    }
}
