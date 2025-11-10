using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TheForce_Standalone.Generic
{
    internal class ModExtension_AbilityReflection : DefModExtension
    {
        public float reflectionChance = 1.0f;
        public float nullificationChance = 0.0f;
        public string reflectionStat = null; // Stat to scale reflection chance
        public string nullificationStat = null; // Stat to scale nullification chance
        public List<AbilityDef> abilityWhitelist = null; // If null, all abilities are affected
        public List<AbilityDef> abilityBlacklist = null;
        public bool affectHostileAbilitiesOnly = true;
        public EffecterDef reflectionEffect = null;
        public SoundDef reflectionSound = null;

        public bool CanReflectAbility(AbilityDef abilityDef, Pawn reflector, Pawn caster)
        {
            // Check if caster is hostile
            if (affectHostileAbilitiesOnly && caster?.Faction != null &&
                !caster.HostileTo(reflector))
                return false;

            // Check blacklist
            if (abilityBlacklist != null && abilityBlacklist.Contains(abilityDef))
                return false;

            // Check whitelist (if exists)
            if (abilityWhitelist != null && !abilityWhitelist.Contains(abilityDef))
                return false;

            return true;
        }

        public float GetReflectionChance(Pawn pawn)
        {
            float chance = reflectionChance;
            if (!string.IsNullOrEmpty(reflectionStat))
            {
                float statValue = pawn.GetStatValue(StatDef.Named(reflectionStat));
                chance *= statValue;
            }
            return Mathf.Clamp01(chance);
        }

        public float GetNullificationChance(Pawn pawn)
        {
            float chance = nullificationChance;
            if (!string.IsNullOrEmpty(nullificationStat))
            {
                float statValue = pawn.GetStatValue(StatDef.Named(nullificationStat));
                chance *= statValue;
            }
            return Mathf.Clamp01(chance);
        }
    }
}
