using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace WorkerDronesMod.Patches
{
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Pawn_Kill_Patch
    {
        // This postfix runs after a pawn is killed.
        public static void Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            // Ensure damage info and instigator exist.
            if (!dinfo.HasValue) return;
            Pawn killer = dinfo.Value.Instigator as Pawn;
            if (killer == null) return;

            // Only trigger laughter if the killed pawn is humanlike.
            if (!__instance.RaceProps.Humanlike)
                return;

            // Check if the killer's race is "MD_CoreHeartRace"
            if (killer.def.defName == "MD_CoreHeartRace")
                return;

            // Check if the killer has the MD_ManicBloodlust trait.
            if (killer.story?.traits != null &&
                killer.story.traits.HasTrait(TraitDef.Named("MD_ManicBloodlust")))
            {
                // Set the chance to laugh (e.g., 50% chance).
                float laughChance = 0.5f;
                if (Rand.Value < laughChance)
                {

                    SoundDef laughSound = null;
                    // Choose sound based on gender.
                    if (killer.gender == Gender.Female)
                    {
                        laughSound = SoundDef.Named("MD_LaughterEffect");
                    }
                    else // Defaults to male sound.
                    {
                        laughSound = SoundDef.Named("MD_LaughterEffect_Male");
                    }

                    // Play the sound if it's found.
                    if (laughSound != null)
                    {
                        laughSound.PlayOneShot(new TargetInfo(killer.Position, killer.Map, false));
                    }
                }
            }
        }
    }
}



