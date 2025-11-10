using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    [StaticConstructorOnStartup]
    public class ForceUserGizmo : Gizmo
    {
        private CompClass_ForceUser forceUser;
        private float alignmentValue; // Your alignment system value (0-1)
        private bool draggingAlignment;
        private Color barColor = Color.cyan;
        private static readonly Texture2D ForceBarHighlightTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.3f, 0.8f, 1f));
        private static readonly Texture2D AlignmentBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.8f, 0.2f, 0.2f));
        private static readonly Texture2D AlignmentBarHighlightTex = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 0.3f, 0.3f));
        private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));
        private static readonly Texture2D XPBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.4f, 0.8f, 0.2f, 0.8f)); // Green XP bar

        public ForceUserGizmo(CompClass_ForceUser forceUser)
        {
            this.forceUser = forceUser;
            Order = -100f;
        }

        public override float GetWidth(float maxWidth) => 150f;

        public override bool GroupsWith(Gizmo other)
        {
            return other is ForceUserGizmo;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            if (forceUser == null)
            {
                Log.Error("ForceUserGizmo: forceUser is null");
                return new GizmoResult(GizmoState.Clear);
            }

            const float TitleHeight = 20f;
            const float BarHeight = 20f;
            const float XPBarHeight = 12f; // Smaller XP bar
            const float Padding = 5f;
            const float ButtonSize = 20f;

            // Calculate total height with XP bar
            float totalHeight = 75f;

            // Main container
            Rect mainRect = new(topLeft.x, topLeft.y, GetWidth(maxWidth), totalHeight);
            Widgets.DrawWindowBackground(mainRect);

            //Rect gearRect = new(mainRect.x + mainRect.width - ButtonSize - Padding, mainRect.y + Padding, ButtonSize, ButtonSize);
            //if (Widgets.ButtonImage(gearRect, ContentFinder<Texture2D>.Get("UI/Icons/Options/OptionsGeneral"), Color.white, GenUI.MouseoverColor))
            //{
            //    Find.WindowStack.Add(new Dialog_ColorPicker(barColor, (newColor) => {
            //        barColor = newColor;
            //        forceUser.barColor = newColor; // Update the force user's bar color if needed
            //    }));
            //}
            //TooltipHandler.TipRegion(gearRect, "Force.Gizmo.ChangeColor".Translate());

            // Title with ability points and unlock button
            Rect titleRect = new(mainRect.x + Padding, mainRect.y + Padding, mainRect.width - ButtonSize - Padding * 2, TitleHeight);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(titleRect, "Force.Gizmo.Title".Translate());

            // Force Points Bar
            Rect fpRect = new(mainRect.x + Padding, mainRect.y + TitleHeight + Padding, mainRect.width - Padding * 2, BarHeight);
            float fpRatio = forceUser.FPRatio;

            Widgets.FillableBar(fpRect, fpRatio, SolidColorMaterials.NewSolidColorTexture(forceUser.barColor), EmptyBarTex, false);

            // FP Text (centered on bar)
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = forceUser.textColor;
            Widgets.Label(fpRect, "Force.Gizmo.FPCount".Translate(forceUser.currentFP.ToString("F0"), forceUser.MaxFP.ToString("F0")));
            Text.Anchor = TextAnchor.UpperLeft;

            // XP Bar (smaller bar underneath FP bar)
            Rect xpRect = new(mainRect.x + Padding, fpRect.yMax + Padding, mainRect.width - Padding * 2, XPBarHeight);

            // Calculate XP ratio
            float xpRatio = 0;
            string xpText = "";

            if (forceUser.Leveling != null)
            {
                int xpRequired = forceUser.Leveling.XPRequiredForNextLevel;
                if (xpRequired > 0)
                {
                    xpRatio = Mathf.Clamp01(forceUser.Leveling.ForceExperience / xpRequired);
                }
            }

            // Draw XP bar
            Widgets.FillableBar(xpRect, xpRatio, XPBarTex, EmptyBarTex, false);

            // XP Text (centered on bar)
            Text.Font = GameFont.Tiny; // Smaller font for XP text
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            Widgets.Label(xpRect, xpText);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // Tooltip - with null checks for all components
            if (Mouse.IsOver(mainRect))
            {
                string tooltip = "Force.Gizmo.Tooltip".Translate(
                    forceUser.currentFP.ToString("F0"),
                    forceUser.MaxFP.ToString("F0"),
                    forceUser.forceLevel,
                    forceUser.Abilities.AvailableAbilityPoints,
                    alignmentValue.ToString("P0"),
                    forceUser.Leveling?.ForceExperience.ToString("0.00") ?? "0.00",
                    forceUser.Leveling?.XPRequiredForNextLevel.ToString() ?? "0",
                    forceUser.unlockedAbiliities?.Count() ?? 0
                );
                TooltipHandler.TipRegion(mainRect, tooltip);
            }

            return new GizmoResult(GizmoState.Clear);
        }
    }


    public class Dialog_ColorPicker : Window
    {
        private Color currentColor;
        private Action<Color> onColorSelected;

        public Dialog_ColorPicker(Color initialColor, Action<Color> onColorSelected)
        {
            this.currentColor = initialColor;
            this.onColorSelected = onColorSelected;
            this.doCloseButton = false;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(300f, 460f); // Increased height to accommodate new button

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new(inRect.x, inRect.y, inRect.width, 30f), "Force.Gizmo.ChooseColor".Translate());
            Text.Font = GameFont.Small;

            // Color preview
            Rect previewRect = new(inRect.x + 10f, inRect.y + 40f, inRect.width - 20f, 40f);
            Widgets.DrawBoxSolid(previewRect, currentColor);
            Widgets.DrawBox(previewRect);

            // RGB sliders
            Rect rSliderRect = new(inRect.x + 10f, inRect.y + 90f, inRect.width - 20f, 30f);
            currentColor.r = Widgets.HorizontalSlider(rSliderRect, currentColor.r, 0f, 1f, false, "Force.Gizmo.Red".Translate(), "0", "1", 0.01f);

            Rect gSliderRect = new(inRect.x + 10f, inRect.y + 130f, inRect.width - 20f, 30f);
            currentColor.g = Widgets.HorizontalSlider(gSliderRect, currentColor.g, 0f, 1f, false, "Force.Gizmo.Green".Translate(), "0", "1", 0.01f);

            Rect bSliderRect = new(inRect.x + 10f, inRect.y + 170f, inRect.width - 20f, 30f);
            currentColor.b = Widgets.HorizontalSlider(bSliderRect, currentColor.b, 0f, 1f, false, "Force.Gizmo.Blue".Translate(), "0", "1", 0.01f);

            // Preset colors
            Rect presetsRect = new(inRect.x + 10f, inRect.y + 210f, inRect.width - 20f, 120f);
            Widgets.Label(new(presetsRect.x, presetsRect.y, presetsRect.width, 20f), "Force.Gizmo.Presets".Translate());

            float buttonSize = 25f;
            float spacing = 5f;
            Color[] presetColors = new Color[]
            {
            Color.red,
            Color.green,
            Color.blue,
            Color.cyan,
            Color.magenta,
            Color.yellow,
            Color.white,
            Color.black
            };

            for (int i = 0; i < presetColors.Length; i++)
            {
                float x = presetsRect.x + (i % 4) * (buttonSize + spacing);
                float y = presetsRect.y + 25f + (i / 4) * (buttonSize + spacing);
                Rect colorButtonRect = new(x, y, buttonSize, buttonSize);

                if (Widgets.ButtonImage(colorButtonRect, BaseContent.WhiteTex, presetColors[i], presetColors[i] * 1.2f))
                {
                    currentColor = presetColors[i];
                }
            }

            // ColorDef button - opens float menu with all ColorDefs
            Rect colorDefButtonRect = new(inRect.x + 10f, inRect.y + 340f, inRect.width - 20f, 30f);
            if (Widgets.ButtonText(colorDefButtonRect, "Force.Gizmo.ChooseFromVanilla".Translate()))
            {
                List<FloatMenuOption> colorOptions = new List<FloatMenuOption>();
                List<ColorDef> colorDefs = DefDatabase<ColorDef>.AllDefs.ToList();

                foreach (ColorDef colorDef in colorDefs)
                {
                    colorOptions.Add(new FloatMenuOption(
                        colorDef.LabelCap,
                        () => currentColor = colorDef.color,
                        BaseContent.WhiteTex,
                        colorDef.color    
                    ));
                }

                Find.WindowStack.Add(new FloatMenu(colorOptions));
            }

            // Apply button - moved up to avoid overlap with close button
            Rect applyButtonRect = new(inRect.x + 10f, inRect.y + 385f, inRect.width - 20f, 30f);
            if (Widgets.ButtonText(applyButtonRect, "Force.Gizmo.ApplyColor".Translate()))
            {
                onColorSelected?.Invoke(currentColor);
                Close();
            }
        }
    }
}