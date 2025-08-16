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

        public ForceUserGizmo(CompClass_ForceUser forceUser)
        {
            this.forceUser = forceUser;
            Order = -100f;
        }

        public override float GetWidth(float maxWidth) => 140f;

        public override bool GroupsWith(Gizmo other)
        {
            return other is ForceUserGizmo;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            const float TitleHeight = 20f;
            const float BarHeight = 20f;
            const float LevelHeight = 15f;
            const float Padding = 5f;
            const float ButtonSize = 20f;

            // Main container
            Rect mainRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), TitleHeight + BarHeight + LevelHeight + Padding * 3);
            Widgets.DrawWindowBackground(mainRect);

            // Title with ability points and unlock button
            Rect titleRect = new Rect(mainRect.x + Padding, mainRect.y + Padding, mainRect.width - ButtonSize - Padding * 2, TitleHeight);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(titleRect, "Force.Gizmo.Title".Translate());

            // Force Points Bar
            Rect fpRect = new Rect(mainRect.x + Padding, mainRect.y + TitleHeight + Padding * 2, mainRect.width - Padding * 2, BarHeight);
            float fpRatio = forceUser.FPRatio;
            if (ModsConfig.IdeologyActive)
            {
                barColor = forceUser.Pawn.story.favoriteColor.color;
            }
            Widgets.FillableBar(fpRect, fpRatio, SolidColorMaterials.NewSolidColorTexture(barColor), EmptyBarTex, false);

            // FP Text (centered on bar)
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(fpRect, "Force.Gizmo.FPCount".Translate(forceUser.currentFP.ToString("F0"), forceUser.MaxFP.ToString("F0")));
            Text.Anchor = TextAnchor.UpperLeft;

            // Level and Experience (bottom line)
            Rect infoRect = new Rect(mainRect.x + Padding, mainRect.y + TitleHeight + BarHeight + Padding * 3, mainRect.width - Padding * 2, LevelHeight);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(infoRect, "Force.Gizmo.Experience".Translate(
                forceUser.Leveling.ForceExperience.ToString("0.00"),
                forceUser.Leveling.XPRequiredForNextLevel
            ));

            // Tooltip
            if (Mouse.IsOver(mainRect))
            {
                TooltipHandler.TipRegion(mainRect, "Force.Gizmo.Tooltip".Translate(
                    forceUser.currentFP.ToString("F0"),
                    forceUser.MaxFP.ToString("F0"),
                    forceUser.forceLevel,
                    forceUser.AvailableAbilityPoints,
                    alignmentValue.ToString("P0"),
                    forceUser.Leveling.ForceExperience.ToString("0.00"),
                    forceUser.Leveling.XPRequiredForNextLevel,
                    forceUser.unlockedAbiliities.Count()
                ));
            }

            return new GizmoResult(GizmoState.Clear);
        }
    }
}