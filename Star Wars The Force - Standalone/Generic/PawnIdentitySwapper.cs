using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TheForce_Standalone.Generic
{
    public static class PawnIdentitySwapper
    {
        public static void SwapPawnIdentities(Pawn pawnA, Pawn pawnB)
        {
            if (pawnA == null || pawnB == null) return;

            SwapNames(pawnA, pawnB);
            SwapBackstories(pawnA, pawnB);
            SwapTraits(pawnA, pawnB);
            SwapSkills(pawnA, pawnB);

            if (ModsConfig.IdeologyActive)
            {
                SwapIdeo(pawnA, pawnB);
            }

            SwapFaction(pawnA, pawnB);
            SwapForceUserData(pawnA, pawnB);

            // Update pawn components
            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawnA);
            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawnB);

            // Notify about the swap
            NotifySwapComplete(pawnA, pawnB);
        }

        public static void SwapNames(Pawn pawnA, Pawn pawnB)
        {
            string tempName = pawnA.Name.ToString();
            pawnA.Name = pawnB.Name;
            pawnB.Name = new NameSingle(tempName);
        }

        public static void SwapBackstories(Pawn pawnA, Pawn pawnB)
        {
            var tempChildhood = pawnA.story.Childhood;
            var tempAdulthood = pawnA.story.Adulthood;
            pawnA.story.Childhood = pawnB.story.Childhood;
            pawnA.story.Adulthood = pawnB.story.Adulthood;
            pawnB.story.Childhood = tempChildhood;
            pawnB.story.Adulthood = tempAdulthood;
        }

        public static void SwapFaction(Pawn pawnA, Pawn pawnB)
        {
            if (pawnA.Faction != pawnB.Faction)
            {
                var tempFaction = pawnA.Faction;
                pawnA.SetFaction(pawnB.Faction);
                pawnB.SetFaction(tempFaction);
            }
        }

        public static void SwapTraits(Pawn pawnA, Pawn pawnB)
        {
            var pawnATraits = new List<Trait>(pawnA.story.traits.allTraits);
            var pawnBTraits = new List<Trait>(pawnB.story.traits.allTraits);

            pawnA.story.traits.allTraits.Clear();
            pawnB.story.traits.allTraits.Clear();

            foreach (var trait in pawnATraits)
            {
                if (trait.sourceGene is null)
                {
                    pawnB.story.traits.GainTrait(new Trait(trait.def, trait.Degree));
                }
            }

            foreach (var trait in pawnBTraits)
            {
                if (trait.sourceGene is null)
                {
                    pawnA.story.traits.GainTrait(new Trait(trait.def, trait.Degree));
                }
            }
        }

        public static void SwapSkills(Pawn pawnA, Pawn pawnB)
        {
            var tempSkills = new List<(SkillDef def, int level, Passion passion, float xpSinceLastLevel, float xpSinceMidnight)>();

            // Store pawnA's skills temporarily
            foreach (var skill in pawnA.skills.skills)
            {
                tempSkills.Add((
                    skill.def,
                    skill.Level,
                    skill.passion,
                    skill.xpSinceLastLevel,
                    skill.xpSinceMidnight));
            }

            // Copy pawnB's skills to pawnA
            foreach (var skill in pawnB.skills.skills)
            {
                var pawnASkill = pawnA.skills.GetSkill(skill.def);
                pawnASkill.Level = skill.Level;
                pawnASkill.passion = skill.passion;
                pawnASkill.xpSinceLastLevel = skill.xpSinceLastLevel;
                pawnASkill.xpSinceMidnight = skill.xpSinceMidnight;
            }

            // Copy stored pawnA skills to pawnB
            foreach (var tempSkill in tempSkills)
            {
                var pawnBSkill = pawnB.skills.GetSkill(tempSkill.def);
                pawnBSkill.Level = tempSkill.level;
                pawnBSkill.passion = tempSkill.passion;
                pawnBSkill.xpSinceLastLevel = tempSkill.xpSinceLastLevel;
                pawnBSkill.xpSinceMidnight = tempSkill.xpSinceMidnight;
            }
        }

        public static void SwapIdeo(Pawn pawnA, Pawn pawnB)
        {
            if (pawnA.ideo.Ideo != null && pawnB.ideo.Ideo != null && pawnA.Ideo != pawnB.Ideo)
            {
                var tempIdeo = pawnA.Ideo;
                pawnA.ideo.SetIdeo(pawnB.Ideo);
                pawnB.ideo.SetIdeo(tempIdeo);
            }
        }

        public static void SwapForceUserData(Pawn pawnA, Pawn pawnB)
        {
            var forceA = pawnA.GetComp<CompClass_ForceUser>();
            var forceB = pawnB.GetComp<CompClass_ForceUser>();

            if (forceA == null || forceB == null)
                return;

            // Ensure both have Force sensitivity trait if needed
            var forceTraitDef = DefDatabase<TraitDef>.GetNamed("Force_NeutralSensitivity");
            if (forceTraitDef != null)
            {
                bool aHasTrait = pawnA.story.traits.HasTrait(forceTraitDef);
                bool bHasTrait = pawnB.story.traits.HasTrait(forceTraitDef);

                if (!forceA.IsValidForceUser && bHasTrait)
                {
                    pawnA.story.traits.GainTrait(new Trait(forceTraitDef, degree: 1));
                }
                if (!forceB.IsValidForceUser && aHasTrait)
                {
                    pawnB.story.traits.GainTrait(new Trait(forceTraitDef, degree: 1));
                }
            }

            // Swap force level
            int tempLevel = forceA.forceLevel;
            forceA.forceLevel = forceB.forceLevel;
            forceB.forceLevel = tempLevel;

            // Swap current FP
            float tempFP = forceA.currentFP;
            forceA.currentFP = forceB.currentFP;
            forceB.currentFP = tempFP;

            // Swap alignment
            float tempDark = forceA.Alignment.DarkSideAttunement;
            float tempLight = forceA.Alignment.LightSideAttunement;
            forceA.Alignment.DarkSideAttunement = forceB.Alignment.DarkSideAttunement;
            forceA.Alignment.LightSideAttunement = forceB.Alignment.LightSideAttunement;
            forceB.Alignment.DarkSideAttunement = tempDark;
            forceB.Alignment.LightSideAttunement = tempLight;

            // Swap unlocked abilities
            var tempUnlocked = new HashSet<string>(forceA.unlockedAbiliities);
            forceA.unlockedAbiliities = new HashSet<string>(forceB.unlockedAbiliities);
            forceB.unlockedAbiliities = new HashSet<string>(tempUnlocked);

            // Clean up abilities from both pawns
            CleanupForceAbilities(pawnA);
            CleanupForceAbilities(pawnB);

            // Swap ability presets
            var tempPresets = new Dictionary<string, AbilityPreset>(forceA.Abilities.abilityPresets);
            forceA.Abilities.abilityPresets = new Dictionary<string, AbilityPreset>();
            foreach (var preset in forceB.Abilities.abilityPresets)
            {
                forceA.Abilities.abilityPresets[preset.Key] = new AbilityPreset
                {
                    presetName = preset.Value.presetName,
                    activeAbilities = new HashSet<string>(preset.Value.activeAbilities)
                };
            }

            forceB.Abilities.abilityPresets = new Dictionary<string, AbilityPreset>();
            foreach (var preset in tempPresets)
            {
                forceB.Abilities.abilityPresets[preset.Key] = new AbilityPreset
                {
                    presetName = preset.Value.presetName,
                    activeAbilities = new HashSet<string>(preset.Value.activeAbilities)
                };
            }

            // Swap current preset
            string tempCurrentPreset = forceA.Abilities.currentPreset;
            forceA.Abilities.currentPreset = forceB.Abilities.currentPreset;
            forceB.Abilities.currentPreset = tempCurrentPreset;

            // Ensure presets are valid after swap
            forceA.Abilities.EnsureDefaultPreset();
            forceB.Abilities.EnsureDefaultPreset();

            // Update pawn abilities
            forceA.Abilities.UpdatePawnAbilities();
            forceB.Abilities.UpdatePawnAbilities();

            // Refresh gizmos
            forceA.Abilities.RefreshAbilityGizmos();
            forceB.Abilities.RefreshAbilityGizmos();
        }

        private static void CleanupForceAbilities(Pawn pawn)
        {
            var forceComp = pawn.GetComp<CompClass_ForceUser>();
            if (forceComp == null) return;

            var abilitiesToRemove = new List<Ability>();
            foreach (var ability in pawn.abilities.AllAbilitiesForReading)
            {
                if (ability.def.HasModExtension<ForceAbilityDefExtension>())
                {
                    abilitiesToRemove.Add(ability);
                }
            }

            foreach (var ability in abilitiesToRemove)
            {
                pawn.abilities.RemoveAbility(ability.def);
            }
        }

        private static void NotifySwapComplete(Pawn pawnA, Pawn pawnB)
        {
            Find.LetterStack.ReceiveLetter(
                "Force.IdentitySwapComplete".Translate(),
                "Force.IdentitySwapSuccess".Translate(
                    pawnA.Named("PAWNA"),
                    pawnB.Named("PAWNB")
                ),
                LetterDefOf.NeutralEvent,
                new List<Pawn> { pawnA, pawnB }
            );
        }

        // Additional utility methods
        public static bool CanSwapWithPawn(Pawn source, Pawn target)
        {
            if (source == null || target == null) return false;
            if (target.Dead) return false;
            if (ForceGhostUtility.IsForceGhost(target)) return false;

            return true;
        }

        public static void ExtractPawnDataToContainer(Pawn source, Pawn container)
        {
            if (source == null || container == null) return;

            SwapPawnIdentities(source, container);
        }

        public static void ApplyPawnDataFromContainer(Pawn target, Pawn container)
        {
            if (target == null || container == null) return;

            SwapPawnIdentities(target, container);
        }
    }
}
