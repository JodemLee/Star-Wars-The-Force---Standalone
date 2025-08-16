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
        private float scrollViewHeight;
        private bool showLightSide = true;
        private bool showDarkSide = true;

        public Dialog_AlignmentActions(Pawn pawn)
        {
            this.pawn = pawn;
            doCloseButton = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            forcePause = true;
        }

        public override Vector2 InitialSize => new Vector2(620f, 700f);

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            windowRect.x = (UI.screenWidth - windowRect.width) / 2;
            windowRect.y = (UI.screenHeight - windowRect.height) / 2;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Header
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 40f), "Force.AlignmentActions.Title".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // Toggle buttons
            Rect toggleRect = new Rect(0f, 40f, inRect.width, 30f);
            Widgets.CheckboxLabeled(new Rect(toggleRect.x, toggleRect.y, 150f, toggleRect.height), "Force.AlignmentActions.ShowLightSide".Translate(), ref showLightSide);
            Widgets.CheckboxLabeled(new Rect(toggleRect.x + 160f, toggleRect.y, 150f, toggleRect.height), "Force.AlignmentActions.ShowDarkSide".Translate(), ref showDarkSide);

            // Main content area
            Rect mainRect = new Rect(0f, 70f, inRect.width, inRect.height - 70f - CloseButSize.y);
            Widgets.BeginScrollView(mainRect, ref scrollPosition, new Rect(0f, 0f, mainRect.width - 20f, scrollViewHeight));

            float curY = 0f;
            float sectionHeight = 0f;

            // Light Side Section
            if (showLightSide)
            {
                curY = DrawSectionHeader(ref sectionHeight, "Force.AlignmentActions.LightSideHeader".Translate(), curY, new Color(0.2f, 0.5f, 1f));

                // Healing Others
                AddAction(ref curY, mainRect, "Force.AlignmentActions.HealingOthers".Translate(),
                    "Force.AlignmentActions.HealingOthersDesc".Translate(),
                    ref sectionHeight, TextAnchor.MiddleLeft, new Color(0.2f, 0.5f, 1f, 0.1f));

                // Charity Quests
                AddAction(ref curY, mainRect, "Force.AlignmentActions.CharityQuests".Translate(),
                    "Force.AlignmentActions.CharityQuestsDesc".Translate(),
                    ref sectionHeight, TextAnchor.MiddleLeft, new Color(0.2f, 0.5f, 1f, 0.1f));

                // Peaceful Resolutions
                AddAction(ref curY, mainRect, "Force.AlignmentActions.PeacefulDiplomacy".Translate(),
                    "Force.AlignmentActions.PeacefulDiplomacyDesc".Translate(),
                    ref sectionHeight, TextAnchor.MiddleLeft, new Color(0.2f, 0.5f, 1f, 0.1f));

                // Diplomatic Triumphs
                AddAction(ref curY, mainRect, "Force.AlignmentActions.DiplomaticVictories".Translate(),
                    "Force.AlignmentActions.DiplomaticVictoriesDesc".Translate(),
                    ref sectionHeight, TextAnchor.MiddleLeft, new Color(0.2f, 0.5f, 1f, 0.1f));

                // Light Side Thoughts
                foreach (var thoughtDef in GetAlignmentThoughts(AlignmentType.Lightside))
                {
                    var ext = thoughtDef.GetModExtension<ThoughtAlignmentExtension>();
                    AddThoughtAction(ref curY, mainRect, thoughtDef, ext.alignmentIncrease, ref sectionHeight);
                }

                curY += 10f;
                sectionHeight += 10f;
            }

            // Dark Side Section
            if (showDarkSide)
            {
                curY = DrawSectionHeader(ref sectionHeight, "Force.AlignmentActions.DarkSideHeader".Translate(), curY, new Color(0.8f, 0.1f, 0.1f));

                // Killing Non-Hostiles
                AddAction(ref curY, mainRect, "Force.AlignmentActions.KillingNonHostiles".Translate(),
                    "Force.AlignmentActions.KillingNonHostilesDesc".Translate(),
                    ref sectionHeight, TextAnchor.MiddleLeft, new Color(0.8f, 0.1f, 0.1f, 0.1f));

                // Harming Allies
                AddAction(ref curY, mainRect, "Force.AlignmentActions.HarmingAllies".Translate(),
                    "Force.AlignmentActions.HarmingAlliesDesc".Translate(),
                    ref sectionHeight, TextAnchor.MiddleLeft, new Color(0.8f, 0.1f, 0.1f, 0.1f));

                // Verbal Abuse
                AddAction(ref curY, mainRect, "Force.AlignmentActions.InsultingOthers".Translate(),
                    "Force.AlignmentActions.InsultingOthersDesc".Translate(),
                    ref sectionHeight, TextAnchor.MiddleLeft, new Color(0.8f, 0.1f, 0.1f, 0.1f));

                // Social Slights
                AddAction(ref curY, mainRect, "Force.AlignmentActions.SocialSlights".Translate(),
                    "Force.AlignmentActions.SocialSlightsDesc".Translate(),
                    ref sectionHeight, TextAnchor.MiddleLeft, new Color(0.8f, 0.1f, 0.1f, 0.1f));

                // Dark Side Thoughts
                foreach (var thoughtDef in GetAlignmentThoughts(AlignmentType.Darkside))
                {
                    var ext = thoughtDef.GetModExtension<ThoughtAlignmentExtension>();
                    AddThoughtAction(ref curY, mainRect, thoughtDef, ext.alignmentIncrease, ref sectionHeight);
                }
            }

            scrollViewHeight = sectionHeight;
            Widgets.EndScrollView();
        }

        private float DrawSectionHeader(ref float sectionHeight, string label, float curY, Color color)
        {
            Rect headerRect = new Rect(0f, curY, 300f, 30f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Medium;
            GUI.color = color;
            Widgets.Label(headerRect, label);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            curY += headerRect.height;
            sectionHeight += headerRect.height;
            return curY;
        }

        private void AddAction(ref float curY, Rect mainRect, string label, string description, ref float sectionHeight,
                            TextAnchor anchor = TextAnchor.MiddleLeft, Color? highlight = null)
        {
            Rect actionRect = new Rect(10f, curY, mainRect.width - 30f, 28f);

            if (highlight.HasValue)
            {
                Widgets.DrawBoxSolid(actionRect, highlight.Value);
            }

            Text.Anchor = anchor;
            Widgets.Label(actionRect, label);
            Text.Anchor = TextAnchor.UpperLeft;

            // Info button
            Rect infoRect = new Rect(actionRect.xMax - 24f, curY + 4f, 20f, 20f);
            if (Mouse.IsOver(infoRect))
            {
                Widgets.DrawHighlight(actionRect);
            }
            TooltipHandler.TipRegion(infoRect, description);
            curY += actionRect.height + 4f;
            sectionHeight += actionRect.height + 4f;
        }

        private void AddThoughtAction(ref float curY, Rect mainRect, ThoughtDef thoughtDef, float alignmentGain, ref float sectionHeight)
        {
            Rect thoughtRect = new Rect(20f, curY, mainRect.width - 40f, 28f);
            Widgets.DrawBoxSolid(thoughtRect, new Color(0.1f, 0.1f, 0.1f, 0.1f));

            if (thoughtDef.Icon != null)
            {
                Rect iconRect = new Rect(thoughtRect.x + 4f, thoughtRect.y + 4f, 20f, 20f);
                GUI.DrawTexture(iconRect, thoughtDef.Icon);
            }


            Rect labelRect = new Rect(thoughtRect.x + 30f, thoughtRect.y, thoughtRect.width - 60f, thoughtRect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, thoughtDef.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;


            Rect gainRect = new Rect(thoughtRect.xMax - 50f, thoughtRect.y, 50f, thoughtRect.height);
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = thoughtDef.GetModExtension<ThoughtAlignmentExtension>().alignment == AlignmentType.Lightside ?
                new Color(0.2f, 0.5f, 1f) : new Color(0.8f, 0.1f, 0.1f);
            Widgets.Label(gainRect, "+".Translate() + alignmentGain.ToString());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            string description = "Force.AlignmentActions.ThoughtTooltip".Translate(
                thoughtDef.description,
                alignmentGain,
                thoughtDef.GetModExtension<ThoughtAlignmentExtension>().alignment == AlignmentType.Lightside ?
                    "Force.AlignmentActions.LightSide".Translate() :
                    "Force.AlignmentActions.DarkSide".Translate()
            );
            TooltipHandler.TipRegion(thoughtRect, description);

            curY += thoughtRect.height + 4f;
            sectionHeight += thoughtRect.height + 4f;
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