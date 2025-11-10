using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TheForce_Standalone.Apprenticeship;
using TheForce_Standalone.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Dialogs
{
    [StaticConstructorOnStartup]
    public static class ForceAlignmentUtility
    {
        private static readonly Texture2D LightSideTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.5f, 1f, 0.4f));
        private static readonly Texture2D DarkSideTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.8f, 0.1f, 0.1f, 0.4f));
        private static readonly Texture2D NeutralTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.2f));
        private static readonly Texture2D MarkerTex = ContentFinder<Texture2D>.Get("UI/Icons/AlignmentMarker");
        public static Regex ValidNameRegex = new ("^[\\p{L}0-9 '\\-.]*$");

        public static void DrawAlignmentVisualization(Rect rect, Pawn pawn)
        {
            Widgets.DrawBox(rect, 2);
            Widgets.DrawLightHighlight(rect);

            Rect titleRect = new(rect.x, rect.y, rect.width, 25f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, "Force.Alignment.AttunementHeader".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            Rect vizRect = new Rect(rect.x, titleRect.yMax + 5f, rect.width, rect.height - 30f).ContractedBy(5f);
            GUI.BeginGroup(vizRect);

            float lightValue = pawn.GetStatValueForPawn(StatDef.Named("Force_Lightside_Attunement"), pawn, true);
            float darkValue = pawn.GetStatValueForPawn(StatDef.Named("Force_Darkside_Attunement"), pawn, true);
            float maxPossible = 1000f;

            float lightWidth = (lightValue / maxPossible) * (vizRect.width / 2);
            float darkWidth = (darkValue / maxPossible) * (vizRect.width / 2);

            Rect barRect = new(0, vizRect.height / 2 - 10f, vizRect.width, 20f);
            DrawDualAlignmentBar(barRect, lightWidth, darkWidth);

            Rect centerLine = new(barRect.center.x - 1f, barRect.y - 5f, 2f, barRect.height + 10f);
            GUI.DrawTexture(centerLine, BaseContent.WhiteTex);

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = new Color(0.2f, 0.5f, 1f);
            Widgets.Label(new(barRect.x, barRect.y - 30f, 100f, 25f), "Force.Alignment.LightSideValue".Translate(lightValue.ToString("F1")));

            Text.Anchor = TextAnchor.UpperRight;
            GUI.color = new Color(0.8f, 0.1f, 0.1f);
            Widgets.Label(new(barRect.xMax - 100f, barRect.y - 30f, 100f, 25f), "Force.Alignment.DarkSideValue".Translate(darkValue.ToString("F1")));

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }

        private static void DrawDualAlignmentBar(Rect rect, float lightWidth, float darkWidth)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.15f, 0.15f, 0.8f));
            float centerX = rect.center.x;
            Rect darkRect = new(centerX, rect.y, darkWidth, rect.height);
            Widgets.DrawBoxSolid(darkRect, new Color(0.8f, 0.1f, 0.1f, 0.6f));
            Rect lightRect = new(centerX - lightWidth, rect.y, lightWidth, rect.height);
            Widgets.DrawBoxSolid(lightRect, new Color(0.2f, 0.5f, 1f, 0.6f));
            if (darkWidth > 0)
            {
                Rect darkEdge = new(darkRect.xMax - 2f, darkRect.y, 4f, darkRect.height);
                GUI.DrawTexture(darkEdge, BaseContent.WhiteTex, ScaleMode.StretchToFill, true, 0, new Color(1, 1, 1, 0.4f), 0, 0);
            }

            if (lightWidth > 0)
            {
                Rect lightEdge = new(lightRect.x, lightRect.y, 4f, lightRect.height);
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


            var forceComp = pawn.GetComp<CompClass_ForceUser>();
            if (forceComp?.Apprenticeship == null) return;

            Text.Font = GameFont.Small;
            Widgets.Label(new(0, curY, rect.width, lineHeight), "Force.Alignment.MasteryHeader".Translate());
            curY += lineHeight;

            Rect summaryRect = new(0, curY, rect.width, lineHeight * 2);
            string summaryText = "Force.Alignment.MasterSummary".Translate(
                forceComp.Apprenticeship.apprentices.Count,
                forceComp.Apprenticeship.apprenticeCapacity,
                forceComp.Apprenticeship.graduatedApprenticesCount
            );
            Widgets.Label(summaryRect, summaryText);
            curY += lineHeight * 2;

            if (Force_ModSettings.rankUpMaster)
            {
                int needed = Mathf.Max(0, Force_ModSettings.requiredGraduatedApprentices - forceComp.Apprenticeship.graduatedApprenticesCount);
                Rect promotionRect = new(0, curY, rect.width, lineHeight);
                Widgets.Label(promotionRect, "Force.Alignment.PromotionProgress".Translate(
                    forceComp.Apprenticeship.graduatedApprenticesCount,
                    Force_ModSettings.requiredGraduatedApprentices
                ));
                curY += lineHeight;

                Rect neededRect = new(0, curY, rect.width, lineHeight);
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

            var forceComp = pawn.GetComp<CompClass_ForceUser>();
            if (forceComp?.Apprenticeship?.master == null) return;

            Text.Font = GameFont.Small;
            Widgets.Label(new(0, curY, rect.width, lineHeight), "Force.Alignment.MasterRelationship".Translate());
            curY += lineHeight;

            Rect masterRow = new(0, curY, rect.width, lineHeight * 1.5f);
            GUI.BeginGroup(masterRow);

            string masterInfo = "Force.Alignment.MasterInfo".Translate(
                forceComp.Apprenticeship.master.LabelShortCap,
                forceComp.Apprenticeship.master.GetComp<CompClass_ForceUser>()?.forceLevel ?? 0
            );
            Widgets.Label(new(0, 0, rect.width - buttonWidth - 5f, lineHeight * 1.5f), masterInfo);

            if (Widgets.ButtonText(new(rect.width - buttonWidth, 0, buttonWidth, lineHeight * 1.5f), "Force.Common.Details".Translate()))
            {
                Find.WindowStack.Add(new Dialog_InfoCard(forceComp.Apprenticeship.master));
            }

            GUI.EndGroup();
            curY += lineHeight * 1.5f + sectionSpacing;

            Rect trainingStatsRect = new(0, curY, rect.width, lineHeight);
            Widgets.Label(trainingStatsRect, "Force.Alignment.TrainingDuration".Translate(
                (Find.TickManager.TicksGame - forceComp.Apprenticeship.ticksSinceLastXPGain) / 60000
            ));
            curY += lineHeight + sectionSpacing;

            Widgets.Label(new(0, curY, rect.width, lineHeight), "Force.Alignment.TrainingProgress".Translate());
            curY += lineHeight;

            if (forceComp != null)
            {
                int currentXP = (int)forceComp.Leveling.ForceExperience;
                int xpForNextLevel = forceComp.Leveling.XPRequiredForNextLevel;
                int xpNeeded = xpForNextLevel - currentXP;

                Rect xpRect = new(0, curY, rect.width, lineHeight);
                Widgets.Label(xpRect, "Force.Alignment.CurrentXP".Translate(currentXP, xpNeeded));
                curY += lineHeight;

                Rect barRect = new(0, curY, rect.width, 20f);
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
            Rect settingsButtonRect = new(rect.width - 30f, 2f, 20f, 20f);
            if (Widgets.ButtonImage(settingsButtonRect, ContentFinder<Texture2D>.Get("UI/Icons/Options/OptionsGeneral")))
            {
                Find.WindowStack.Add(new Dialog_PawnForceSettings(pawn));
            }
            TooltipHandler.TipRegion(settingsButtonRect, "Force.Alignment.PawnSettingsTooltip".Translate());

            Rect infoIconRect = new(settingsButtonRect.x - 24f, 2f, 20f, 20f);
            if (Widgets.ButtonImage(infoIconRect, TexButton.Info))
            {
                Find.WindowStack.Add(new Dialog_AlignmentActions(pawn));
            }

            TooltipHandler.TipRegion(infoIconRect, "Force.Alignment.ViewActionsTooltip".Translate());
            if (Prefs.DevMode && DebugSettings.godMode)
            {
                Rect resetIconRect = new(infoIconRect.x - 24f, 2f, 20f, 20f);
                if (Widgets.ButtonImage(resetIconRect, TexButton.HotReloadDefs))
                {
                    // Your existing reset logic here
                    if (forceUser != null && forceUser.IsValidForceUser)
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
                    }
                    else
                    {
                        Messages.Message($"{pawn.LabelShortCap} is not a valid Force user", MessageTypeDefOf.RejectInput);
                    }
                }
                TooltipHandler.TipRegion(resetIconRect, "Force.Alignment.ResetForceUserTooltip".Translate());
            }

            // Force Level
            if (forceUser != null)
            {
                Rect levelRect = new(0, curY, rect.width - 25f, lineHeight);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(levelRect, "Force.Alignment.ForceLevel".Translate(forceUser.forceLevel));
                curY += lineHeight + sectionPadding;
            }

            // Alignment Bar
            float lightValue = pawn.GetStatValueForPawn(StatDef.Named("Force_Lightside_Attunement"), pawn, true);
            float darkValue = pawn.GetStatValueForPawn(StatDef.Named("Force_Darkside_Attunement"), pawn, true);

            Rect barRect = new(0, curY, rect.width, barHeight);
            float lightFraction = lightValue / 100;
            float darkFraction = darkValue / 100;
            DrawDualAlignmentBar(barRect, lightFraction * (rect.width / 2), darkFraction * (rect.width / 2));
            curY += barHeight + sectionPadding;

            // Light/Dark Percentages
            Rect lightRect = new(0, curY, rect.width / 2, lineHeight);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.2f, 0.5f, 1f);
            Widgets.Label(lightRect, "Force.Alignment.LightSidePercentage".Translate(lightValue.ToString("F1")));

            Rect darkRect = new(rect.width / 2, curY, rect.width / 2, lineHeight);
            GUI.color = new Color(0.8f, 0.1f, 0.1f);
            Widgets.Label(darkRect, "Force.Alignment.DarkSidePercentage".Translate(darkValue.ToString("F1")));
            curY += lineHeight + sectionPadding;
            GUI.color = Color.white;
            Rect abilitiesButtonRect = new(
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

        // New helper methods for comp-based system

    }
}