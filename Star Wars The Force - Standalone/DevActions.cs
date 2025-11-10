using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TheForce_Standalone
{
    public class DevActions_ForceUser
    {
        [DebugAction("The Force", "Force Level Up", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceLevelUp()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
            {
                CompClass_ForceUser forceUser = pawn.GetComp<CompClass_ForceUser>();
                if (forceUser != null)
                {
                    list.Add(new FloatMenuOption(pawn.LabelShortCap, () =>
                    {
                        forceUser.Leveling.LevelUp(1);
                        Messages.Message($"Forced level up for {pawn.LabelShortCap} to level {forceUser.forceLevel}", MessageTypeDefOf.PositiveEvent);
                    }));
                }
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        [DebugAction("The Force", "Add Ability Point", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AddAbilityPoint()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
            {
                CompClass_ForceUser forceUser = pawn.GetComp<CompClass_ForceUser>();
                if (forceUser != null)
                {
                    list.Add(new FloatMenuOption(pawn.LabelShortCap, () =>
                    {
                        forceUser.Abilities.AddAbilityPoint(1);
                        Messages.Message($"Added ability point to {pawn.LabelShortCap} (Total: {forceUser.Abilities.AvailableAbilityPoints})", MessageTypeDefOf.PositiveEvent);
                    }));
                }
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        [DebugAction("The Force", "Check FP Status", allowedGameStates = AllowedGameStates.Playing)]
        private static void CheckFPStatus()
        {
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(Options()));
        }

        [DebugAction("The Force", "Clear All Cache", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearAllCache()
        {
            ForceSensitivityUtils.ClearAllCache();
            Messages.Message("Cleared all Force-related caches", MessageTypeDefOf.PositiveEvent);
        }

        private static List<DebugMenuOption> Options()
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (Pawn pawn in Find.CurrentMap.mapPawns.AllPawns)
            {
                var forceUser = pawn.TryGetComp<CompClass_ForceUser>();
                if (forceUser != null)
                {
                    list.Add(new DebugMenuOption(pawn.LabelShortCap, DebugMenuOptionMode.Action, () =>
                    {
                        Messages.Message($"{pawn.LabelShortCap} FP: {forceUser.currentFP}/{forceUser.MaxFP}", MessageTypeDefOf.NeutralEvent);
                    }));
                }
            }
            return list;
        }

        [DebugAction("The Force", "Reset Force User (Complete)", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ResetForceUserComplete()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
            {
                CompClass_ForceUser forceUser = pawn.GetComp<CompClass_ForceUser>();
                if (forceUser != null && forceUser.IsValidForceUser)
                {
                    list.Add(new FloatMenuOption(pawn.LabelShortCap, () =>
                    {
                        // Store original values for reference
                        int originalForceLevel = forceUser.forceLevel;
                        float originalCurrentFP = forceUser.currentFP;
                        int originalAbilityPoints = forceUser.Abilities.AvailableAbilityPoints;
                        int originalUnlockedAbilities = forceUser.unlockedAbiliities?.Count ?? 0;

                        // Complete reset
                        forceUser.currentFP = 0f;
                        forceUser.forceLevel = 1;
                        
                        forceUser.unlockedAbiliities?.Clear();
                        forceUser.isInitialized = false;
                        forceUser.Abilities.abilityPresets?.Clear();
                        forceUser.Abilities.currentPreset = "Default";

                        // Reset subsystems
                        forceUser.Alignment?.Reset();
                        forceUser.Leveling?.Reset();
                        forceUser.Abilities?.Reset();

                        // Reinitialize
                        forceUser.RecalculateMaxFP();
                        forceUser.RecoverFP(forceUser.MaxFP);
                        forceUser.Abilities.EnsureDefaultPreset();

                        // Add starting ability point
                        forceUser.Abilities.AddAbilityPoint(1);

                        // Force reinitialization
                        forceUser.isInitialized = true;

                        Messages.Message($"Completely reset {pawn.LabelShortCap}'s Force abilities. " +
                                       $"Level: {originalForceLevel}→1, FP: {originalCurrentFP}→{forceUser.currentFP}, " +
                                       $"Abilities: {originalUnlockedAbilities}→0, Points: {originalAbilityPoints}→1",
                                       MessageTypeDefOf.PositiveEvent);
                    }));
                }
            }

            if (list.Count == 0)
            {
                list.Add(new FloatMenuOption("No valid Force users found", null));
            }

            Find.WindowStack.Add(new FloatMenu(list));
        }

        [DebugAction("The Force", "Log Force Caches", allowedGameStates = AllowedGameStates.Playing)]
        private static void LogForceCaches()
        {
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(LogCacheOptions()));
        }

        [DebugAction("Pawns", "Remove Ability (All Types)", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void RemoveAnyAbilityDevAction()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            foreach (Pawn pawn in Find.CurrentMap.mapPawns.AllPawns)
            {
                if (pawn.abilities != null && pawn.abilities.AllAbilitiesForReading.Count > 0)
                {
                    list.Add(new FloatMenuOption(pawn.LabelShortCap + $" ({pawn.abilities.AllAbilitiesForReading.Count} total abilities)", () =>
                    {
                        List<FloatMenuOption> abilityList = new List<FloatMenuOption>();

                        // Show permanent abilities first
                        if (pawn.abilities.abilities.Count > 0)
                        {
                            abilityList.Add(new FloatMenuOption("--- Permanent Abilities ---", null));
                            foreach (Ability ability in pawn.abilities.abilities)
                            {
                                abilityList.Add(new FloatMenuOption($"{ability.def.LabelCap} (Permanent)", () =>
                                {
                                    // Check if this is a Force ability before removing
                                    CompClass_ForceUser forceUser = pawn.GetComp<CompClass_ForceUser>();
                                    bool wasForceAbility = false;

                                    if (forceUser != null && forceUser.unlockedAbiliities != null)
                                    {
                                        wasForceAbility = forceUser.unlockedAbiliities.Contains(ability.def.defName);
                                        if (wasForceAbility)
                                        {
                                            forceUser.unlockedAbiliities.Remove(ability.def.defName);
                                        }
                                    }

                                    // Remove from pawn's ability tracker
                                    pawn.abilities.RemoveAbility(ability.def);

                                    string message = wasForceAbility
                                        ? $"Removed Force ability {ability.def.LabelCap} from {pawn.LabelShortCap}"
                                        : $"Removed permanent ability {ability.def.LabelCap} from {pawn.LabelShortCap}";

                                    Messages.Message(message, MessageTypeDefOf.NeutralEvent);
                                }));
                            }
                        }
                        // Show temporary abilities from hediffs, equipment, etc.
                        List<Ability> tempAbilities = pawn.abilities.AllAbilitiesForReading
                            .Where(a => !pawn.abilities.abilities.Contains(a))
                            .ToList();

                        if (tempAbilities.Count > 0)
                        {
                            abilityList.Add(new FloatMenuOption("--- Temporary Abilities ---", null));

                            foreach (Ability ability in tempAbilities)
                            {
                                string source = "Unknown";

                                // Try to determine source
                                if (pawn.health?.hediffSet?.hediffs != null)
                                {
                                    foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                                    {
                                        if (hediff.AllAbilitiesForReading.Contains(ability))
                                        {
                                            source = $"Hediff: {hediff.def.LabelCap}";
                                            break;
                                        }
                                    }
                                }

                                if (source == "Unknown" && pawn.equipment?.Primary != null)
                                {
                                    var comp = pawn.equipment.Primary.TryGetComp<CompEquippableAbility>();
                                    if (comp?.AbilityForReading == ability)
                                    {
                                        source = $"Weapon: {pawn.equipment.Primary.LabelCap}";
                                    }
                                }

                                if (source == "Unknown" && pawn.apparel?.WornApparel != null)
                                {
                                    foreach (Apparel apparel in pawn.apparel.WornApparel)
                                    {
                                        if (apparel.AllAbilitiesForReading.Contains(ability))
                                        {
                                            source = $"Apparel: {apparel.LabelCap}";
                                            break;
                                        }
                                    }
                                }

                                abilityList.Add(new FloatMenuOption($"{ability.def.LabelCap} ({source})", () =>
                                {
                                    Messages.Message($"Cannot remove temporary ability {ability.def.LabelCap} - remove the source instead", MessageTypeDefOf.CautionInput);
                                }));
                            }
                        }

                        if (abilityList.Count == 0)
                        {
                            abilityList.Add(new FloatMenuOption("No abilities found", null));
                        }

                        Find.WindowStack.Add(new FloatMenu(abilityList));
                    }));
                }
            }

            if (list.Count == 0)
            {
                list.Add(new FloatMenuOption("No pawns with abilities found", null));
            }

            Find.WindowStack.Add(new FloatMenu(list));
        }

        private static List<DebugMenuOption> LogCacheOptions()
        {
            return new List<DebugMenuOption>
    {
        new DebugMenuOption("Log TraitDefs Cache", DebugMenuOptionMode.Action, () => LogTraitDefsCache()),
        new DebugMenuOption("Log HediffDefs Cache", DebugMenuOptionMode.Action, () => LogHediffDefsCache()),
        new DebugMenuOption("Log GeneDefs Cache", DebugMenuOptionMode.Action, () => LogGeneDefsCache()),
        new DebugMenuOption("Log PawnKindDefs Cache", DebugMenuOptionMode.Action, () => LogPawnKindDefsCache()),
        new DebugMenuOption("Log Force Abilities", DebugMenuOptionMode.Action, () => LogForceAbilities()), // New option
        new DebugMenuOption("Log All Caches", DebugMenuOptionMode.Action, () => LogAllCaches())
    };
        }

        private static void LogTraitDefsCache()
        {
            Log.Message("=== Force TraitDefs Cache ===");
            if (ForceSensitivityUtils.ForceTraitDefs.Count == 0)
            {
                Log.Message("Cache is empty");
                return;
            }

            foreach (var traitDef in ForceSensitivityUtils.ForceTraitDefs)
            {
                Log.Message($"- {traitDef.defName} ({traitDef.LabelCap})");
            }
            Log.Message($"Total: {ForceSensitivityUtils.ForceTraitDefs.Count} entries");
        }

        private static void LogHediffDefsCache()
        {
            Log.Message("=== Force HediffDefs Cache ===");
            if (ForceSensitivityUtils.ForceHediffDefs.Count == 0)
            {
                Log.Message("Cache is empty");
                return;
            }

            foreach (var hediffDef in ForceSensitivityUtils.ForceHediffDefs)
            {
                Log.Message($"- {hediffDef.defName} ({hediffDef.LabelCap})");
            }
            Log.Message($"Total: {ForceSensitivityUtils.ForceHediffDefs.Count} entries");
        }

        private static void LogGeneDefsCache()
        {
            Log.Message("=== Force GeneDefs Cache ===");
            if (ForceSensitivityUtils.ForceGeneDefs.Count == 0)
            {
                Log.Message("Cache is empty");
                return;
            }

            foreach (var geneDef in ForceSensitivityUtils.ForceGeneDefs)
            {
                Log.Message($"- {geneDef.defName} ({geneDef.LabelCap})");
            }
            Log.Message($"Total: {ForceSensitivityUtils.ForceGeneDefs.Count} entries");
        }

        private static void LogPawnKindDefsCache()
        {
            Log.Message("=== Force PawnKindDefs Cache ===");
            if (ForceSensitivityUtils.ForcePawnKindDefs.Count == 0)
            {
                Log.Message("Cache is empty");
                return;
            }

            foreach (var pawnKindDef in ForceSensitivityUtils.ForcePawnKindDefs)
            {
                Log.Message($"- {pawnKindDef.defName} ({pawnKindDef.LabelCap})");
            }
            Log.Message($"Total: {ForceSensitivityUtils.ForcePawnKindDefs.Count} entries");
        }

        private static void LogForceAbilities()
        {
            Log.Message("=== Force Abilities ===");

            var forceAbilities = DefDatabase<AbilityDef>.AllDefs
                .Where(ad => ad.GetModExtension<ForceAbilityDefExtension>() != null)
                .ToList();

            if (forceAbilities.Count == 0)
            {
                Log.Message("No Force abilities found");
                return;
            }

            // Group by category for better organization
            var abilitiesByCategory = forceAbilities
                .GroupBy(ad => ad.category?.label ?? "Misc")
                .OrderBy(g => g.Key);

            foreach (var categoryGroup in abilitiesByCategory)
            {
                Log.Message($"--- {categoryGroup.Key} ---");

                foreach (var abilityDef in categoryGroup.OrderBy(ad => ad.level))
                {
                    var ext = abilityDef.GetModExtension<ForceAbilityDefExtension>();
                    StringBuilder requirements = new StringBuilder();

                    if (ext != null)
                    {
                        if (ext.RequiredLevel > 0)
                            requirements.Append($"Level {ext.RequiredLevel} ");

                        if (ext.requiredAbilities != null && ext.requiredAbilities.Any())
                            requirements.Append($"Prereqs: {string.Join(", ", ext.requiredAbilities.Select(ra => ra.defName))} ");

                        if (ext.requiredTraits != null && ext.requiredTraits.Any())
                            requirements.Append($"Traits: {string.Join(", ", ext.requiredTraits.Select(rt => rt.defName))} ");

                        if (ext.requiredHediffs != null && ext.requiredHediffs.Any())
                            requirements.Append($"Hediffs: {string.Join(", ", ext.requiredHediffs.Select(rh => rh.defName))} ");

                        if (ext.requiredAlignment != null)
                            requirements.Append($"Alignment: {ext.requiredAlignment.alignmentType} {ext.requiredAlignment.value} ");
                    }

                    Log.Message($"- {abilityDef.defName} (Level: {abilityDef.level}) - {abilityDef.LabelCap}");
                    if (requirements.Length > 0)
                        Log.Message($"  Requirements: {requirements}");
                    if (!string.IsNullOrEmpty(abilityDef.description))
                        Log.Message($"  Description: {abilityDef.description.Truncate(100)}");
                }
            }

            Log.Message($"Total: {forceAbilities.Count} Force abilities across {abilitiesByCategory.Count()} categories");
        }

        private static void LogAllCaches()
        {
            LogTraitDefsCache();
            LogHediffDefsCache();
            LogGeneDefsCache();
            LogPawnKindDefsCache();
            LogForceAbilities(); // Added here
            Log.Message("=== All caches logged ===");
        }
    }
}
