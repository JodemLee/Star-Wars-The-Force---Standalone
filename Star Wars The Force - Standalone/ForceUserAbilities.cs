using RimWorld;
using System;
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

        private int availableAbilityPoints = 1;
        public int AvailableAbilityPoints
        {
            get => availableAbilityPoints;
            private set => availableAbilityPoints = value;
        }
        public bool OnGlobalCooldown => onGlobalCooldown;

        // Ability preset system
        public Dictionary<string, AbilityPreset> abilityPresets = new Dictionary<string, AbilityPreset>();
        public string currentPreset = "Default";

        public ForceUserAbilities(CompClass_ForceUser parent)
        {
            this.parent = parent;
        }

        public void Reset()
        {
            availableAbilityPoints = 0;
        }

        public bool CanUnlockAbility(AbilityDef abilityDef)
        {
            // Enhanced null checking
            if (abilityDef == null)
            {
                Log.Error("Null AbilityDef in CanUnlockAbility");
                return false;
            }

            if (parent == null || !parent.IsValidForceUser)
            {
                Log.Warning("Invalid parent or force user in CanUnlockAbility");
                return false;
            }

            // Check available points and if already unlocked
            if (AvailableAbilityPoints <= 0)
                return false;

            if (parent.unlockedAbiliities == null)
            {
                Log.Warning("unlockedAbiliities is null in CanUnlockAbility");
                return false;
            }

            if (parent.unlockedAbiliities.Contains(abilityDef.defName))
                return false;

            var forceExt = abilityDef.GetModExtension<ForceAbilityDefExtension>();
            if (forceExt == null)
                return false;

            // Level requirement
            if (parent.forceLevel < forceExt.RequiredLevel)
                return false;

            // Check pawn reference
            if (parent.Pawn == null)
            {
                Log.Warning("Pawn is null in CanUnlockAbility");
                return false;
            }

            // Check abilities (ANY or ALL)
            if (forceExt.requiredAbilities != null && forceExt.requiredAbilities.Count > 0)
            {
                bool meetsAbilities = forceExt.requireAllAbilities
                    ? forceExt.requiredAbilities.All(req => req != null && parent.unlockedAbiliities.Contains(req.defName)) // ALL
                    : forceExt.requiredAbilities.Any(req => req != null && parent.unlockedAbiliities.Contains(req.defName)); // ANY

                if (!meetsAbilities)
                    return false;
            }

            // Check traits (ANY or ALL)
            if (forceExt.requiredTraits != null && forceExt.requiredTraits.Count > 0)
            {
                if (parent.Pawn.story?.traits == null)
                    return false;

                bool meetsTraits = forceExt.requireAllTraits
                    ? forceExt.requiredTraits.All(t => t != null && parent.Pawn.story.traits.HasTrait(t)) // ALL
                    : forceExt.requiredTraits.Any(t => t != null && parent.Pawn.story.traits.HasTrait(t)); // ANY

                if (!meetsTraits)
                    return false;
            }

            // Check hediffs (ANY or ALL)
            if (forceExt.requiredHediffs != null && forceExt.requiredHediffs.Count > 0)
            {
                if (parent.Pawn.health?.hediffSet == null)
                    return false;

                bool meetsHediffs = forceExt.requireAllHediffs
                    ? forceExt.requiredHediffs.All(h => h != null && parent.Pawn.health.hediffSet.HasHediff(h)) // ALL
                    : forceExt.requiredHediffs.Any(h => h != null && parent.Pawn.health.hediffSet.HasHediff(h)); // ANY

                if (!meetsHediffs)
                    return false;
            }

            // Alignment check
            if (forceExt.requiredAlignment != null)
            {
                if (parent.Alignment == null)
                    return false;

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
            if (!CanUnlockAbility(abilityDef) || AvailableAbilityPoints <= 0)
                return false;

            parent.unlockedAbiliities.Add(abilityDef.defName);
            AvailableAbilityPoints--;

            // Auto-add to current preset when unlocked
            OnAbilityUnlocked(abilityDef.defName);
            UpdatePawnAbilities();

            return true;
        }

        public void AddAbilityPoint(int amount = 1)
        {
            AvailableAbilityPoints += amount;
        }

        public void Initialize()
        {
            if (parent?.Pawn == null) return;

            // Initialize the default preset FIRST with all abilities active
            EnsureDefaultPreset();
            parent.unlockedAbiliities = new HashSet<string>();
            var forceUserExt = parent.Pawn.kindDef?.GetModExtension<ModExtension_ForceUser>();
            if (forceUserExt != null)
            {
                AvailableAbilityPoints = forceUserExt.abilityPointsToGrant;
                if (forceUserExt.grantRandomAbilities && forceUserExt.abilityCategories != null)
                {
                    GrantRandomAbilities(forceUserExt);
                }
                UnlockPawnKindAbilities();
            }

            // After unlocking abilities, ensure they're all active in the default preset
            SyncDefaultPresetWithUnlockedAbilities();
            UpdatePawnAbilities();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref availableAbilityPoints, "availableAbilityPoints", 0);
            Scribe_Collections.Look(ref alreadyHadAbilities, "alreadyHadAbilities", LookMode.Def, LookMode.Value);
            Scribe_Values.Look(ref onGlobalCooldown, "onGlobalCooldown", false);
            Scribe_Values.Look(ref globalCooldownTicksRemaining, "globalCooldownTicksRemaining", 0);
            Scribe_Collections.Look(ref abilityPresets, "abilityPresets", LookMode.Value, LookMode.Deep);
            Scribe_Values.Look(ref currentPreset, "currentPreset", "Default");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureDefaultPreset();
                // After loading, sync the default preset with unlocked abilities
                SyncDefaultPresetWithUnlockedAbilities();
                UpdatePawnAbilities();
            }
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

                // Add to default preset to ensure it's active for new pawns
                if (abilityPresets.TryGetValue("Default", out var defaultPreset))
                {
                    defaultPreset.activeAbilities.Add(selectedAbility.defName);
                }
            }

            UpdatePawnAbilities();
        }

        public void UnlockPawnKindAbilities()
        {
            if (parent.Pawn?.kindDef?.abilities == null)
                return;

            foreach (var abilityDef in parent.Pawn.kindDef.abilities)
            {
                if (parent.unlockedAbiliities.Contains(abilityDef.defName))
                    continue;

                var forceExtension = abilityDef.GetModExtension<ForceAbilityDefExtension>();
                if (forceExtension != null && parent.forceLevel >= forceExtension.RequiredLevel)
                {
                    parent.unlockedAbiliities.Add(abilityDef.defName);

                    // Add to default preset to ensure it's active for new pawns
                    if (abilityPresets.TryGetValue("Default", out var defaultPreset))
                    {
                        defaultPreset.activeAbilities.Add(abilityDef.defName);
                    }
                }
            }

            UpdatePawnAbilities();
        }

        public void UpdatePawnAbilities()
        {
            try
            {
                // Additional null safety checks
                if (parent == null || !parent.IsValidForceUser || parent.Pawn?.abilities == null)
                    return;

                if (parent.unlockedAbiliities == null)
                    return;

                var abilitiesToMaintain = new HashSet<AbilityDef>();
                foreach (var abilityDefName in parent.unlockedAbiliities)
                {
                    if (string.IsNullOrEmpty(abilityDefName))
                        continue;

                    var abilityDef = DefDatabase<AbilityDef>.GetNamedSilentFail(abilityDefName);
                    if (abilityDef != null)
                    {
                        if (alreadyHadAbilities == null)
                            alreadyHadAbilities = new Dictionary<AbilityDef, bool>();

                        if (!alreadyHadAbilities.ContainsKey(abilityDef))
                        {
                            alreadyHadAbilities[abilityDef] = parent.Pawn.abilities.abilities != null &&
                                                             parent.Pawn.abilities.abilities.Any(a => a.def == abilityDef);
                        }
                        if (!alreadyHadAbilities[abilityDef])
                        {
                            parent.Pawn.abilities.GainAbility(abilityDef);
                        }
                        abilitiesToMaintain.Add(abilityDef);
                    }
                }
            }
            catch (System.NullReferenceException ex)
            {
                Log.Error($"NullReferenceException in UpdatePawnAbilities: {ex.Message}\n{ex.StackTrace}");
            }
            catch (System.Exception ex)
            {
                Log.Error($"Unexpected error in UpdatePawnAbilities: {ex.Message}\n{ex.StackTrace}");
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

        // ========== PRESET SYSTEM METHODS ==========

        public void OnAbilityUnlocked(string abilityDefName)
        {
            EnsureDefaultPreset();

            // Add the new ability to the current preset only
            if (abilityPresets.TryGetValue(currentPreset, out var preset) && preset != null)
            {
                if (preset.activeAbilities == null)
                {
                    preset.activeAbilities = new HashSet<string>();
                }
                preset.activeAbilities.Add(abilityDefName);
            }

            // Also add to default preset to ensure new pawns get it active
            if (abilityPresets.TryGetValue("Default", out var defaultPreset) && defaultPreset != null)
            {
                if (defaultPreset.activeAbilities == null)
                {
                    defaultPreset.activeAbilities = new HashSet<string>();
                }
                defaultPreset.activeAbilities.Add(abilityDefName);
            }

            RefreshAbilityGizmos();
        }

        public bool IsAbilityActive(string abilityDefName)
        {
            if (parent.unlockedAbiliities == null || abilityPresets == null)
                return false;
            if (!parent.unlockedAbiliities.Contains(abilityDefName))
                return false;
            if (abilityPresets.TryGetValue(currentPreset, out var preset) && preset != null)
            {
                return preset.activeAbilities != null && preset.activeAbilities.Contains(abilityDefName);
            }
            return false;
        }

        public void SaveCurrentAsPreset(string presetName)
        {
            var newPreset = new AbilityPreset
            {
                presetName = presetName,
                activeAbilities = new HashSet<string>()
            };

            // Only copy the currently ACTIVE abilities from the current preset
            if (abilityPresets.TryGetValue(currentPreset, out var currentPresetObj) && currentPresetObj != null)
            {
                foreach (var ability in currentPresetObj.activeAbilities)
                {
                    newPreset.activeAbilities.Add(ability);
                }
            }

            abilityPresets[presetName] = newPreset;
            currentPreset = presetName;
            RefreshAbilityGizmos();
        }

        public void RefreshAbilityGizmos()
        {
            if (parent.Pawn?.abilities != null)
            {
                // This will force a refresh of all ability gizmos
                foreach (var ability in parent.Pawn.abilities.AllAbilitiesForReading)
                {
                    ability.GizmosVisible();
                }
            }
        }

        public void ToggleAbilityInPreset(string abilityDefName, bool active)
        {
            if (!parent.unlockedAbiliities.Contains(abilityDefName))
            {
                Log.Warning($"Cannot toggle {abilityDefName} - not unlocked!");
                return;
            }

            if (abilityPresets.TryGetValue(currentPreset, out var preset))
            {
                if (active)
                {
                    if (preset.activeAbilities.Add(abilityDefName))
                    {
                        // Ability activated
                    }
                }
                else
                {
                    if (preset.activeAbilities.Remove(abilityDefName))
                    {
                        // Ability deactivated
                    }
                }
                RefreshAbilityGizmos();
            }
            else
            {
                Log.Error($"Current preset '{currentPreset}' not found in abilityPresets!");
            }
        }

        public bool LoadPreset(string presetName)
        {
            if (abilityPresets.TryGetValue(presetName, out var preset))
            {
                currentPreset = presetName;
                RefreshAbilityGizmos();
                return true;
            }
            return false;
        }

        public IEnumerable<string> GetActiveAbilities()
        {
            foreach (var ability in parent.unlockedAbiliities)
            {
                if (IsAbilityActive(ability))
                {
                    yield return ability;
                }
            }
        }

        public void DeletePreset(string presetName)
        {
            if (abilityPresets.ContainsKey(presetName))
            {
                abilityPresets.Remove(presetName);
                if (currentPreset == presetName)
                {
                    currentPreset = abilityPresets.Keys.FirstOrDefault() ?? "Default";
                }
            }
        }

        public void EnsureDefaultPreset()
        {
            try
            {
                if (abilityPresets == null)
                {
                    abilityPresets = new Dictionary<string, AbilityPreset>();
                }

                // Create default preset if it doesn't exist
                if (!abilityPresets.ContainsKey("Default"))
                {
                    var defaultPreset = new AbilityPreset
                    {
                        presetName = "Default",
                        activeAbilities = new HashSet<string>()
                    };
                    abilityPresets["Default"] = defaultPreset;
                }

                if (string.IsNullOrEmpty(currentPreset) || !abilityPresets.ContainsKey(currentPreset))
                {
                    currentPreset = "Default";
                }

                // Ensure preset collections are not null
                foreach (var preset in abilityPresets.Values.ToList())
                {
                    if (preset == null)
                    {
                        continue;
                    }

                    if (preset.activeAbilities == null)
                    {
                        preset.activeAbilities = new HashSet<string>();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in EnsureDefaultPreset for {parent.Pawn?.LabelShort}: {ex}");
                abilityPresets = new Dictionary<string, AbilityPreset>();
                parent.unlockedAbiliities = new HashSet<string>();
                currentPreset = "Default";

                var defaultPreset = new AbilityPreset
                {
                    presetName = "Default",
                    activeAbilities = new HashSet<string>()
                };
                abilityPresets["Default"] = defaultPreset;
            }
        }

        private void SyncDefaultPresetWithUnlockedAbilities()
        {
            if (abilityPresets.TryGetValue("Default", out var defaultPreset) && defaultPreset != null)
            {
                // Clear and repopulate with all currently unlocked abilities
                defaultPreset.activeAbilities.Clear();
                if (parent.unlockedAbiliities != null)
                {
                    foreach (var ability in parent.unlockedAbiliities)
                    {
                        defaultPreset.activeAbilities.Add(ability);
                    }
                }
            }
        }
    }

    public class AbilityPreset : IExposable
    {
        public string presetName;
        public HashSet<string> activeAbilities;

        public AbilityPreset()
        {
            activeAbilities = new HashSet<string>();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref presetName, "presetName");
            Scribe_Collections.Look(ref activeAbilities, "activeAbilities", LookMode.Value);
        }
    }
}