using RimWorld;
using System.Collections.Generic;
using TheForce_Standalone.Alignment;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    public class Dialog_AlignmentActions : Window
    {
        private readonly Pawn pawn;
        private Vector2 scrollPosition;
        private Vector2 logScrollPosition;
        private float scrollViewHeight;
        private float logScrollViewHeight;
        private bool showLightSide = true;
        private bool showDarkSide = true;
        private bool showActionsTab = true;
        private bool showLogTab = false;

        // Filter fields
        private string pawnFilter = "";
        private string actionFilter = "";
        private string factionFilter = "";
        private AlignmentType? alignmentFilter = null;
        private float minAmountFilter = 0f;
        private float maxAmountFilter = 100f;

        public Dialog_AlignmentActions(Pawn pawn)
        {
            this.pawn = pawn;
            doCloseButton = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            forcePause = true;
        }

        public override Vector2 InitialSize => new Vector2(800f, 750f);

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            windowRect.x = (UI.screenWidth - windowRect.width) / 2;
            windowRect.y = (UI.screenHeight - windowRect.height) / 2;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Tab buttons
            Rect tabRect = new(0f, 0f, inRect.width, 35f);
            Widgets.DrawMenuSection(tabRect);

            if (Widgets.ButtonText(new(tabRect.x + 5f, tabRect.y + 5f, 160f, 25f),
                "Force.AlignmentActions.ActionsTab".Translate()))
            {
                showActionsTab = true;
                showLogTab = false;
            }

            if (Widgets.ButtonText(new(tabRect.x + 170f, tabRect.y + 5f, 160f, 25f),
                "Force.AlignmentActions.LogTab".Translate()))
            {
                showActionsTab = false;
                showLogTab = true;
            }

            // Content area with proper spacing from tabs
            Rect contentRect = new(0f, 40f, inRect.width, inRect.height - 40f - CloseButSize.y);

            if (showActionsTab)
            {
                DrawActionsTab(contentRect);
            }
            else if (showLogTab)
            {
                DrawLogTab(contentRect);
            }
        }

        private void DrawActionsTab(Rect inRect)
        {
            // Header with proper spacing
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // Toggle buttons with better spacing
            Rect toggleRect = new(0f, 40f, inRect.width, 30f);
            Widgets.CheckboxLabeled(new(toggleRect.x, toggleRect.y, 160f, toggleRect.height),
                "Force.AlignmentActions.ShowLightSide".Translate(), ref showLightSide);
            Widgets.CheckboxLabeled(new(toggleRect.x + 165f, toggleRect.y, 160f, toggleRect.height),
                "Force.AlignmentActions.ShowDarkSide".Translate(), ref showDarkSide);

            // Main content area with proper margins
            Rect mainRect = new(0f, 75f, inRect.width, inRect.height - 75f);
            Rect viewRect = new(0f, 0f, mainRect.width - 16f, scrollViewHeight);

            Widgets.BeginScrollView(mainRect, ref scrollPosition, viewRect);
            float curY = 0f;
            float sectionHeight = 0f;

            // Light Side Section
            if (showLightSide)
            {
                curY = DrawSectionHeader(ref sectionHeight, "Force.AlignmentActions.LightSideHeader".Translate(), curY, new Color(0.2f, 0.5f, 1f));

                // Healing Others
                AddAction(ref curY, viewRect, "Force.AlignmentActions.HealingOthers".Translate(),
                    "Force.AlignmentActions.HealingOthersDesc".Translate(),
                    ref sectionHeight, new Color(0.2f, 0.5f, 1f, 0.1f));

                // Charity Quests
                AddAction(ref curY, viewRect, "Force.AlignmentActions.CharityQuests".Translate(),
                    "Force.AlignmentActions.CharityQuestsDesc".Translate(),
                    ref sectionHeight, new Color(0.2f, 0.5f, 1f, 0.1f));

                // Peaceful Resolutions
                AddAction(ref curY, viewRect, "Force.AlignmentActions.PeacefulDiplomacy".Translate(),
                    "Force.AlignmentActions.PeacefulDiplomacyDesc".Translate(),
                    ref sectionHeight, new Color(0.2f, 0.5f, 1f, 0.1f));

                // Diplomatic Triumphs
                AddAction(ref curY, viewRect, "Force.AlignmentActions.DiplomaticVictories".Translate(),
                    "Force.AlignmentActions.DiplomaticVictoriesDesc".Translate(),
                    ref sectionHeight, new Color(0.2f, 0.5f, 1f, 0.1f));

                // Light Side Thoughts
                foreach (var thoughtDef in GetAlignmentThoughts(AlignmentType.Lightside))
                {
                    var ext = thoughtDef.GetModExtension<ThoughtAlignmentExtension>();
                    AddThoughtAction(ref curY, viewRect, thoughtDef, ext.alignmentIncrease, ref sectionHeight);
                }

                curY += 15f;
                sectionHeight += 15f;
            }

            // Dark Side Section
            if (showDarkSide)
            {
                curY = DrawSectionHeader(ref sectionHeight, "Force.AlignmentActions.DarkSideHeader".Translate(), curY, new Color(0.8f, 0.1f, 0.1f));

                // Killing Non-Hostiles
                AddAction(ref curY, viewRect, "Force.AlignmentActions.KillingNonHostiles".Translate(),
                    "Force.AlignmentActions.KillingNonHostilesDesc".Translate(),
                    ref sectionHeight, new Color(0.8f, 0.1f, 0.1f, 0.1f));

                // Harming Allies
                AddAction(ref curY, viewRect, "Force.AlignmentActions.HarmingAllies".Translate(),
                    "Force.AlignmentActions.HarmingAlliesDesc".Translate(),
                    ref sectionHeight, new Color(0.8f, 0.1f, 0.1f, 0.1f));

                // Verbal Abuse
                AddAction(ref curY, viewRect, "Force.AlignmentActions.InsultingOthers".Translate(),
                    "Force.AlignmentActions.InsultingOthersDesc".Translate(),
                    ref sectionHeight, new Color(0.8f, 0.1f, 0.1f, 0.1f));

                // Social Slights
                AddAction(ref curY, viewRect, "Force.AlignmentActions.SocialSlights".Translate(),
                    "Force.AlignmentActions.SocialSlightsDesc".Translate(),
                    ref sectionHeight, new Color(0.8f, 0.1f, 0.1f, 0.1f));

                // Dark Side Thoughts
                foreach (var thoughtDef in GetAlignmentThoughts(AlignmentType.Darkside))
                {
                    var ext = thoughtDef.GetModExtension<ThoughtAlignmentExtension>();
                    AddThoughtAction(ref curY, viewRect, thoughtDef, ext.alignmentIncrease, ref sectionHeight);
                }
            }

            scrollViewHeight = sectionHeight;
            Widgets.EndScrollView();
        }

        private void DrawLogTab(Rect inRect)
        {
            float curY = 0f;

            // Header
            Rect headerRect = new(0f, curY, inRect.width, 35f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(headerRect, "Force.AlignmentActions.ActionLog".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            curY += 35f;

            // Filter section
            curY = DrawFilterSection(new(0f, curY, inRect.width, 150f), ref curY);

            // Clear button
            Rect clearButtonRect = new(inRect.width - 90f, curY, 80f, 25f);
            if (Widgets.ButtonText(clearButtonRect, "Force.AlignmentActions.ClearLog".Translate()))
            {
                AlignmentActionLogger.Instance.ClearLog();
            }
            curY += 30f;

            // Log content
            Rect logRect = new(0f, curY, inRect.width, inRect.height - curY);
            DrawLogContent(logRect);
        }

        private float DrawFilterSection(Rect rect, ref float curY)
        {
            // Filter background
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            // Filter header
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new(innerRect.x, innerRect.y, 100f, 25f), "Force.AlignmentActions.Filters".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            float filterY = innerRect.y + 25f;
            float filterWidth = (innerRect.width - 20f) / 2f;

            // First row of filters
            // Pawn filter
            Rect pawnFilterRect = new(innerRect.x, filterY, filterWidth, 30f);
            Widgets.Label(pawnFilterRect.LeftPart(0.3f), "Force.AlignmentActions.PawnFilter".Translate());
            pawnFilter = Widgets.TextField(pawnFilterRect.RightPart(0.7f), pawnFilter);

            // Action filter
            Rect actionFilterRect = new(innerRect.x + filterWidth + 10f, filterY, filterWidth, 30f);
            Widgets.Label(actionFilterRect.LeftPart(0.3f), "Force.AlignmentActions.ActionFilter".Translate());
            actionFilter = Widgets.TextField(actionFilterRect.RightPart(0.7f), actionFilter);

            filterY += 35f;

            // Second row of filters
            // Faction filter
            Rect factionFilterRect = new(innerRect.x, filterY, filterWidth, 30f);
            Widgets.Label(factionFilterRect.LeftPart(0.3f), "Force.AlignmentActions.FactionFilter".Translate());
            factionFilter = Widgets.TextField(factionFilterRect.RightPart(0.7f), factionFilter);

            // Alignment filter
            Rect alignmentFilterRect = new(innerRect.x + filterWidth + 10f, filterY, filterWidth, 30f);
            Widgets.Label(alignmentFilterRect.LeftPart(0.3f), "Force.AlignmentActions.AlignmentFilter".Translate());

            // Alignment filter dropdown
            Rect alignmentButtonRect = alignmentFilterRect.RightPart(0.7f);
            string alignmentLabel = alignmentFilter.HasValue ?
                alignmentFilter.Value.ToString() :
                "Force.AlignmentActions.AnyAlignment".Translate();

            if (Widgets.ButtonText(alignmentButtonRect, alignmentLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("Force.AlignmentActions.AnyAlignment".Translate(), () => alignmentFilter = null),
                    new FloatMenuOption("Force.AlignmentActions.LightSide".Translate(), () => alignmentFilter = AlignmentType.Lightside),
                    new FloatMenuOption("Force.AlignmentActions.DarkSide".Translate(), () => alignmentFilter = AlignmentType.Darkside)
                };
                Find.WindowStack.Add(new FloatMenu(options));
            }

            filterY += 35f;

            // Third row - Amount range
            Rect amountFilterRect = new(innerRect.x, filterY, innerRect.width, 30f);
            Widgets.Label(amountFilterRect.LeftPart(0.15f), "Force.AlignmentActions.AmountRange".Translate());

            // Min amount
            Rect minAmountRect = new(amountFilterRect.x + amountFilterRect.width * 0.15f, filterY, 80f, 30f);
            Widgets.Label(minAmountRect.LeftPart(0.5f), "Force.AlignmentActions.Min".Translate());
            string minAmountBuffer = minAmountFilter.ToString("F2");
            Widgets.TextFieldNumeric(minAmountRect.RightPart(0.5f), ref minAmountFilter, ref minAmountBuffer, 0f, 100f);

            // Max amount
            Rect maxAmountRect = new(minAmountRect.xMax + 10f, filterY, 80f, 30f);
            Widgets.Label(maxAmountRect.LeftPart(0.5f), "Force.AlignmentActions.Max".Translate());
            string maxAmountBuffer = maxAmountFilter.ToString("F2");
            Widgets.TextFieldNumeric(maxAmountRect.RightPart(0.5f), ref maxAmountFilter, ref maxAmountBuffer, 0f, 100f);

            // Reset filters button
            Rect resetFiltersRect = new(innerRect.xMax - 100f, filterY, 100f, 25f);
            if (Widgets.ButtonText(resetFiltersRect, "Force.AlignmentActions.ResetFilters".Translate()))
            {
                ResetFilters();
            }

            curY += rect.height;
            return curY;
        }

        private void DrawLogContent(Rect inRect)
        {
            Rect logViewRect = new(0f, 0f, inRect.width - 16f, logScrollViewHeight);

            Widgets.BeginScrollView(inRect, ref logScrollPosition, logViewRect);

            float curY = 0f;
            var filteredEntries = GetFilteredLogEntries();

            if (filteredEntries.Count == 0)
            {
                Rect noEntriesRect = new(0f, curY, logViewRect.width, 40f);
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                Widgets.Label(noEntriesRect, "Force.AlignmentActions.NoLogEntries".Translate());
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                curY += 40f;
            }
            else
            {
                // Column headers
                Rect timeHeaderRect = new(5f, curY, 70f, 25f);
                Rect pawnHeaderRect = new(80f, curY, 120f, 25f);
                Rect actionHeaderRect = new(205f, curY, 200f, 25f);
                Rect alignmentHeaderRect = new(410f, curY, 100f, 25f);
                Rect amountHeaderRect = new(515f, curY, 70f, 25f);
                Rect sourceHeaderRect = new(590f, curY, logViewRect.width - 590f, 25f);

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.LowerLeft;
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(timeHeaderRect, "Force.AlignmentActions.Time".Translate());
                Widgets.Label(pawnHeaderRect, "Force.AlignmentActions.Pawn".Translate());
                Widgets.Label(actionHeaderRect, "Force.AlignmentActions.Action".Translate());
                Widgets.Label(alignmentHeaderRect, "Force.AlignmentActions.Alignment".Translate());
                Widgets.Label(amountHeaderRect, "Force.AlignmentActions.Amount".Translate());
                Widgets.Label(sourceHeaderRect, "Force.AlignmentActions.Source".Translate());
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;

                curY += 30f;

                // Draw line separator
                Widgets.DrawLineHorizontal(0f, curY, logViewRect.width);
                curY += 10f;

                foreach (var entry in filteredEntries)
                {
                    Rect entryRect = new(0f, curY, logViewRect.width, 25f);

                    // Alternate background
                    if (curY % 50f < 25f)
                    {
                        Widgets.DrawLightHighlight(entryRect);
                    }

                    // Time
                    Rect timeRect = new(5f, curY, 70f, 25f);
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(timeRect, entry.GetFormattedTime());
                    Text.Font = GameFont.Small;

                    // Pawn name
                    Rect pawnRect = new(80f, curY, 120f, 25f);
                    Widgets.Label(pawnRect, entry.PawnName.Truncate(pawnRect.width));

                    // Action
                    Rect actionRect = new(205f, curY, 200f, 25f);
                    Widgets.Label(actionRect, entry.ActionName.Truncate(actionRect.width));

                    // Alignment type with color
                    Rect alignmentRect = new(410f, curY, 100f, 25f);
                    GUI.color = entry.GetAlignmentColor();
                    Widgets.Label(alignmentRect, entry.GetFormattedAlignment());
                    GUI.color = Color.white;

                    // Amount
                    Rect amountRect = new(515f, curY, 70f, 25f);
                    Text.Anchor = TextAnchor.MiddleRight;
                    Widgets.Label(amountRect, $"+{entry.Amount:0.00}");
                    Text.Anchor = TextAnchor.UpperLeft;

                    // Source
                    Rect sourceRect = new(590f, curY, logViewRect.width - 590f, 25f);
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(sourceRect, entry.Source.Truncate(sourceRect.width));
                    Text.Font = GameFont.Small;

                    // Tooltip with full details
                    string tooltip = $"Time: {entry.GetFormattedTime()}\nPawn: {entry.PawnName}\nAction: {entry.ActionName}\nAlignment: {entry.GetFormattedAlignment()}\nAmount: +{entry.Amount:0.00}\nSource: {entry.Source}";
                    TooltipHandler.TipRegion(entryRect, tooltip);

                    curY += 25f;
                }
            }

            logScrollViewHeight = curY;
            Widgets.EndScrollView();
        }

        private List<AlignmentActionLogEntry> GetFilteredLogEntries()
        {
            var filteredEntries = new List<AlignmentActionLogEntry>();
            var allEntries = AlignmentActionLogger.Instance.LogEntries;

            foreach (var entry in allEntries)
            {
                // Pawn filter
                if (!string.IsNullOrEmpty(pawnFilter) &&
                    !entry.PawnName.ToLower().Contains(pawnFilter.ToLower()))
                    continue;

                // Action filter
                if (!string.IsNullOrEmpty(actionFilter) &&
                    !entry.ActionName.ToLower().Contains(actionFilter.ToLower()))
                    continue;

                // Faction filter (if entry has faction information)
                if (!string.IsNullOrEmpty(factionFilter))
                {
                    string entryFaction = entry.Source;
                    if (!entryFaction.ToLower().Contains(factionFilter.ToLower()))
                        continue;
                }

                // Alignment filter
                if (alignmentFilter.HasValue && entry.AlignmentType != alignmentFilter.Value)
                    continue;

                // Amount range filter
                if (entry.Amount < minAmountFilter || entry.Amount > maxAmountFilter)
                    continue;

                filteredEntries.Add(entry);
            }

            return filteredEntries;
        }

        private void ResetFilters()
        {
            pawnFilter = "";
            actionFilter = "";
            factionFilter = "";
            alignmentFilter = null;
            minAmountFilter = 0f;
            maxAmountFilter = 100f;
        }

        private float DrawSectionHeader(ref float sectionHeight, string label, float curY, Color color)
        {
            Rect headerRect = new(0f, curY, 300f, 35f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Medium;
            GUI.color = color;
            Widgets.Label(headerRect, label);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            curY += headerRect.height + 5f;
            sectionHeight += headerRect.height + 5f;
            return curY;
        }

        private void AddAction(ref float curY, Rect viewRect, string label, string description, ref float sectionHeight, Color highlight)
        {
            Rect actionRect = new(10f, curY, viewRect.width - 20f, 32f);

            // Background highlight
            Widgets.DrawBoxSolid(actionRect, highlight);

            // Label
            Rect labelRect = new(15f, curY, viewRect.width - 50f, 32f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;

            // Info icon
            Rect infoRect = new(viewRect.width - 35f, curY + 6f, 20f, 20f);
            GUI.DrawTexture(infoRect, TexButton.Info);

            // Tooltip
            if (Mouse.IsOver(actionRect))
            {
                Widgets.DrawHighlight(actionRect);
            }
            TooltipHandler.TipRegion(actionRect, description);

            curY += actionRect.height + 8f;
            sectionHeight += actionRect.height + 8f;
        }

        private void AddThoughtAction(ref float curY, Rect viewRect, ThoughtDef thoughtDef, float alignmentGain, ref float sectionHeight)
        {
            Rect thoughtRect = new(20f, curY, viewRect.width - 40f, 32f);

            // Subtle background
            Widgets.DrawBoxSolid(thoughtRect, new Color(0.1f, 0.1f, 0.1f, 0.15f));

            // Icon if available
            if (thoughtDef.Icon != null)
            {
                Rect iconRect = new(thoughtRect.x + 5f, thoughtRect.y + 6f, 20f, 20f);
                GUI.DrawTexture(iconRect, thoughtDef.Icon);
            }

            // Label
            Rect labelRect = new(thoughtRect.x + 30f, thoughtRect.y, thoughtRect.width - 80f, thoughtRect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, thoughtDef.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;

            // Alignment gain
            Rect gainRect = new(thoughtRect.xMax - 45f, thoughtRect.y, 40f, thoughtRect.height);
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = thoughtDef.GetModExtension<ThoughtAlignmentExtension>().alignment == AlignmentType.Lightside ?
                new Color(0.2f, 0.5f, 1f) : new Color(0.8f, 0.1f, 0.1f);
            Widgets.Label(gainRect, $"+{alignmentGain:0.##}");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // Tooltip
            string tooltipDescription = "Force.AlignmentActions.ThoughtTooltip".Translate(
                thoughtDef.description,
                alignmentGain.ToString("0.##"),
                thoughtDef.GetModExtension<ThoughtAlignmentExtension>().alignment == AlignmentType.Lightside ?
                    "Force.AlignmentActions.LightSide".Translate() :
                    "Force.AlignmentActions.DarkSide".Translate()
            );
            TooltipHandler.TipRegion(thoughtRect, tooltipDescription);

            curY += thoughtRect.height + 6f;
            sectionHeight += thoughtRect.height + 6f;
        }

        private static IEnumerable<ThoughtDef> GetAlignmentThoughts(AlignmentType alignment)
        {
            foreach (var thoughtDef in DefDatabase<ThoughtDef>.AllDefs)
            {
                var ext = thoughtDef.GetModExtension<ThoughtAlignmentExtension>();
                if (ext != null && ext.alignment == alignment)
                {
                    yield return thoughtDef;
                }
            }
        }
    }
}