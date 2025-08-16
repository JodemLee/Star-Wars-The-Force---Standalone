using RimWorld;
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

        public Dialog_ForceAbilities(CompClass_ForceUser forceUser)
        {
            this.forceUser = forceUser;
            doCloseButton = true;
            forcePause = true;
            absorbInputAroundWindow = true;

            // Set default selected tab to the first category
            var firstCategory = DefDatabase<AbilityDef>.AllDefs
                .Where(ad => ad.GetModExtension<ForceAbilityDefExtension>() != null)
                .Select(ad => ad.category?.label ?? "Force.AbilityCategories.Misc".Translate())
                .FirstOrDefault();
            selectedTab = firstCategory ?? "Force.AbilityCategories.General".Translate();
        }

        public override Vector2 InitialSize => new Vector2(650f, 750f);

        private void RefreshDialog()
        {
            scrollPosition = Vector2.zero;
            selectedTab = selectedTab; // Force refresh
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Calculate heights for each section
            float titleHeight = 35f;
            float infoHeight = 30f;
            float tabHeight = 30f;
            float paddingBetweenSections = 5f;

            // Title
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(0f, 0f, inRect.width, titleHeight);
            Widgets.Label(titleRect, "Force.Abilities.Title".Translate());
            Text.Font = GameFont.Small;

            // Info box
            Rect infoRect = new Rect(0f, titleHeight, inRect.width, infoHeight);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            Widgets.DrawBoxSolid(infoRect, GUI.color);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(infoRect, "Force.Abilities.Info".Translate(forceUser.AvailableAbilityPoints, forceUser.unlockedAbiliities.Count()));
            Text.Anchor = TextAnchor.UpperLeft;

            // Tab buttons
            Rect tabsRect = new Rect(0f, titleHeight + infoHeight + paddingBetweenSections + 35, inRect.width, tabHeight);
            Widgets.DrawLineHorizontal(0f, titleHeight + infoHeight + 2f, inRect.width);

            var categories = DefDatabase<AbilityDef>.AllDefs
                .Where(ad => ad.GetModExtension<ForceAbilityDefExtension>() != null)
                .GroupBy(ad => ad.category?.label ?? "Force.AbilityCategories.Misc".Translate())
                .OrderBy(g => g.Key)
                .ToList();

            List<TabRecord> tabs = new List<TabRecord>();
            foreach (var category in categories)
            {
                string label = category.Key;
                tabs.Add(new TabRecord(label, () => { selectedTab = label; RefreshDialog(); }, selectedTab == label));
            }

            TabDrawer.DrawTabs(tabsRect, tabs);

            // Main content area
            float contentTop = tabsRect.y + tabHeight + paddingBetweenSections;
            Rect contentRect = new Rect(0f, contentTop, inRect.width, inRect.height - contentTop - CloseButSize.y);
            GUI.BeginGroup(contentRect);

            var abilitiesInTab = DefDatabase<AbilityDef>.AllDefs
                .Where(ad =>
                {
                    var ext = ad.GetModExtension<ForceAbilityDefExtension>();
                    return ext != null && (ad.category?.label ?? "Force.AbilityCategories.Misc".Translate()) == selectedTab;
                })
                .OrderBy(ad => ad.level)
                .ToList();

            // Calculate total height needed
            float rowHeight = 100f;
            float totalHeight = abilitiesInTab.Count * rowHeight;
            float scrollViewHeight = Mathf.Min(contentRect.height, totalHeight);

            // Scroll view setup
            Rect outRect = new Rect(0f, 0f, contentRect.width, contentRect.height);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, totalHeight);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float y = 0f;

            foreach (AbilityDef abilityDef in abilitiesInTab)
            {
                var ext = abilityDef.GetModExtension<ForceAbilityDefExtension>();
                if (ext == null) continue;

                Rect rowRect = new Rect(0f, y, viewRect.width, rowHeight);

                if (y % (rowHeight * 2) < rowHeight)
                {
                    Widgets.DrawLightHighlight(rowRect);
                }

                bool requirementsMet = forceUser.Abilities.CanUnlockAbility(abilityDef);
                bool canAfford = forceUser.AvailableAbilityPoints > 0;
                bool alreadyUnlocked = forceUser.unlockedAbiliities.Contains(abilityDef.defName);
                bool canUnlock = requirementsMet && canAfford && !alreadyUnlocked;

                // Draw icon
                if (abilityDef.uiIcon != null && abilityDef.uiIcon != BaseContent.BadTex)
                {
                    Rect iconRect = new Rect(rowRect.x + 10f, rowRect.y + 10f, 60f, 60f);
                    GUI.DrawTexture(iconRect, abilityDef.uiIcon);
                }

                Rect mainRect = new Rect(80f, rowRect.y + 10f, rowRect.width - 90f, rowHeight - 20f);

                Text.Font = GameFont.Small;
                Rect nameRect = new Rect(mainRect.x, mainRect.y, mainRect.width * 0.7f, 24f);
                string nameText = abilityDef.LabelCap;
                if (ext.RequiredLevel > 0)
                {
                    nameText += "Force.Abilities.LevelRequirement".Translate(ext.RequiredLevel);
                }
                Widgets.Label(nameRect, nameText);

                // Requirements status
                Rect statusRect = new Rect(mainRect.x + mainRect.width * 0.7f, mainRect.y, mainRect.width * 0.3f, 24f);
                Text.Anchor = TextAnchor.MiddleRight;
                if (alreadyUnlocked)
                {
                    GUI.color = UnlockedColor;
                    Widgets.Label(statusRect, "Force.Abilities.Status.Unlocked".Translate());
                }
                else if (canUnlock)
                {
                    GUI.color = CanUnlockColor;
                    Widgets.Label(statusRect, "Force.Abilities.Status.CanUnlock".Translate());
                }
                else
                {
                    GUI.color = RequirementNotMetColor;
                    string status = "Force.Abilities.Status.Locked".Translate();
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
                        }
                        else if (ext.requiredHediffs != null)
                        {
                            bool hasHediffs = ext.requireAllHediffs
                                ? ext.requiredHediffs.All(h => forceUser.Pawn.health.hediffSet.HasHediff(h))
                                : ext.requiredHediffs.Any(h => forceUser.Pawn.health.hediffSet.HasHediff(h));

                            if (!hasHediffs)
                                status = ext.requireAllHediffs ? "Force.Abilities.Status.MissingAllConditions".Translate() : "Force.Abilities.Status.MissingOneCondition".Translate();
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
                        }
                    }
                    else if (!canAfford)
                    {
                        status = "Force.Abilities.Status.NoPoints".Translate();
                    }
                    Widgets.Label(statusRect, status);
                }
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;

                // Description
                Rect descRect = new Rect(mainRect.x, mainRect.y + 28f, mainRect.width - 110f, 32f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(descRect, abilityDef.description.Truncate(descRect.width, null));

                // Display stats
                if (abilityDef.statBases != null && abilityDef.statBases.Any())
                {
                    Rect statsRect = new Rect(mainRect.x, mainRect.y + 60f, mainRect.width - 110f, 20f);
                    Text.Font = GameFont.Tiny;
                    string statsText = string.Join(", ", abilityDef.statBases.Select(s =>
                        "Force.Abilities.StatFormat".Translate(s.stat.label.CapitalizeFirst(), s.value)));
                    Widgets.Label(statsRect, statsText);
                }

                // Button or requirements text
                if (!alreadyUnlocked)
                {
                    Rect buttonRect = new Rect(mainRect.x + mainRect.width - 100f, mainRect.y + 60f, 100f, 24f);

                    if (canUnlock)
                    {
                        if (Widgets.ButtonText(buttonRect, "Force.Abilities.Button.Unlock".Translate()))
                        {
                            if (forceUser.Abilities.TryUnlockAbility(abilityDef))
                            {
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
                            StringBuilder tip = new StringBuilder();
                            tip.AppendLine("Force.Abilities.Tooltip.RequirementsHeader".Translate());

                            if (forceUser.forceLevel < ext.RequiredLevel)
                                tip.AppendLine("Force.Abilities.Tooltip.NeedLevel".Translate(ext.RequiredLevel, forceUser.forceLevel));

                            if (ext.requiredAbilities != null)
                            {
                                var missing = ext.requiredAbilities
                                    .Where(req => !forceUser.unlockedAbiliities.Contains(req.defName))
                                    .Select(req => req.LabelCap);

                                if (missing.Any())
                                    tip.AppendLine("Force.Abilities.Tooltip.MissingPrereqs".Translate(string.Join(", ", missing)));
                            }

                            if (forceUser.AvailableAbilityPoints <= 0)
                                tip.AppendLine("Force.Abilities.Tooltip.NoPoints".Translate());

                            TooltipHandler.TipRegion(buttonRect, tip.ToString());
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
    }
}