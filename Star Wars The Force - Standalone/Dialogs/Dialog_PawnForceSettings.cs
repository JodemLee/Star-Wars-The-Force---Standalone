using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheForce_Standalone.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Dialogs
{
    public class Dialog_PawnForceSettings : Window
    {
        private Pawn pawn;
        private CompClass_ForceUser forceUser;
        private Vector2 scrollPosition;

        public Dialog_PawnForceSettings(Pawn pawn)
        {
            this.pawn = pawn;
            this.forceUser = pawn.GetComp<CompClass_ForceUser>();
            doCloseButton = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(400f, 400f); // Increased height for new buttons

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new(0f, 0f, inRect.width, 30f), "Force.Alignment.PawnSettings".Translate(pawn.LabelShortCap));
            Text.Font = GameFont.Small;

            float y = 35f;
            Rect contentRect = new(0f, y, inRect.width, inRect.height - y - CloseButSize.y);
            Rect viewRect = new(0f, 0f, contentRect.width - 20f, CalculateContentHeight());

            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);
            float currentY = 0f;

            // Color Settings Section
            Rect colorsHeaderRect = new(0f, currentY, viewRect.width, 25f);
            Text.Font = GameFont.Medium;
            Widgets.Label(colorsHeaderRect, "Force.Alignment.ColorSettings".Translate());
            Text.Font = GameFont.Small;
            currentY += 30f;

            // ForceBar Color Button
            Rect barColorRect = new(0f, currentY, viewRect.width, 30f);
            if (Widgets.ButtonText(barColorRect, "Force.Alignment.ChangeForceBarColor".Translate()))
            {
                Find.WindowStack.Add(new Dialog_ColorPicker(forceUser.barColor, (newColor) => {
                    forceUser.barColor = newColor;
                    forceUser.MarkForRedraw(); // Update any visuals if needed
                }));
            }
            TooltipHandler.TipRegion(barColorRect, "Force.Alignment.ForceBarColorTooltip".Translate());
            currentY += 35f;

            // Text Color Button
            Rect textColorRect = new(0f, currentY, viewRect.width, 30f);
            if (Widgets.ButtonText(textColorRect, "Force.Alignment.ChangeTextColor".Translate()))
            {
                // You'll need to add a textColor field to CompClass_ForceUser first
                Color currentTextColor = forceUser.textColor; // Assuming you add this field
                Find.WindowStack.Add(new Dialog_ColorPicker(currentTextColor, (newColor) => {
                    forceUser.textColor = newColor; // Assuming you add this field
                    forceUser.MarkForRedraw(); // Update any visuals if needed
                }));
            }
            TooltipHandler.TipRegion(textColorRect, "Force.Alignment.TextColorTooltip".Translate());
            currentY += 40f;

            // Dark Side Visuals Setting
            if (forceUser?.Alignment != null)
            {
                Rect darkVisualsRect = new(0f, currentY, viewRect.width, 30f);
                bool darkVisualsEnabled = forceUser.Alignment.enableDarkSideVisuals;
                bool originalValue = darkVisualsEnabled; // Store original value

                Widgets.CheckboxLabeled(darkVisualsRect, "Force.Alignment.EnableDarkSideVisuals".Translate(), ref darkVisualsEnabled);

                // Check if value changed
                if (darkVisualsEnabled != originalValue)
                {
                    forceUser.Alignment.enableDarkSideVisuals = darkVisualsEnabled;
                    forceUser.MarkForRedraw(); // Force redraw to update visuals immediately
                }

                TooltipHandler.TipRegion(darkVisualsRect, "Force.Alignment.DarkSideVisualsTooltip".Translate());
                currentY += 35f;
            }

            // Crystal Transformation Setting
            if (ModsConfig.IsActive("lee.theforce.lightsaber") && forceUser != null)
            {
                Rect crystalRect = new(0f, currentY, viewRect.width, 30f);
                bool crystalEnabled = forceUser.enableCrystalTransformation;
                Widgets.CheckboxLabeled(crystalRect, "Force.Alignment.EnableCrystalTransformation".Translate(), ref crystalEnabled);
                forceUser.enableCrystalTransformation = crystalEnabled;
                TooltipHandler.TipRegion(crystalRect, "Force.Alignment.CrystalTransformTooltip".Translate());
                currentY += 35f;
            }

            Widgets.EndScrollView();
        }

        private float CalculateContentHeight()
        {
            float height = 0f;

            // Color settings section
            height += 30f; // Header
            height += 35f; // ForceBar color button
            height += 40f; // Text color button + extra spacing

            // Add height for dark side visuals toggle
            if (forceUser?.Alignment != null)
            {
                height += 35f;
            }

            if (ModsConfig.IsActive("lee.theforce.lightsaber") && forceUser != null)
            {
                height += 35f; // Crystal transformation
            }

            return height;
        }
    }
}