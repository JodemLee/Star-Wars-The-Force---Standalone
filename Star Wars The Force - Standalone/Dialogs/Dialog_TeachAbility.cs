using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using TheForce_Standalone;

namespace TheForce_Standalone.Dialogs
{
    public class Dialog_TeachAbility : Window
    {
        private Pawn masterPawn;
        private Pawn apprenticePawn;
        private List<Ability> teachableAbilities;

        private Vector2 scrollPosition = Vector2.zero;
        private const float RowHeight = 160f;
        private new const float Margin = 10f;
        private const float ButtonWidth = 100f;
        private const float IconSize = 60f;

        public override Vector2 InitialSize => new Vector2(1000f, 900f);

        public Dialog_TeachAbility(Pawn master, Pawn apprentice, List<Ability> abilities)
        {
            this.masterPawn = master;
            this.apprenticePawn = apprentice;
            this.teachableAbilities = abilities;

            doCloseButton = true;
            draggable = true;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
            forcePause = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Header Section
            Rect headerRect = new(0f, 0f, inRect.width, 60f);
            DrawHeader(headerRect);

            // Apprentice Info Section
            Rect apprenticeRect = new(0f, headerRect.yMax + Margin, inRect.width, 70f);
            DrawApprenticeInfo(apprenticeRect);

            // Action Buttons Section
            Rect buttonsRect = new(0f, apprenticeRect.yMax + Margin, inRect.width, 40f);
            DrawActionButtons(buttonsRect);

            // Abilities List Section
            float abilitiesTop = buttonsRect.yMax + Margin;
            Rect abilitiesRect = new(0f, abilitiesTop, inRect.width, inRect.height - abilitiesTop - CloseButSize.y - Margin);
            DrawAbilitiesList(abilitiesRect);
        }

        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "Teach Ability");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // Add separator line
            Widgets.DrawLineHorizontal(rect.x, rect.yMax - 2f, rect.width);
        }

        private void DrawApprenticeInfo(Rect rect)
        {
            GUI.color = new Color(0.9f, 0.9f, 0.9f, 0.3f);
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            Rect innerRect = rect.ContractedBy(5f);

            // Apprentice portrait
            if (apprenticePawn != null)
            {
                Rect portraitRect = new(innerRect.x, innerRect.y, 60f, 60f);
                GUI.DrawTexture(portraitRect, PortraitsCache.Get(apprenticePawn, new Vector2(60f, 60f), Rot4.South, default(Vector3), 1f, renderClothes: true));

                // Apprentice name and info
                Rect infoRect = new(portraitRect.xMax + Margin, innerRect.y, innerRect.width - portraitRect.width - Margin, innerRect.height);
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleLeft;

                string pawnInfo = $"{apprenticePawn.Name.ToStringShort}\nApprentice";
                Widgets.Label(infoRect, pawnInfo);

                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }
        }

        private void DrawActionButtons(Rect rect)
        {
            float buttonWidth = (rect.width - Margin) / 2f;

            Rect removeButtonRect = new(rect.x, rect.y, buttonWidth, rect.height);
            Rect graduateButtonRect = new(rect.x + buttonWidth + Margin, rect.y, buttonWidth, rect.height);

            // Remove Apprentice button
            GUI.color = Color.white;
            if (Widgets.ButtonText(removeButtonRect, "Remove Apprentice"))
            {
                RemoveApprentice();
            }

            // Graduate Apprentice button
            GUI.color = Color.white;
            if (Widgets.ButtonText(graduateButtonRect, "Graduate Apprentice"))
            {
                GraduateApprentice();
            }

            GUI.color = Color.white;
        }

        private void DrawAbilitiesList(Rect rect)
        {
            // Section header
            Rect headerRect = new(rect.x, rect.y, rect.width, 30f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(headerRect, $"Teachable Abilities ({teachableAbilities.Count}):");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // List area
            Rect outRect = new(rect.x, headerRect.yMax + 5f, rect.width, rect.height - headerRect.height - 5f);

            if (teachableAbilities == null || !teachableAbilities.Any())
            {
                DrawNoAbilitiesMessage(outRect);
                return;
            }

            float totalHeight = teachableAbilities.Count * RowHeight;
            Rect viewRect = new(0f, 0f, outRect.width - 16f, totalHeight);

            // Background for scroll view
            Widgets.DrawBoxSolid(outRect, new Color(0.1f, 0.1f, 0.1f, 0.3f));

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            float y = 0f;
            for (int i = 0; i < teachableAbilities.Count; i++)
            {
                Ability ability = teachableAbilities[i];
                Rect rowRect = new(0f, y, viewRect.width, RowHeight);

                // Alternate background colors for readability
                if (i % 2 == 0)
                {
                    Widgets.DrawLightHighlight(rowRect);
                }

                DrawAbilityRow(rowRect, ability, i);
                y += RowHeight;
            }

            Widgets.EndScrollView();
        }

        private void DrawAbilityRow(Rect rect, Ability ability, int index)
        {
            Rect paddedRect = rect.ContractedBy(5f);

            // Ability icon
            if (ability.def.uiIcon != null && ability.def.uiIcon != BaseContent.BadTex)
            {
                Rect iconRect = new(paddedRect.x + 10f, paddedRect.y + 10f, IconSize, IconSize);
                GUI.DrawTexture(iconRect, ability.def.uiIcon);
            }

            // Ability information (offset for icon)
            Rect infoRect = new(paddedRect.x + IconSize + 20f, paddedRect.y, paddedRect.width - IconSize - 20f - ButtonWidth - Margin, paddedRect.height);

            // Ability name
            Rect nameRect = new(infoRect.x, infoRect.y, infoRect.width, 30f);
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.9f, 0.9f, 0.6f, 1f);
            Widgets.Label(nameRect, ability.def.label.CapitalizeFirst());

            // Ability description
            Rect descRect = new(infoRect.x, nameRect.yMax + 2f, infoRect.width, 50f);
            GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            Text.Font = GameFont.Small;

            string description = ability.def.description;
            string truncatedDesc = description.Truncate(descRect.width * 4f);

            Widgets.Label(descRect, truncatedDesc);

            // Display stats
            if (ability.def.statBases != null && ability.def.statBases.Any())
            {
                Rect statsRect = new(infoRect.x, descRect.yMax + 2f, infoRect.width, 30f);
                Text.Font = GameFont.Small;
                GUI.color = new Color(0.7f, 1f, 0.7f, 1f);

                string statsText = "Stats: " + string.Join(", ", ability.def.statBases.Select(s =>
                    $"{s.stat.label.CapitalizeFirst()} {s.value}"));
                Widgets.Label(statsRect, statsText);
                GUI.color = Color.white;
            }

            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // Teach button
            Rect buttonRect = new(paddedRect.xMax - ButtonWidth, paddedRect.y + (paddedRect.height - 40f) / 2f, ButtonWidth, 40f);

            var apprenticeComp = apprenticePawn.TryGetComp<CompClass_ForceUser>();
            bool canLearn = apprenticeComp != null && !apprenticeComp.unlockedAbiliities.Contains(ability.def.defName);

            GUI.color = Color.white;
            if (Widgets.ButtonText(buttonRect, "Teach") && canLearn)
            {
                TeachAbility(ability);
            }

            // Color the button text based on availability
            if (!canLearn)
            {
                Rect buttonTextRect = buttonRect;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                Widgets.Label(buttonTextRect, "Teach");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            // Separator line between rows
            if (index < teachableAbilities.Count - 1)
            {
                Widgets.DrawLineHorizontal(rect.x + 10f, rect.yMax - 1f, rect.width - 20f);
            }
        }

        private void DrawNoAbilitiesMessage(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);

            Widgets.Label(rect, "No abilities available");

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // Additional info text
            Rect infoRect = new(rect.x, rect.y + 40f, rect.width, 50f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(infoRect, "No teachable abilities available for this apprentice.");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private void TeachAbility(Ability ability)
        {
            var apprenticeForceUser = apprenticePawn.TryGetComp<CompClass_ForceUser>();
            if (apprenticeForceUser == null)
            {
                Messages.Message("Force.ApprenticeCannotLearnAbility".Translate(apprenticePawn.Name.ToStringShort), MessageTypeDefOf.RejectInput);
                return;
            }

            if (apprenticeForceUser.unlockedAbiliities.Contains(ability.def.defName))
            {
                Messages.Message("Force.ApprenticeAlreadyKnowsAbility".Translate(apprenticePawn.Name.ToStringShort, ability.def.label), MessageTypeDefOf.RejectInput);
                return;
            }

            apprenticeForceUser.unlockedAbiliities.Add(ability.def.defName);
            apprenticeForceUser.Abilities.UpdatePawnAbilities();
            apprenticeForceUser.Abilities.OnAbilityUnlocked(ability.def.defName);

            ApplyCooldown(masterPawn);
            Messages.Message("Force.TaughtNewAbility".Translate(apprenticePawn.Name.ToStringShort, ability.def.label), MessageTypeDefOf.PositiveEvent);
            Close();
        }

        private void ApplyCooldown(Pawn pawn)
        {
            Hediff cooldownHediff = HediffMaker.MakeHediff(ForceDefOf.Force_TeachingCooldown, pawn);
            pawn.health.AddHediff(cooldownHediff);
        }

        private void RemoveApprentice()
        {
            var masterComp = masterPawn.GetComp<CompClass_ForceUser>();
            if (masterComp?.Apprenticeship != null)
            {
                RemoveApprenticeReference();
                Messages.Message("Force.ApprenticeshipRemoved".Translate(apprenticePawn.Name.ToStringShort), MessageTypeDefOf.NeutralEvent);
            }
            Close();
        }

        public void GraduateApprentice()
        {
            var apprenticeStory = apprenticePawn.story;
            if (Force_ModSettings.rankUpApprentice && apprenticeStory != null)
            {
                var masterComp = masterPawn.GetComp<CompClass_ForceUser>();
                if (masterComp?.Apprenticeship != null)
                {
                    masterComp.Apprenticeship.apprentices.Remove(apprenticePawn);
                    RemoveApprenticeReference();

                    if (Force_ModSettings.rankUpMaster)
                    {
                        masterComp.Apprenticeship.graduatedApprenticesCount++;
                        masterComp.Apprenticeship.CheckAndPromoteMasterBackstory();
                    }
                }
                Find.WindowStack.Add(new TheForce_Standalone.Dialogs.Dialog_SelectBackstory(apprenticePawn));
            }
            Close();
        }

        public void RemoveApprenticeReference()
        {
            var masterComp = masterPawn.GetComp<CompClass_ForceUser>();
            var apprenticeComp = apprenticePawn.GetComp<CompClass_ForceUser>();

            if (masterComp?.Apprenticeship != null)
            {
                masterComp.Apprenticeship.apprentices.Remove(apprenticePawn);
            }

            if (apprenticeComp?.Apprenticeship != null)
            {
                apprenticeComp.Apprenticeship.master = null;
            }

            apprenticePawn.relations.RemoveDirectRelation(ForceDefOf.Force_MasterRelation, masterPawn);
            masterPawn.relations.RemoveDirectRelation(ForceDefOf.Force_ApprenticeRelation, apprenticePawn);
        }
    }
}