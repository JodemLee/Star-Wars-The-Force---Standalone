using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheForce_Standalone.Apprenticeship;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Dialogs
{
    public class Dialog_SelectBackstory : Window
    {
        private Pawn targetPawn;
        private List<BackstoryDef> availableBackstories;
        private Vector2 scrollPosition = Vector2.zero;
        private const float RowHeight = 160f;
        private new const float Margin = 10f;
        private const float ButtonWidth = 100f;

        // Name editing fields
        private string firstName = "";
        private string nickName = "";
        private string lastName = "";
        private bool editingName = false;

        public Dialog_SelectBackstory(Pawn targetPawn)
        {
            this.targetPawn = targetPawn ?? throw new ArgumentNullException(nameof(targetPawn));

            // Initialize name fields
            if (targetPawn.Name is NameTriple nameTriple)
            {
                firstName = nameTriple.First;
                nickName = nameTriple.Nick;
                lastName = nameTriple.Last;
            }
            else
            {
                firstName = targetPawn.Name.ToStringShort;
                nickName = targetPawn.Name.ToStringShort;
                lastName = "";
            }

            BackstoryModExtension modExtension = targetPawn.story.Childhood.GetModExtension<BackstoryModExtension>();
            if (modExtension != null)
            {
                availableBackstories = modExtension.availableBackstories ?? new List<BackstoryDef>();
            }
            else
            {
                availableBackstories = new List<BackstoryDef>();
            }

            this.closeOnClickedOutside = false;
            this.doCloseX = true;
            doCloseButton = true;
            draggable = true;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
            forcePause = true;
        }

        public override Vector2 InitialSize => new Vector2(1000f, 900f);

        public override void DoWindowContents(Rect inRect)
        {
            // Header Section
            Rect headerRect = new(0f, 0f, inRect.width, 60f);
            DrawHeader(headerRect);

            // Pawn Info Section with Name Editing
            Rect pawnRect = new(0f, headerRect.yMax + Margin, inRect.width, 100f); // Increased height for name editing
            DrawPawnInfo(pawnRect);

            // Backstories List Section
            float listTop = pawnRect.yMax + Margin;
            Rect listRect = new(0f, listTop, inRect.width, inRect.height - listTop - CloseButSize.y - Margin);
            DrawBackstoriesList(listRect);
        }

        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "Select Adult Backstory");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // Add separator line
            Widgets.DrawLineHorizontal(rect.x, rect.yMax - 2f, rect.width);
        }

        private void DrawPawnInfo(Rect rect)
        {
            GUI.color = new Color(0.9f, 0.9f, 0.9f, 0.3f);
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            Rect innerRect = rect.ContractedBy(5f);

            // Pawn portrait
            if (targetPawn != null)
            {
                Rect portraitRect = new(innerRect.x, innerRect.y, 60f, 60f);
                GUI.DrawTexture(portraitRect, PortraitsCache.Get(targetPawn, new Vector2(60f, 60f), Rot4.South, default(Vector3), 1f, renderClothes: true));

                // Name editing section - similar to CharacterCardUtility
                Rect nameRect = new(portraitRect.xMax + Margin, innerRect.y, innerRect.width - portraitRect.width - Margin, 60f);
                DrawNameEditing(nameRect);

                // Graduating apprentice info
                Rect infoRect = new(portraitRect.xMax + Margin, innerRect.y + 60f, innerRect.width - portraitRect.width - Margin, 30f);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(infoRect, "Graduating Apprentice");
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawNameEditing(Rect rect)
        {
            Text.Font = GameFont.Small;

            if (editingName)
            {
                // Name editing mode - similar to CharacterCardUtility
                float nameFieldWidth = rect.width / 3f - 5f;

                // First name
                Rect firstRect = new(rect.x, rect.y, nameFieldWidth, 30f);
                GUI.color = new Color(0.95f, 0.95f, 0.8f, 1f);
                Widgets.Label(new(firstRect.x, firstRect.y - 20f, firstRect.width, 20f), "First Name");
                GUI.color = Color.white;
                string newFirst = Widgets.TextField(firstRect, firstName);
                if (newFirst.Length <= 12 && CharacterCardUtility.ValidNameRegex.IsMatch(newFirst))
                {
                    firstName = newFirst;
                }
                TooltipHandler.TipRegion(firstRect, "FirstNameDesc");

                // Nick name
                Rect nickRect = new(rect.x + nameFieldWidth + 5f, rect.y, nameFieldWidth, 30f);
                GUI.color = new Color(0.95f, 0.95f, 0.8f, 1f);
                Widgets.Label(new(nickRect.x, nickRect.y - 20f, nickRect.width, 20f), "Nickname");
                GUI.color = Color.white;

                // Gray out if same as first or last name
                if (nickName == firstName || nickName == lastName)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.5f);
                }
                string newNick = Widgets.TextField(nickRect, nickName);
                if (newNick.Length <= 16 && CharacterCardUtility.ValidNameRegex.IsMatch(newNick))
                {
                    nickName = newNick;
                }
                GUI.color = Color.white;
                TooltipHandler.TipRegion(nickRect, "ShortIdentifierDesc");

                // Last name
                Rect lastRect = new(rect.x + (nameFieldWidth + 5f) * 2f, rect.y, nameFieldWidth, 30f);
                GUI.color = new Color(0.95f, 0.95f, 0.8f, 1f);
                Widgets.Label(new(lastRect.x, lastRect.y - 20f, lastRect.width, 20f), "Last Name");
                GUI.color = Color.white;
                string newLast = Widgets.TextField(lastRect, lastName);
                if (newLast.Length <= 12 && CharacterCardUtility.ValidNameRegex.IsMatch(newLast))
                {
                    lastName = newLast;
                }
                TooltipHandler.TipRegion(lastRect, "LastNameDesc");

                // Apply/Cancel buttons
                Rect applyRect = new(rect.x, rect.y + 35f, 80f, 25f);
                if (Widgets.ButtonText(applyRect, "Apply"))
                {
                    ApplyNameChange();
                    editingName = false;
                }

                Rect cancelRect = new(rect.x + 85f, rect.y + 35f, 80f, 25f);
                if (Widgets.ButtonText(cancelRect, "Cancel"))
                {
                    ResetNameFields();
                    editingName = false;
                }
            }
            else
            {
                // Display mode
                Text.Font = GameFont.Medium;
                string displayName = targetPawn.Name.ToStringFull;
                Rect nameDisplayRect = new(rect.x, rect.y, rect.width - 100f, 30f);
                Widgets.Label(nameDisplayRect, displayName);

                // Edit name button
                Rect editButtonRect = new(rect.x + rect.width - 90f, rect.y, 90f, 25f);
                if (Widgets.ButtonText(editButtonRect, "Edit Name"))
                {
                    editingName = true;
                }

                Text.Font = GameFont.Small;
            }
        }

        private void ApplyNameChange()
        {
            if (targetPawn.Name is NameTriple)
            {
                string finalNick = string.IsNullOrEmpty(nickName) ? firstName : nickName;
                targetPawn.Name = new NameTriple(firstName, finalNick, lastName);
            }
            // If it's not a NameTriple, we don't modify the name structure
        }

        private void ResetNameFields()
        {
            if (targetPawn.Name is NameTriple nameTriple)
            {
                firstName = nameTriple.First;
                nickName = nameTriple.Nick;
                lastName = nameTriple.Last;
            }
            else
            {
                firstName = targetPawn.Name.ToStringShort;
                nickName = targetPawn.Name.ToStringShort;
                lastName = "";
            }
        }

        private void DrawBackstoriesList(Rect rect)
        {
            // Section header
            Rect headerRect = new(rect.x, rect.y, rect.width, 30f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(headerRect, $"Available Backstories ({availableBackstories.Count}):");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // List area
            Rect outRect = new(rect.x, headerRect.yMax + 5f, rect.width, rect.height - headerRect.height - 5f);

            if (availableBackstories == null || !availableBackstories.Any())
            {
                DrawNoBackstoriesMessage(outRect);
                return;
            }

            float totalHeight = availableBackstories.Count * RowHeight;
            Rect viewRect = new(0f, 0f, outRect.width - 16f, totalHeight);

            // Background for scroll view
            Widgets.DrawBoxSolid(outRect, new Color(0.1f, 0.1f, 0.1f, 0.3f));

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            float y = 0f;
            for (int i = 0; i < availableBackstories.Count; i++)
            {
                BackstoryDef backstory = availableBackstories[i];
                Rect rowRect = new(0f, y, viewRect.width, RowHeight);

                // Alternate background colors for readability
                if (i % 2 == 0)
                {
                    Widgets.DrawLightHighlight(rowRect);
                }

                DrawBackstoryRow(rowRect, backstory, i);
                y += RowHeight;
            }

            Widgets.EndScrollView();
        }

        private void DrawBackstoryRow(Rect rect, BackstoryDef backstory, int index)
        {
            Rect paddedRect = rect.ContractedBy(5f);

            // Backstory information - now uses full width minus button
            Rect infoRect = new(paddedRect.x, paddedRect.y, paddedRect.width - ButtonWidth - Margin, paddedRect.height);

            // Title
            Rect titleRect = new(infoRect.x, infoRect.y, infoRect.width, 30f);
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.9f, 0.9f, 0.6f, 1f);
            string displayTitle = backstory.TitleFor(targetPawn.gender);
            Widgets.Label(titleRect, displayTitle);

            // Description - with proper height and scrolling if needed
            Rect descRect = new(infoRect.x, titleRect.yMax + 2f, infoRect.width, 50f);
            GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            Text.Font = GameFont.Small;

            // FIXED: Format the description with the pawn's name
            string description = backstory.FullDescriptionFor(targetPawn).Resolve();
            string truncatedDesc = description.Truncate(descRect.width * 4f);

            Widgets.Label(descRect, truncatedDesc);

            // Skill gains
            Rect skillsRect = new(infoRect.x, descRect.yMax + 2f, infoRect.width, 30f);
            if (backstory.skillGains != null && backstory.skillGains.Count > 0)
            {
                string skillText = "Skills: " + string.Join(", ", backstory.skillGains.Select(sg => $"{sg.skill.skillLabel.CapitalizeFirst()} {sg.amount:+#;-#}"));
                Widgets.Label(skillsRect, skillText.Truncate(skillsRect.width));
            }

            // Work disables
            Rect workRect = new(infoRect.x, skillsRect.yMax + 2f, infoRect.width, 30f);
            if (backstory.workDisables != WorkTags.None)
            {
                string workText = "Disabled Work: " + GetWorkTagsDisplay(backstory.workDisables);
                GUI.color = new Color(0.9f, 0.5f, 0.5f, 1f);
                Widgets.Label(workRect, workText.Truncate(workRect.width));
                GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            }

            // Required work tags
            Rect requiredWorkRect = new(infoRect.x, workRect.yMax + 2f, infoRect.width, 30f);
            if (backstory.requiredWorkTags != WorkTags.None)
            {
                string requiredWorkText = "Required Work: " + GetWorkTagsDisplay(backstory.requiredWorkTags);
                GUI.color = new Color(0.5f, 0.9f, 0.5f, 1f);
                Widgets.Label(requiredWorkRect, requiredWorkText.Truncate(requiredWorkRect.width));
                GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            }

            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // Select button - positioned to the right, not overlapping description
            Rect buttonRect = new(paddedRect.xMax - ButtonWidth, paddedRect.y + (paddedRect.height - 40f) / 2f, ButtonWidth, 40f);
            GUI.color = Color.white;

            if (Widgets.ButtonText(buttonRect, "Select"))
            {
                SelectBackstory(backstory);
            }

            GUI.color = Color.white;

            // Separator line between rows (except for the last one)
            if (index < availableBackstories.Count - 1)
            {
                Widgets.DrawLineHorizontal(rect.x + 10f, rect.yMax - 1f, rect.width - 20f);
            }
        }

        private string GetWorkTagsDisplay(WorkTags workTags)
        {
            if (workTags == WorkTags.None)
                return "None";

            List<string> tags = new List<string>();
            foreach (WorkTags value in Enum.GetValues(typeof(WorkTags)))
            {
                if (value != WorkTags.None && (workTags & value) == value)
                {
                    tags.Add(value.ToString());
                }
            }
            return string.Join(", ", tags);
        }

        private void DrawNoBackstoriesMessage(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);

            Widgets.Label(rect, "No backstories available");

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // Additional info text
            Rect infoRect = new(rect.x, rect.y + 40f, rect.width, 50f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(infoRect, "This pawn does not have any\navailable backstory options.");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private void SelectBackstory(BackstoryDef backstory)
        {
            // Apply name changes if we were editing
            if (editingName)
            {
                ApplyNameChange();
            }

            targetPawn.story.Adulthood = backstory;

            // Show a more detailed success message
            Messages.Message(
                $"Force.ApprenticeshipGraduated".Translate(
                    targetPawn.Name.ToStringShort,
                    backstory.TitleFor(targetPawn.gender)
                ),
                MessageTypeDefOf.PositiveEvent
            );

            Close();
        }
    }
}