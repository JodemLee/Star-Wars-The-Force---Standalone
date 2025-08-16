using RimWorld;
using System.Collections.Generic;
using System.Linq;
using TheForce_Standalone.Alignment;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    public class ForceUserAbilities
    {
        private readonly CompClass_ForceUser parent;
        private Dictionary<AbilityDef, bool> alreadyHadAbilities = new Dictionary<AbilityDef, bool>();
        private bool onGlobalCooldown = false;
        private int globalCooldownTicksRemaining = 0;

        private int availableAbilityPoints;
        public int AvailableAbilityPoints
        {
            get => availableAbilityPoints;
            private set => availableAbilityPoints = value;
        }
        public bool OnGlobalCooldown => onGlobalCooldown;

        public ForceUserAbilities(CompClass_ForceUser parent)
        {
            this.parent = parent;
        }

        public void Initialize()
        {
            var forceUserExt = parent.Pawn.kindDef?.GetModExtension<ModExtension_ForceUser>();
            if (forceUserExt != null)
            {
                AvailableAbilityPoints = forceUserExt.abilityPointsToGrant;
                if (forceUserExt.grantRandomAbilities && forceUserExt.abilityCategories != null)
                {
                    GrantRandomAbilities(forceUserExt);
                }
            }
        }

        public bool CanUnlockAbility(AbilityDef abilityDef)
        {
            if (abilityDef == null)
            {
                Log.Error("Null AbilityDef in CanUnlockAbility");
                return false;
            }

            var forceExt = abilityDef.GetModExtension<ForceAbilityDefExtension>();

            if (!parent.IsValidForceUser || parent.AvailableAbilityPoints <= 0 || parent.unlockedAbiliities.Contains(abilityDef.defName))
                return false;

            if (forceExt == null)
                return false;

            // Level requirement
            if (parent.forceLevel < forceExt.RequiredLevel)
                return false;

            // Check abilities (ANY or ALL)
            if (forceExt.requiredAbilities != null && forceExt.requiredAbilities.Count > 0)
            {
                bool meetsAbilities = forceExt.requiredAbilities == null ||
                (forceExt.requireAllAbilities
                    ? forceExt.requiredAbilities.All(req => parent.unlockedAbiliities.Contains(req.defName)) // ALL
                    : forceExt.requiredAbilities.Any(req => parent.unlockedAbiliities.Contains(req.defName))); // ANY

                if (!meetsAbilities)
                    return false;
            }

            // Check traits (ANY or ALL)
            if (forceExt.requiredTraits != null && forceExt.requiredTraits.Count > 0)
            {
                bool meetsTraits = forceExt.requireAllTraits
                    ? forceExt.requiredTraits.All(t => parent.Pawn.story.traits.HasTrait(t)) // ALL
                    : forceExt.requiredTraits.Any(t => parent.Pawn.story.traits.HasTrait(t)); // ANY

                if (!meetsTraits)
                    return false;
            }

            // Check hediffs (ANY or ALL)
            if (forceExt.requiredHediffs != null && forceExt.requiredHediffs.Count > 0)
            {
                bool meetsHediffs = forceExt.requireAllHediffs
                    ? forceExt.requiredHediffs.All(h => parent.Pawn.health.hediffSet.HasHediff(h)) // ALL
                    : forceExt.requiredHediffs.Any(h => parent.Pawn.health.hediffSet.HasHediff(h)); // ANY

                if (!meetsHediffs)
                    return false;
            }

            // Alignment check
            if (forceExt.requiredAlignment != null)
            {
                float currentAlignment = forceExt.requiredAlignment.alignmentType == AlignmentType.Lightside
                    ? parent.Alignment.LightSideAttunement
                    : parent.Alignment.DarkSideAttunement;

                if (currentAlignment < forceExt.requiredAlignment.value)
                    return false;
            }

            // Only return true if all checks passed
            return true;
        }

        public bool TryUnlockAbility(AbilityDef abilityDef)
        {
            if (!CanUnlockAbility(abilityDef))
                return false;

            parent.unlockedAbiliities.Add(abilityDef.defName);
            AvailableAbilityPoints--;



            if (parent != null)
                UpdatePawnAbilities();

            return true;
        }

        public void AddAbilityPoint(int amount = 1)
        {
            AvailableAbilityPoints += amount;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref availableAbilityPoints, "availableAbilityPoints", 0);
            Scribe_Collections.Look(ref alreadyHadAbilities, "alreadyHadAbilities", LookMode.Def, LookMode.Value);
            Scribe_Values.Look(ref onGlobalCooldown, "onGlobalCooldown", false);
            Scribe_Values.Look(ref globalCooldownTicksRemaining, "globalCooldownTicksRemaining", 0);
        }

        public void GrantRandomAbilities(ModExtension_ForceUser ext)
        {
            var eligibleAbilities = DefDatabase<AbilityDef>.AllDefs
                .Where(ability => ext.abilityCategories.Contains(ability.category))
                .Where(ability => parent.forceLevel >= ability.GetModExtension<ForceAbilityDefExtension>()?.RequiredLevel)
                .ToList();

            int abilitiesToGrant = Mathf.Clamp(
                Rand.RangeInclusive(ext.minRandomAbilities, ext.maxRandomAbilities),
                0,
                parent.forceLevel - parent.unlockedAbiliities.Count
            );

            for (int i = 0; i < abilitiesToGrant; i++)
            {
                if (Rand.Value > ext.abilityChance) continue;
                var selectedAbility = eligibleAbilities.RandomElement();
                parent.unlockedAbiliities.Add(selectedAbility.defName);
            }

            UpdatePawnAbilities();
        }

        public void UpdatePawnAbilities()
        {
            if (!parent.IsValidForceUser || parent.Pawn.abilities == null) return;

            var abilitiesToMaintain = new HashSet<AbilityDef>();
            foreach (var abilityDefName in parent.unlockedAbiliities)
            {
                var abilityDef = DefDatabase<AbilityDef>.GetNamedSilentFail(abilityDefName);
                if (abilityDef != null)
                {
                    if (!alreadyHadAbilities.ContainsKey(abilityDef))
                    {
                        alreadyHadAbilities[abilityDef] = parent.Pawn.abilities.abilities.Any(a => a.def == abilityDef);
                    }
                    if (!alreadyHadAbilities[abilityDef])
                    {
                        parent.Pawn.abilities.GainAbility(abilityDef);
                    }
                    abilitiesToMaintain.Add(abilityDef);
                }
            }

            // Remove abilities no longer maintained
            foreach (var ability in parent.Pawn.abilities.abilities.ToList())
            {
                if (ability.def.GetModExtension<ForceAbilityDefExtension>() != null &&
                    !abilitiesToMaintain.Contains(ability.def) &&
                    (!alreadyHadAbilities.TryGetValue(ability.def, out var hadBefore) || !hadBefore))
                {
                    parent.Pawn.abilities.RemoveAbility(ability.def);
                }
            }
        }

        public void StartGlobalCooldown(int ticks)
        {
            onGlobalCooldown = true;
            globalCooldownTicksRemaining = ticks;
        }

        public void CompTickInterval(int delta)
        {
            if (onGlobalCooldown)
            {
                globalCooldownTicksRemaining -= delta;
                if (globalCooldownTicksRemaining <= 0)
                {
                    onGlobalCooldown = false;
                }
            }
        }
    }
}
