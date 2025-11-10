using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Apprenticeship
{
    internal class Gizmo_Apprentice : Gizmo
    {
        public const int InRectPadding = 6;

        private static readonly Color EmptyBlockColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        private static readonly Color FilledBlockColor = Color.yellow;

        private static readonly Color ExcessBlockColor = ColorLibrary.Red;

        private Hediff_Master hediff;

        public override bool Visible => Find.Selector.SelectedPawns.Count == 1;

        public Gizmo_Apprentice(Hediff_Master hediff)
        {
            this.hediff = hediff;
            Order = -90f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect rect2 = rect.ContractedBy(InRectPadding);
            Widgets.DrawWindowBackground(rect);

            int totalBandwidth = hediff.apprenticeCapacity;
            int usedBandwidth = hediff.apprentices.Count;
            string text = $"{usedBandwidth} / {totalBandwidth}";

            // Tooltip
            string tooltipText = $"Force.Apprentices".Translate().Colorize(ColoredText.TipSectionTitleColor) + $": {text}\n\n";
            if (usedBandwidth > 0)
            {
                IEnumerable<string> entries = hediff.apprentices.Select(p => p.LabelCap);
                tooltipText += $"Force.ApprenticeUsage".Translate() + "\n" + entries.ToLineList(" - ");
            }
            TooltipHandler.TipRegion(rect, tooltipText);

            // Labels
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect labelRect = new(rect2.x, rect2.y, rect2.width, 30f);
            Widgets.Label(labelRect, "Force.Apprentices".Translate());
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(labelRect, text);

            // Draw bandwidth bars
            Rect bandwidthBarRect = new(rect2.x, rect2.y + 30f, rect2.width, rect2.height - 30f);
            float usedBandwidthRatio = (float)usedBandwidth / (float)totalBandwidth;
            float usedBandwidthWidth = bandwidthBarRect.width * usedBandwidthRatio;

            Rect filledBandwidthRect = new(bandwidthBarRect.x, bandwidthBarRect.y, usedBandwidthWidth, bandwidthBarRect.height);
            Widgets.DrawRectFast(filledBandwidthRect, FilledBlockColor);

            Rect emptyBandwidthRect = new(bandwidthBarRect.x + usedBandwidthWidth, bandwidthBarRect.y, bandwidthBarRect.width - usedBandwidthWidth, bandwidthBarRect.height);
            Widgets.DrawRectFast(emptyBandwidthRect, EmptyBlockColor);

            return new GizmoResult(GizmoState.Clear);
        }

        public override float GetWidth(float maxWidth)
        {
            return 136f;
        }
    }
}
