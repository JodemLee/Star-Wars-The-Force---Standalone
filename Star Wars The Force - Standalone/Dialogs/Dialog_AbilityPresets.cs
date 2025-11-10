using RimWorld;
using System.Collections.Generic;
using System.Linq;
using TheForce_Standalone.Dialogs;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    public class Dialog_AbilityPresets : Window
    {
        private CompClass_ForceUser forceUser;
        private string newPresetName = "";
        private Vector2 scrollPosition;
        private string selectedPreset;
        private Dictionary<string, bool> expandedPresets = new Dictionary<string, bool>();

        // Property to get the correct ability presets
        private Dictionary<string, AbilityPreset> AbilityPresets => forceUser.Abilities.abilityPresets;
        private string CurrentPreset => forceUser.Abilities.currentPreset;

        public Dialog_AbilityPresets(CompClass_ForceUser forceUser)
        {
            this.forceUser = forceUser;
            doCloseButton = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            selectedPreset = CurrentPreset;

            // Initialize expanded state for all presets
            foreach (var presetName in AbilityPresets.Keys)
            {
                expandedPresets[presetName] = false;
            }
        }

        public override Vector2 InitialSize => new Vector2(700f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new(0f, 0f, inRect.width, 30f), "Ability Presets");
            Text.Font = GameFont.Small;

            float y = 40f;

            // Current preset info
            Widgets.Label(new(0f, y, inRect.width, 30f), $"Current: {CurrentPreset}");
            y += 35f;

            // Create new preset
            Widgets.Label(new(0f, y, 100f, 30f), "New Preset:");
            newPresetName = Widgets.TextField(new(110f, y, 150f, 30f), newPresetName);

            if (Widgets.ButtonText(new(270f, y, 100f, 30f), "Create") && !string.IsNullOrEmpty(newPresetName))
            {
                if (!AbilityPresets.ContainsKey(newPresetName))
                {
                    forceUser.Abilities.SaveCurrentAsPreset(newPresetName);
                    expandedPresets[newPresetName] = false; // Add to expanded tracking
                    Messages.Message($"Created preset: {newPresetName}", MessageTypeDefOf.PositiveEvent);
                    newPresetName = "";
                }
                else
                {
                    Messages.Message("Preset name already exists!", MessageTypeDefOf.RejectInput);
                }
            }

            y += 40f;

            // Preset list with ability toggles
            Rect listRect = new(0f, y, inRect.width, inRect.height - y - CloseButSize.y);
            DrawPresetList(listRect);
        }

        private void DrawPresetList(Rect rect)
        {
            Widgets.Label(new(rect.x, rect.y, 200f, 30f), "Available Presets:");

            float y = rect.y + 35f;

            // Scroll view for presets
            Rect outRect = new(rect.x, y, rect.width, rect.height - y);
            Rect viewRect = new(0f, 0f, outRect.width - 20f, CalculateTotalPresetHeight());

            bool presetWasDeleted = false;
            string presetToDelete = null;

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float currentY = 0f;

            foreach (var presetEntry in AbilityPresets.ToList()) // Use ToList() to avoid modification during iteration
            {
                var preset = presetEntry.Value;
                bool isExpanded = expandedPresets.TryGetValue(preset.presetName, out var expanded) && expanded;

                // Preset header row
                Rect presetHeaderRect = new(0f, currentY, viewRect.width, 30f);

                // Expand/collapse button
                Rect expandRect = new(0f, currentY, 30f, 30f);
                if (Widgets.ButtonText(expandRect, isExpanded ? "▼" : "►"))
                {
                    // Toggle expansion state
                    expandedPresets[preset.presetName] = !isExpanded;
                    // Collapse other presets when expanding this one
                    if (!isExpanded)
                    {
                        foreach (var key in expandedPresets.Keys.ToList())
                        {
                            if (key != preset.presetName)
                            {
                                expandedPresets[key] = false;
                            }
                        }
                    }
                }

                // Preset name button
                Rect nameRect = new(35f, currentY, 150f, 30f);
                if (Widgets.ButtonText(nameRect, preset.presetName))
                {
                    selectedPreset = preset.presetName;
                }

                // Load button
                Rect loadRect = new(190f, currentY, 80f, 30f);
                if (Widgets.ButtonText(loadRect, "Load"))
                {
                    forceUser.Abilities.LoadPreset(preset.presetName);
                    Messages.Message($"Loaded preset: {preset.presetName}", MessageTypeDefOf.NeutralEvent);
                }

                // Copy button
                Rect copyRect = new(275f, currentY, 80f, 30f);
                if (Widgets.ButtonText(copyRect, "Copy"))
                {
                    Find.WindowStack.Add(new Dialog_CopyPreset(forceUser, preset.presetName));
                }

                // Delete button - mark for deletion instead of immediate deletion
                Rect deleteRect = new(360f, currentY, 80f, 30f);
                if (Widgets.ButtonText(deleteRect, "Delete"))
                {
                    if (AbilityPresets.Count > 1)
                    {
                        presetToDelete = preset.presetName;
                        presetWasDeleted = true;
                    }
                    else
                    {
                        Messages.Message("Cannot delete the last preset!", MessageTypeDefOf.RejectInput);
                    }
                }

                currentY += 35f;

                // Show abilities for expanded preset
                if (isExpanded)
                {
                    // Abilities scroll view within the expanded preset
                    Rect abilitiesOutRect = new(20f, currentY, viewRect.width - 40f, 200f);
                    Rect abilitiesViewRect = new(0f, 0f, abilitiesOutRect.width - 20f, forceUser.unlockedAbiliities.Count * 30f);

                    Widgets.BeginScrollView(abilitiesOutRect, ref scrollPosition, abilitiesViewRect);
                    float abilitiesY = 0f;

                    foreach (var abilityDefName in forceUser.unlockedAbiliities)
                    {
                        var abilityDef = DefDatabase<AbilityDef>.GetNamedSilentFail(abilityDefName);
                        if (abilityDef != null)
                        {
                            // Get the active state from THIS preset, not the current one
                            bool isActiveInThisPreset = preset.activeAbilities.Contains(abilityDefName);

                            Rect abilityRowRect = new(0f, abilitiesY, abilitiesViewRect.width, 25f);

                            // Display-only - no toggling from other presets
                            Rect statusRect = new(0f, abilitiesY, 80f, 25f);
                            GUI.color = isActiveInThisPreset ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.8f, 0.4f, 0.4f);
                            Widgets.Label(statusRect, isActiveInThisPreset ? "Active" : "Inactive");
                            GUI.color = Color.white;

                            // Ability name
                            Rect abilityNameRect = new(85f, abilitiesY, abilitiesViewRect.width - 85f, 25f);
                            Widgets.Label(abilityNameRect, abilityDef.LabelCap);

                            abilitiesY += 30f;
                        }
                    }

                    Widgets.EndScrollView();
                    currentY += 205f; // Height of abilities scroll view + padding
                }
            }

            Widgets.EndScrollView();

            if (presetWasDeleted && presetToDelete != null)
            {
                forceUser.Abilities.DeletePreset(presetToDelete);
                expandedPresets.Remove(presetToDelete);
                Messages.Message($"Deleted preset: {presetToDelete}", MessageTypeDefOf.NeutralEvent);
            }
        }

        private float CalculateTotalPresetHeight()
        {
            float height = 0f;
            foreach (var presetEntry in AbilityPresets)
            {
                height += 35f; // Header row

                if (expandedPresets.TryGetValue(presetEntry.Key, out var expanded) && expanded)
                {
                    height += 205f; // Expanded content height
                }
            }
            return height;
        }
    }

    

   
}