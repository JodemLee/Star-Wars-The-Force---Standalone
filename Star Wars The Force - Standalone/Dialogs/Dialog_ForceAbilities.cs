using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheForce_Standalone.Alignment;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    public class Dialog_ForceAbilities : Window
    {
        private CompClass_ForceUser forceUser;
        private Vector2 scrollPosition;
        private string selectedTab;
        private static readonly Color UnlockedColor = new Color(0.7f, 1f, 0.7f);
        private static readonly Color LockedColor = new Color(0.8f, 0.8f, 0.8f);
        private static readonly Color RequirementNotMetColor = new Color(1f, 0.5f, 0.5f);
        private static readonly Color CanUnlockColor = new Color(1f, 1f, 0.6f);

        // Simplified caching - only what's truly expensive
        private Dictionary<string, List<AbilityDef>> abilitiesByCategory;
        private List<string> categoryNames;
        private long lastCacheRefreshTick = -1;

        // Frame-based cache for translations (auto-cleared each frame)
        private int cacheFrame;
        private Dictionary<string, string> frameTranslationCache = new Dictionary<string, string>();

        public Dialog_ForceAbilities(CompClass_ForceUser forceUser)
        {
            this.forceUser = forceUser;
            doCloseButton = true;
            forcePause = true;
            absorbInputAroundWindow = true;

            EnsureCacheIsValid();
            selectedTab = categoryNames.FirstOrDefault() ?? "Force.AbilityCategories.General".Translate();
        }

        public override Vector2 InitialSize => new Vector2(1000f, 750f);

        private void EnsureCacheIsValid()
        {
            if (abilitiesByCategory != null && lastCacheRefreshTick == GenTicks.TicksGame)
                return;

            abilitiesByCategory = new Dictionary<string, List<AbilityDef>>();

            foreach (var abilityDef in DefDatabase<AbilityDef>.AllDefs)
            {
                var ext = abilityDef.GetModExtension<ForceAbilityDefExtension>();
                if (ext == null) continue;

                string category = abilityDef.category?.label ?? "Force.AbilityCategories.Misc".Translate();

                if (!abilitiesByCategory.ContainsKey(category))
                    abilitiesByCategory[category] = new List<AbilityDef>();

                abilitiesByCategory[category].Add(abilityDef);
            }

            // Sort each category by level
            foreach (var category in abilitiesByCategory.Keys.ToList())
            {
                abilitiesByCategory[category] = abilitiesByCategory[category]
                    .OrderBy(ad => ad.level)
                    .ToList();
            }

            categoryNames = abilitiesByCategory.Keys
                .OrderBy(k => k)
                .ToList();

            lastCacheRefreshTick = GenTicks.TicksGame;
        }


        private string GetTruncatedDescription(AbilityDef abilityDef, float width)
        {
            string description = abilityDef.description;
            float textWidth = Text.CalcSize(description).x;
            return textWidth > width ? description.Truncate(width, null) : description;
        }

        private (string status, Color color) GetStatusInfo(AbilityDef abilityDef, CompClass_ForceUser forceUser)
        {
            var ext = abilityDef.GetModExtension<ForceAbilityDefExtension>();
            bool requirementsMet = forceUser.Abilities.CanUnlockAbility(abilityDef);
            bool canAfford = forceUser.Abilities.AvailableAbilityPoints > 0;
            bool alreadyUnlocked = forceUser.unlockedAbiliities.Contains(abilityDef.defName);
            bool canUnlock = requirementsMet && canAfford && !alreadyUnlocked;

            string status;
            Color color;

            if (alreadyUnlocked)
            {
                status = "Force.Abilities.Status.Unlocked".Translate();
                color = UnlockedColor;
            }
            else if (canUnlock)
            {
                status = "Force.Abilities.Status.CanUnlock".Translate();
                color = CanUnlockColor;
            }
            else
            {
                color = RequirementNotMetColor;

                if (!requirementsMet)
                {
                    if (forceUser.forceLevel < ext.RequiredLevel)
                        status = "Force.Abilities.Status.NeedLevel".Translate(ext.RequiredLevel);
                    else if (ext.requiredAbilities != null &&
                            !ext.requiredAbilities.All(req => forceUser.unlockedAbiliities.Contains(req.defName)))
                        status = "Force.Abilities.Status.MissingPrereqs".Translate();
                    else if (ext.requiredTraits != null)
                    {
                        bool hasTraits = ext.requireAllTraits
                            ? ext.requiredTraits.All(t => forceUser.Pawn.story.traits.HasTrait(t))
                            : ext.requiredTraits.Any(t => forceUser.Pawn.story.traits.HasTrait(t));

                        if (!hasTraits)
                            status = ext.requireAllTraits ? "Force.Abilities.Status.MissingAllTraits".Translate() : "Force.Abilities.Status.MissingOneTrait".Translate();
                        else
                            status = "Force.Abilities.Status.Locked".Translate();
                    }
                    else if (ext.requiredHediffs != null)
                    {
                        bool hasHediffs = ext.requireAllHediffs
                            ? ext.requiredHediffs.All(h => forceUser.Pawn.health.hediffSet.HasHediff(h))
                            : ext.requiredHediffs.Any(h => forceUser.Pawn.health.hediffSet.HasHediff(h));

                        if (!hasHediffs)
                            status = ext.requireAllHediffs ? "Force.Abilities.Status.MissingAllConditions".Translate() : "Force.Abilities.Status.MissingOneCondition".Translate();
                        else
                            status = "Force.Abilities.Status.Locked".Translate();
                    }
                    else if (ext.requiredAlignment != null)
                    {
                        float currentAttunement = ext.requiredAlignment.alignmentType == AlignmentType.Darkside
                            ? forceUser.Alignment.DarkSideAttunement
                            : forceUser.Alignment.LightSideAttunement;

                        if (currentAttunement < ext.requiredAlignment.value)
                        {
                            string sideName = ext.requiredAlignment.alignmentType == AlignmentType.Darkside ?
                                "Force.Abilities.Status.DarkSide".Translate() :
                                "Force.Abilities.Status.LightSide".Translate();
                            status = "Force.Abilities.Status.NeedAlignment".Translate(sideName, ext.requiredAlignment.value);
                        }
                        else
                        {
                            status = "Force.Abilities.Status.Locked".Translate();
                        }
                    }
                    else
                    {
                        status = "Force.Abilities.Status.Locked".Translate();
                    }
                }
                else if (!canAfford)
                {
                    status = "Force.Abilities.Status.NoPoints".Translate();
                }
                else
                {
                    status = "Force.Abilities.Status.Locked".Translate();
                }
            }

            return (status, color);
        }

        private void RefreshDialog()
        {
            scrollPosition = Vector2.zero;
        }

        public override void DoWindowContents(Rect inRect)
        {
            try
            {
                // Reset frame cache at start of each GUI frame
                if (cacheFrame != Time.frameCount)
                {
                    frameTranslationCache.Clear();
                    cacheFrame = Time.frameCount;
                }

                EnsureCacheIsValid();

                // Calculate heights for each section
                float titleHeight = 35f;
                float infoHeight = 30f;
                float tabHeight = 30f;
                float paddingBetweenSections = 5f;

                if (forceUser == null || forceUser.Abilities == null || forceUser.unlockedAbiliities == null)
                {
                    forceUser.Initialize(forceUser.Props);
                    forceUser.Abilities.Initialize();
                    return;
                }

                // Title
                Text.Font = GameFont.Medium;
                Rect titleRect = new(0f, 0f, inRect.width, titleHeight);
                Widgets.Label(titleRect, "Force.Abilities.Title".Translate());
                Text.Font = GameFont.Small;

                // Info box
                Rect infoRect = new(0f, titleHeight, inRect.width, infoHeight);
                GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                Widgets.DrawBoxSolid(infoRect, GUI.color);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.MiddleCenter;
                int unlockedCount = forceUser.unlockedAbiliities?.Count() ?? 0;
                Widgets.Label(infoRect, "Force.Abilities.Info".Translate(forceUser.Abilities.AvailableAbilityPoints, unlockedCount));
                Text.Anchor = TextAnchor.UpperLeft;

                // Preset button
                Rect presetRect = new(inRect.width - 200f, titleHeight + 5f, 190f, 25f);
                if (Widgets.ButtonText(presetRect, "Presets".Translate()))
                {
                    forceUser.Abilities.EnsureDefaultPreset();
                    Find.WindowStack.Add(new Dialog_AbilityPresets(forceUser));
                }

                // Show current preset name
                Rect currentPresetRect = new(inRect.width - 200f, titleHeight + 35f, 190f, 20f);
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(currentPresetRect, "Preset: {0}".Translate(forceUser.Abilities.currentPreset));
                Text.Anchor = TextAnchor.UpperLeft;

                // Tab buttons
                Rect tabsRect = new(0f, titleHeight + infoHeight + paddingBetweenSections + 35, inRect.width, tabHeight);
                Widgets.DrawLineHorizontal(0f, titleHeight + infoHeight + 2f, inRect.width);

                List<TabRecord> tabs = new List<TabRecord>();
                foreach (var category in categoryNames)
                {
                    tabs.Add(new TabRecord(category, () =>
                    {
                        selectedTab = category;
                        RefreshDialog();
                    }, selectedTab == category));
                }

                TabDrawer.DrawTabs(tabsRect, tabs);

                // Main content area
                float contentTop = tabsRect.y + tabHeight + paddingBetweenSections;
                Rect contentRect = new(0f, contentTop, inRect.width, inRect.height - contentTop - CloseButSize.y);
                GUI.BeginGroup(contentRect);

                if (!abilitiesByCategory.TryGetValue(selectedTab, out var abilitiesInTab))
                {
                    abilitiesInTab = new List<AbilityDef>();
                }

                // Calculate total height needed
                float rowHeight = 100f;
                float totalHeight = abilitiesInTab.Count * rowHeight;

                // Scroll view setup
                Rect outRect = new(0f, 0f, contentRect.width, contentRect.height);
                Rect viewRect = new(0f, 0f, outRect.width - 16f, totalHeight);

                Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
                float y = 0f;

                foreach (AbilityDef abilityDef in abilitiesInTab)
                {
                    var ext = abilityDef.GetModExtension<ForceAbilityDefExtension>();
                    if (ext == null) continue;

                    Rect rowRect = new(0f, y, viewRect.width, rowHeight);

                    if (y % (rowHeight * 2) < rowHeight)
                    {
                        Widgets.DrawLightHighlight(rowRect);
                    }

                    bool alreadyUnlocked = forceUser.unlockedAbiliities.Contains(abilityDef.defName);
                    var statusInfo = GetStatusInfo(abilityDef, forceUser);

                    // Draw icon
                    if (abilityDef.uiIcon != null && abilityDef.uiIcon != BaseContent.BadTex)
                    {
                        Rect iconRect = new(rowRect.x + 10f, rowRect.y + 10f, 60f, 60f);
                        GUI.DrawTexture(iconRect, abilityDef.uiIcon);
                    }

                    Rect mainRect = new(80f, rowRect.y + 10f, rowRect.width - 90f, rowHeight - 20f);

                    Text.Font = GameFont.Small;
                    Rect nameRect = new(mainRect.x, mainRect.y, mainRect.width * 0.7f, 24f);
                    string nameText = abilityDef.LabelCap;
                    if (ext.RequiredLevel > 0)
                    {
                        nameText += "Force.Abilities.LevelRequirement".Translate(ext.RequiredLevel);
                    }
                    Widgets.Label(nameRect, nameText);

                    // Requirements status
                    Rect statusRect = new(mainRect.x + mainRect.width * 0.7f, mainRect.y, mainRect.width * 0.3f, 24f);
                    Text.Anchor = TextAnchor.MiddleRight;
                    GUI.color = statusInfo.color;
                    Widgets.Label(statusRect, statusInfo.status);
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = Color.white;

                    // Description
                    Rect descRect = new(mainRect.x, mainRect.y + 28f, mainRect.width - 110f, 32f);
                    Text.Font = GameFont.Tiny;
                    string truncatedDescription = GetTruncatedDescription(abilityDef, descRect.width);
                    Widgets.Label(descRect, truncatedDescription);

                    // Display stats
                    if (abilityDef.statBases != null && abilityDef.statBases.Any())
                    {
                        Rect statsRect = new(mainRect.x, mainRect.y + 60f, mainRect.width - 110f, 20f);
                        Text.Font = GameFont.Tiny;
                        string statsText = string.Join(", ", abilityDef.statBases.Select(s =>
                            "Force.Abilities.StatFormat".Translate(s.stat.label.CapitalizeFirst(), s.value)));
                        Widgets.Label(statsRect, statsText);
                    }

                    bool isUnlocked = forceUser.unlockedAbiliities.Contains(abilityDef.defName);
                    bool isActive = false;
                    if (forceUser != null && abilityDef != null && !abilityDef.defName.NullOrEmpty())
                    {
                        isActive = forceUser.Abilities.IsAbilityActive(abilityDef.defName);
                    }

                    if (isUnlocked)
                    {
                        Rect toggleRect = new(mainRect.x + mainRect.width - 100f, mainRect.y + 30f, 100f, 24f);

                        if (Widgets.ButtonText(toggleRect, isActive ? "Active".Translate() : "Inactive".Translate()))
                        {
                            forceUser.Abilities.ToggleAbilityInPreset(abilityDef.defName, !isActive);
                        }

                        // Color code the button
                        GUI.color = isActive ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.8f, 0.4f, 0.4f);
                        Widgets.DrawBox(toggleRect);
                        GUI.color = Color.white;
                    }

                    // Button or requirements text for unlocking
                    if (!alreadyUnlocked)
                    {
                        Rect buttonRect = new(mainRect.x + mainRect.width - 100f, mainRect.y + 60f, 100f, 24f);

                        bool requirementsMet = forceUser.Abilities.CanUnlockAbility(abilityDef);
                        bool canAfford = forceUser.Abilities.AvailableAbilityPoints > 0;
                        bool canUnlock = requirementsMet && canAfford;

                        if (canUnlock)
                        {
                            if (Widgets.ButtonText(buttonRect, "Force.Abilities.Button.Unlock".Translate()))
                            {
                                if (forceUser.Abilities.TryUnlockAbility(abilityDef))
                                {
                                    forceUser.Abilities.ToggleAbilityInPreset(abilityDef.defName, !isActive);
                                    RefreshDialog();
                                }
                                else
                                {
                                    Messages.Message("Force.Abilities.Error.UnlockFailed".Translate(), MessageTypeDefOf.RejectInput);
                                }
                            }
                        }
                        else
                        {
                            GUI.color = Color.gray;
                            Widgets.ButtonText(buttonRect, "Force.Abilities.Button.Unlock".Translate(), active: false);
                            GUI.color = Color.white;

                            if (Mouse.IsOver(buttonRect))
                            {
                                StringBuilder tip = new();
                                tip.AppendLine("Force.Abilities.Tooltip.RequirementsHeader".Translate());

                                bool hasRequirements = false;

                                // Check level requirement
                                if (forceUser.forceLevel < ext.RequiredLevel)
                                {
                                    tip.AppendLine("Force.Abilities.Tooltip.NeedLevel".Translate(ext.RequiredLevel, forceUser.forceLevel));
                                    hasRequirements = true;
                                }

                                // Check ability requirements
                                if (ext.requiredAbilities != null)
                                {
                                    var missing = ext.requiredAbilities
                                        .Where(req => !forceUser.unlockedAbiliities.Contains(req.defName))
                                        .Select(req => req.LabelCap);

                                    if (missing.Any())
                                    {
                                        tip.AppendLine("Force.Abilities.Tooltip.MissingPrereqs".Translate(string.Join(", ", missing)));
                                        hasRequirements = true;
                                    }
                                }

                                // Check trait requirements
                                if (ext.requiredTraits != null)
                                {
                                    var missing = ext.requiredTraits
                                        .Where(req => !forceUser.Pawn.story.traits.HasTrait(req))
                                        .Select(req => req.LabelCap);

                                    if (missing.Any())
                                    {
                                        tip.AppendLine("Force.Abilities.Tooltip.MissingTraits".Translate(string.Join(", ", missing)));
                                        hasRequirements = true;
                                    }
                                }

                                // Check hediff requirements
                                if (ext.requiredHediffs != null)
                                {
                                    var missing = ext.requiredHediffs
                                        .Where(req => forceUser.Pawn.health.hediffSet.GetFirstHediffOfDef(req) == null)
                                        .Select(req => req.LabelCap);

                                    if (missing.Any())
                                    {
                                        tip.AppendLine("Force.Abilities.Tooltip.MissingHediffs".Translate(string.Join(", ", missing)));
                                        hasRequirements = true;
                                    }
                                }

                                if (ext.requiredAlignment != null)
                                {
                                    // Check what type of alignment is required
                                    if (ext.requiredAlignment.alignmentType == AlignmentType.Darkside)
                                    {
                                        float currentDarkAlignment = forceUser.Alignment.DarkSideAttunement;
                                        if (currentDarkAlignment < ext.requiredAlignment.value)
                                        {
                                            tip.AppendLine("Force.Abilities.Tooltip.NeedDarkAlignment".Translate(
                                                ext.requiredAlignment.value.ToString("F1"),
                                                currentDarkAlignment.ToString("F1")));
                                            hasRequirements = true;
                                        }
                                    }
                                    else if (ext.requiredAlignment.alignmentType == AlignmentType.Lightside)
                                    {
                                        float currentLightAlignment = forceUser.Alignment.LightSideAttunement;
                                        if (currentLightAlignment < ext.requiredAlignment.value)
                                        {
                                            tip.AppendLine("Force.Abilities.Tooltip.NeedLightAlignment".Translate(
                                                ext.requiredAlignment.value.ToString("F1"),
                                                currentLightAlignment.ToString("F1")));
                                            hasRequirements = true;
                                        }
                                    }
                                }

                                // Check ability points
                                if (forceUser.Abilities.AvailableAbilityPoints <= 0)
                                {
                                    tip.AppendLine("Force.Abilities.Tooltip.NoPoints".Translate());
                                    hasRequirements = true;
                                }

                                // Only show tooltip if there are actual requirements missing
                                if (hasRequirements)
                                {
                                    TooltipHandler.TipRegion(buttonRect, tip.ToString());
                                }
                            }
                        }
                    }

                    Text.Font = GameFont.Small;

                    // Tooltip
                    if (Mouse.IsOver(rowRect))
                    {
                        TooltipHandler.TipRegion(rowRect, abilityDef.GetTooltip());
                    }

                    y += rowHeight;
                }

                Widgets.EndScrollView();
                GUI.EndGroup();
            }
            catch (Exception ex)
            {
                // Log the error and display a user-friendly message
                Log.Error($"Error in DoWindowContents: {ex}");

                // Display error message to the user
                Rect errorRect = new(inRect.width / 4, inRect.height / 3, inRect.width / 2, 100f);
                GUI.color = Color.red;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(errorRect, "Error displaying abilities window. Please check logs for details.".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;

                // Optionally add a close button
                Rect closeButtonRect = new(inRect.width / 2 - 50f, inRect.height / 3 + 110f, 100f, 30f);
                if (Widgets.ButtonText(closeButtonRect, "Close".Translate()))
                {
                    this.Close();
                }
            }
        }

        public void RecacheTranslations()
        {
            // Clear the expensive cache to force refresh
            lastCacheRefreshTick = -1;
            EnsureCacheIsValid();
            RefreshDialog();
        }
    }
}