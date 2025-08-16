using RimWorld;
using System;
using System.Text.RegularExpressions;
using TheForce_Standalone.Apprenticeship;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    [StaticConstructorOnStartup]
    public class ITab_Pawn_Alignment : ITab
    {
        private Vector2 thoughtScrollPosition;
        public static readonly Vector3 PawnTextureCameraOffset = default;
        private bool showHat;
        private readonly Rot4 rot = new Rot4(2);
        public const float pawnPanelSize = 128f;
        private const float padding = 10f;
        private const float sectionSpacing = 8f;

        private Pawn PawnToShowInfoAbout
        {
            get
            {
                Pawn pawn = null;
                if (base.SelPawn != null)
                {
                    pawn = base.SelPawn;
                }
                else if (base.SelThing is Corpse corpse)
                {
                    pawn = corpse.InnerPawn;
                }
                return pawn ?? throw new InvalidOperationException("Force.Error.NoPawnSelected".Translate());
            }
        }

        public override bool IsVisible =>
            base.SelPawn != null &&
            !base.SelPawn.RaceProps.Animal &&
            !base.SelPawn.RaceProps.Insect &&
            base.SelPawn.needs?.AllNeeds.Count > 0 &&
            base.SelPawn.GetComp<CompClass_ForceUser>().IsValidForceUser;

        public ITab_Pawn_Alignment()
        {
            labelKey = "Force.Alignment.TabLabel";
            tutorTag = "Force.Alignment";
            size = new Vector2(320f, 400f);
        }

        protected override void FillTab()
        {
            try
            {
                Rect tabRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(padding);
                Pawn pawn = PawnToShowInfoAbout;
                if (pawn.story == null) return;

                GUI.BeginGroup(tabRect);
                try
                {
                    float curY = 0f;

                    Rect headerRect = new Rect(0f, curY, tabRect.width, 30f);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.Font = GameFont.Medium;
                    Widgets.Label(headerRect, "Force.Alignment.Header".Translate());
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                    curY += headerRect.height + sectionSpacing;

                    Rect contentRect = new Rect(0f, curY, tabRect.width, tabRect.height - curY - 35f);

                    Rect rowRect = new Rect(0f, curY, contentRect.width, pawnPanelSize);
                    GUI.BeginGroup(rowRect);
                    {
                        Rect pawnRect = new Rect(0f, 0f, pawnPanelSize, pawnPanelSize);
                        DrawColonist(pawnRect, pawn);

                        Rect statsRect = new Rect(
                            pawnRect.xMax + padding,
                            0f,
                            rowRect.width - pawnRect.width - padding,
                            pawnPanelSize
                        );
                        ForceAlignmentUtility.DrawAlignmentStats(statsRect, pawn);
                    }
                    GUI.EndGroup();
                    curY += rowRect.height + sectionSpacing;

                    float infoBoxHeight = 80f;

                    Hediff_Master masterHediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(ForceDefOf.Force_Master) as Hediff_Master;
                    if (masterHediff != null)
                    {
                        Rect masterRect = new Rect(
                            0f,
                            curY,
                            contentRect.width,
                            infoBoxHeight
                        );
                        ForceAlignmentUtility.DrawMasterInfo(masterRect, pawn);
                        curY += masterRect.height + sectionSpacing;
                    }

                    Hediff_Apprentice apprenticeHediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(ForceDefOf.Force_Apprentice) as Hediff_Apprentice;
                    if (apprenticeHediff != null)
                    {
                        Rect apprenticeRect = new Rect(
                            0f,
                            curY,
                            contentRect.width,
                            infoBoxHeight
                        );
                        ForceAlignmentUtility.DrawApprenticeInfo(apprenticeRect, pawn);
                        curY += apprenticeRect.height + sectionSpacing;
                    }
                }
                finally
                {
                    GUI.EndGroup();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Force.Error.AlignmentTabError".Translate(ex.ToString()));
            }
        }

        private void DrawColonist(Rect rect, Pawn pawn)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(rect, PortraitsCache.Get(
                pawn,
                rect.size,
                rot,
                PawnTextureCameraOffset,
                1.1f,
                true,
                true,
                showHat,
                true,
                null,
                null,
                true
            ));

            GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.4f);
            Widgets.DrawBox(rect, 2);
            GUI.color = Color.white;
        }
    }

    [StaticConstructorOnStartup]
    public static class ForceAlignmentUtility
    {
        private static readonly Texture2D LightSideTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.5f, 1f, 0.4f));
        private static readonly Texture2D DarkSideTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.8f, 0.1f, 0.1f, 0.4f));
        private static readonly Texture2D NeutralTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.2f));
        private static readonly Texture2D MarkerTex = ContentFinder<Texture2D>.Get("UI/Icons/AlignmentMarker");
        public static Regex ValidNameRegex = new Regex("^[\\p{L}0-9 '\\-.]*$");

        public static void DrawAlignmentVisualization(Rect rect, Pawn pawn)
        {
            Widgets.DrawBox(rect, 2);
            Widgets.DrawLightHighlight(rect);

            Rect titleRect = new Rect(rect.x, rect.y, rect.width, 25f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, "Force.Alignment.AttunementHeader".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            Rect vizRect = new Rect(rect.x, titleRect.yMax + 5f, rect.width, rect.height - 30f).ContractedBy(5f);
            GUI.BeginGroup(vizRect);

            float lightValue = pawn.GetStatValueForPawn(StatDef.Named("Force_Lightside_Attunement"), pawn, true);
            float darkValue = pawn.GetStatValueForPawn(StatDef.Named("Force_Darkside_Attunement"), pawn, true);
            float maxPossible = 1000f;
            float totalForce = Mathf.Min(lightValue + darkValue, maxPossible);

            float lightWidth = (lightValue / maxPossible) * (vizRect.width / 2);
            float darkWidth = (darkValue / maxPossible) * (vizRect.width / 2);

            Rect barRect = new Rect(0, vizRect.height / 2 - 10f, vizRect.width, 20f);
            DrawDualAlignmentBar(barRect, lightWidth, darkWidth);

            Rect centerLine = new Rect(barRect.center.x - 1f, barRect.y - 5f, 2f, barRect.height + 10f);
            GUI.DrawTexture(centerLine, BaseContent.WhiteTex);

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = new Color(0.2f, 0.5f, 1f);
            Widgets.Label(new Rect(barRect.x, barRect.y - 30f, 100f, 25f), "Force.Alignment.LightSideValue".Translate(lightValue.ToString("F1")));

            Text.Anchor = TextAnchor.UpperRight;
            GUI.color = new Color(0.8f, 0.1f, 0.1f);
            Widgets.Label(new Rect(barRect.xMax - 100f, barRect.y - 30f, 100f, 25f), "Force.Alignment.DarkSideValue".Translate(darkValue.ToString("F1")));

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }

        private static void DrawDualAlignmentBar(Rect rect, float lightWidth, float darkWidth)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.15f, 0.15f, 0.8f));
            float centerX = rect.center.x;
            Rect darkRect = new Rect(centerX, rect.y, darkWidth, rect.height);
            Widgets.DrawBoxSolid(darkRect, new Color(0.8f, 0.1f, 0.1f, 0.6f));
            Rect lightRect = new Rect(centerX - lightWidth, rect.y, lightWidth, rect.height);
            Widgets.DrawBoxSolid(lightRect, new Color(0.2f, 0.5f, 1f, 0.6f));
            if (darkWidth > 0)
            {
                Rect darkEdge = new Rect(darkRect.xMax - 2f, darkRect.y, 4f, darkRect.height);
                GUI.DrawTexture(darkEdge, BaseContent.WhiteTex, ScaleMode.StretchToFill, true, 0, new Color(1, 1, 1, 0.4f), 0, 0);
            }

            if (lightWidth > 0)
            {
                Rect lightEdge = new Rect(lightRect.x, lightRect.y, 4f, lightRect.height);
                GUI.DrawTexture(lightEdge, BaseContent.WhiteTex, ScaleMode.StretchToFill, true, 0, new Color(1, 1, 1, 0.4f), 0, 0);
            }
        }

        public static void DrawMasterInfo(Rect rect, Pawn pawn)
        {
            Widgets.DrawMenuSection(rect);
            GUI.BeginGroup(rect.ContractedBy(5f));

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            float curY = 0f;
            float lineHeight = 22f;
            float buttonWidth = 100f;

            Hediff_Master masterHediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(ForceDefOf.Force_Master) as Hediff_Master;
            if (masterHediff == null) return;
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0, curY, rect.width, lineHeight), "Force.Alignment.MasteryHeader".Translate());
            curY += lineHeight;
            Rect summaryRect = new Rect(0, curY, rect.width, lineHeight * 2);
            string summaryText = "Force.Alignment.MasterSummary".Translate(
                masterHediff.apprentices.Count,
                masterHediff.apprenticeCapacity,
                masterHediff.graduatedApprenticesCount
            );
            Widgets.Label(summaryRect, summaryText);
            curY += lineHeight * 2;
            if (Force_ModSettings.rankUpMaster)
            {
                int needed = Mathf.Max(0, Force_ModSettings.requiredGraduatedApprentices - masterHediff.graduatedApprenticesCount);
                Rect promotionRect = new Rect(0, curY, rect.width, lineHeight);
                Widgets.Label(promotionRect, "Force.Alignment.PromotionProgress".Translate(
                    masterHediff.graduatedApprenticesCount,
                    Force_ModSettings.requiredGraduatedApprentices
                ));
                curY += lineHeight;

                Rect neededRect = new Rect(0, curY, rect.width, lineHeight);
                Widgets.Label(neededRect, needed > 0 ?
                    "Force.Alignment.NeedMoreGraduates".Translate(needed) :
                    "Force.Alignment.ReadyForPromotion".Translate());
                curY += lineHeight;
            }

            GUI.EndGroup();
        }

        public static void DrawApprenticeInfo(Rect rect, Pawn pawn)
        {
            Widgets.DrawMenuSection(rect);
            GUI.BeginGroup(rect.ContractedBy(5f));

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            float curY = 0f;
            float lineHeight = 22f;
            float buttonWidth = 100f;
            float sectionSpacing = 10f;

            Hediff_Apprentice apprenticeHediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(ForceDefOf.Force_Apprentice) as Hediff_Apprentice;
            if (apprenticeHediff == null || apprenticeHediff.master == null) return;

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0, curY, rect.width, lineHeight), "Force.Alignment.MasterRelationship".Translate());
            curY += lineHeight;

            Rect masterRow = new Rect(0, curY, rect.width, lineHeight * 1.5f);
            GUI.BeginGroup(masterRow);

            string masterInfo = "Force.Alignment.MasterInfo".Translate(
                apprenticeHediff.master.LabelShortCap,
                apprenticeHediff.master.GetComp<CompClass_ForceUser>()?.forceLevel ?? 0
            );
            Widgets.Label(new Rect(0, 0, rect.width - buttonWidth - 5f, lineHeight * 1.5f), masterInfo);

            if (Widgets.ButtonText(new Rect(rect.width - buttonWidth, 0, buttonWidth, lineHeight * 1.5f), "Force.Common.Details".Translate()))
            {
                Find.WindowStack.Add(new Dialog_InfoCard(apprenticeHediff.master));
            }

            GUI.EndGroup();
            curY += lineHeight * 1.5f + sectionSpacing;

            Rect trainingStatsRect = new Rect(0, curY, rect.width, lineHeight);
            Widgets.Label(trainingStatsRect, "Force.Alignment.TrainingDuration".Translate(
                (Find.TickManager.TicksGame - apprenticeHediff.ticksSinceLastXPGain) / 60000
            ));
            curY += lineHeight + sectionSpacing;

            Widgets.Label(new Rect(0, curY, rect.width, lineHeight), "Force.Alignment.TrainingProgress".Translate());
            curY += lineHeight;

            var forceComp = pawn.GetComp<CompClass_ForceUser>();
            if (forceComp != null)
            {
                int currentXP = (int)forceComp.Leveling.ForceExperience;
                int xpForNextLevel = forceComp.Leveling.XPRequiredForNextLevel;
                int xpNeeded = xpForNextLevel - currentXP;

                Rect xpRect = new Rect(0, curY, rect.width, lineHeight);
                Widgets.Label(xpRect, "Force.Alignment.CurrentXP".Translate(currentXP, xpNeeded));
                curY += lineHeight;

                Rect barRect = new Rect(0, curY, rect.width, 20f);
                float fillPercent = (float)currentXP / xpForNextLevel;
                Widgets.FillableBar(barRect, fillPercent,
                    SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.6f, 1f)),
                    SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.1f, 0.1f, 0.5f)),
                    false);

                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(barRect, "Force.Alignment.LevelProgress".Translate(
                    forceComp.forceLevel,
                    forceComp.forceLevel + 1,
                    fillPercent * 100
                ));
                Text.Anchor = TextAnchor.UpperLeft;
                curY += 25f;
            }

            GUI.EndGroup();
        }

        public static void DrawAlignmentStats(Rect rect, Pawn pawn)
        {
            Widgets.DrawMenuSection(rect);
            GUI.BeginGroup(rect.ContractedBy(5f));

            float curY = 0f;
            float lineHeight = 22f;
            float barHeight = 15f;
            float buttonHeight = 30f;
            float sectionPadding = 5f;

            var forceUser = pawn.GetComp<CompClass_ForceUser>();

            // Info icon in top-right corner
            Rect infoIconRect = new Rect(rect.width - 24f, 2f, 20f, 20f);
            if (Widgets.ButtonImage(infoIconRect, TexButton.Info))
            {
                Find.WindowStack.Add(new Dialog_AlignmentActions(pawn));
            }
            TooltipHandler.TipRegion(infoIconRect, "Force.Alignment.ViewActionsTooltip".Translate());

            // Force Level
            if (forceUser != null)
            {
                Rect levelRect = new Rect(0, curY, rect.width - 25f, lineHeight);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(levelRect, "Force.Alignment.ForceLevel".Translate(forceUser.forceLevel));
                curY += lineHeight + sectionPadding;
            }

            // Alignment Bar
            float lightValue = pawn.GetStatValueForPawn(StatDef.Named("Force_Lightside_Attunement"), pawn, true);
            float darkValue = pawn.GetStatValueForPawn(StatDef.Named("Force_Darkside_Attunement"), pawn, true);

            Rect barRect = new Rect(0, curY, rect.width, barHeight);
            float lightFraction = lightValue / 100;
            float darkFraction = darkValue / 100;
            DrawDualAlignmentBar(barRect, lightFraction * (rect.width / 2), darkFraction * (rect.width / 2));
            curY += barHeight + sectionPadding;

            // Light/Dark Percentages
            Rect lightRect = new Rect(0, curY, rect.width / 2, lineHeight);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.2f, 0.5f, 1f);
            Widgets.Label(lightRect, "Force.Alignment.LightSidePercentage".Translate(lightValue.ToString("F1")));

            Rect darkRect = new Rect(rect.width / 2, curY, rect.width / 2, lineHeight);
            GUI.color = new Color(0.8f, 0.1f, 0.1f);
            Widgets.Label(darkRect, "Force.Alignment.DarkSidePercentage".Translate(darkValue.ToString("F1")));
            curY += lineHeight + sectionPadding;
            GUI.color = Color.white;
            Rect abilitiesButtonRect = new Rect(
                (rect.width - 120f) / 2f,
                curY - 4,
                120f,
                buttonHeight
            );
            Text.Font = GameFont.Small;
            if (Widgets.ButtonText(abilitiesButtonRect, "Force.Alignment.ViewAbilities".Translate()))
            {
                if (forceUser != null)
                {
                    Find.WindowStack.Add(new Dialog_ForceAbilities(forceUser));
                }
            }
            TooltipHandler.TipRegion(abilitiesButtonRect, "Force.Alignment.ViewAbilitiesTooltip".Translate());

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }
    }
}