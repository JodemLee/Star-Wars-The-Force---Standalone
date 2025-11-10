using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.Steam;

namespace TheForce_Standalone.Generic
{
    internal class CompAbilityEffect_GiveHediffInCone : CompAbilityEffect_GiveHediff
    {
        private readonly List<IntVec3> tmpCells = new List<IntVec3>();

        // Add these fields for width adjustment
        private float currentLineWidth;
        private const float MinLineWidth = 10f;
        private const float MaxLineWidth = 100f;
        private const float WidthIncrement = 10f;

        public new CompProperties_AbilityGiveHediffInCone Props => (CompProperties_AbilityGiveHediffInCone)props;

        private Pawn Pawn => parent?.pawn;
        private Map PawnMap => Pawn?.Map;

        public override void Initialize(AbilityCompProperties props)
        {
            base.Initialize(props);
            currentLineWidth = Props?.lineWidthEnd ?? 45f;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!target.IsValid || PawnMap == null)
                return;

            List<IntVec3> affectedCells = AffectedCells(target);
            if (affectedCells == null || affectedCells.Count == 0)
                return;

            // Apply hediff to all pawns in the cone area
            foreach (IntVec3 cell in affectedCells)
            {
                if (!cell.InBounds(PawnMap))
                    continue;

                foreach (Pawn pawn in cell.GetThingList(PawnMap).OfType<Pawn>())
                {
                    if (IsValidTarget(pawn))
                    {
                        ApplyToPawn(pawn, target.Pawn);
                    }
                }
            }
        }

        private void ApplyToPawn(Pawn target, Pawn other)
        {
            if (target == null)
                return;

            if (Props.ignoreSelf && target == Pawn)
                return;

            List<BodyPartRecord> targetParts = GetTargetBodyParts(target);

            if (Props.onlyApplyToSelf)
            {
                ApplyInner(Pawn, other, targetParts);
            }
            else if (Props.applyToTarget)
            {
                ApplyInner(target, other, targetParts);
            }
            else if (Props.applyToSelf)
            {
                ApplyInner(Pawn, other, targetParts);
            }
        }

        private List<BodyPartRecord> GetTargetBodyParts(Pawn pawn)
        {
            List<BodyPartRecord> targetParts = new List<BodyPartRecord>();

            if (Props.bodyPartGroup == null && Props.bodyPartTag == null)
            {
                // If no specific body part targeting, return null to affect whole body
                return null;
            }

            // Get all matching body parts
            IEnumerable<BodyPartRecord> eligibleParts = pawn.health.hediffSet.GetNotMissingParts()
                .Where(part =>
                    (Props.bodyPartGroup == null || part.groups.Contains(Props.bodyPartGroup)) &&
                    (Props.bodyPartTag == null || part.def.tags.Contains(Props.bodyPartTag)));

            List<BodyPartRecord> eligiblePartsList = eligibleParts.ToList();

            if (eligiblePartsList.Count == 0)
            {
                // If no specific part found and we should affect whole body
                return Props.affectWholeBodyIfNoPartFound ? null : new List<BodyPartRecord>();
            }

            if (Props.affectRandomPartFromGroup)
            {
                // Return one random part
                targetParts.Add(eligiblePartsList.RandomElement());
            }
            else
            {
                // Return ALL matching parts
                targetParts.AddRange(eligiblePartsList);
            }

            return targetParts;
        }

        // Override to use body parts parameter - apply to each matching body part
        private void ApplyInner(Pawn target, Pawn other, List<BodyPartRecord> bodyParts)
        {
            if (target == null)
                return;

            if (bodyParts == null)
            {
                // Apply to whole body (no specific part)
                ApplyToSinglePart(target, other, null);
                return;
            }

            // Apply to each matching body part
            foreach (BodyPartRecord bodyPart in bodyParts)
            {
                ApplyToSinglePart(target, other, bodyPart);
            }
        }

        private void ApplyToSinglePart(Pawn target, Pawn other, BodyPartRecord bodyPart)
        {
            // Create hediff
            Hediff hediff = HediffMaker.MakeHediff(Props.hediffDef, target, bodyPart);

            // Set severity if specified
            if (Props.severity >= 0f)
            {
                hediff.Severity = Props.severity;
            }

            // Add the hediff
            target.health.AddHediff(hediff, bodyPart);
        }

        private bool IsValidTarget(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed)
                return false;

            if (Props.ignoreSelf && pawn == Pawn)
                return false;

            // Check faction restrictions if any
            if (Props.factionHostileOnly && !pawn.Faction.HostileTo(Pawn.Faction))
                return false;

            if (Props.factionNonHostileOnly && pawn.Faction.HostileTo(Pawn.Faction))
                return false;

            return true;
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            if (PawnMap == null || !target.IsValid)
                return;

            // Handle key presses for width adjustment
            if (KeyBindingDefOf.Designator_RotateRight.JustPressed)
            {
                currentLineWidth = Mathf.Min(currentLineWidth + WidthIncrement, MaxLineWidth);
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
            }
            if (KeyBindingDefOf.Designator_RotateLeft.JustPressed)
            {
                currentLineWidth = Mathf.Max(currentLineWidth - WidthIncrement, MinLineWidth);
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
            }

            // Draw the rotation controls for visual feedback
            DrawRotationControls();

            // Draw the cone preview
            GenDraw.DrawFieldEdges(AffectedCells(target), Valid(target) ? Color.white : Color.red);
        }

        private void DrawRotationControls()
        {
            if (Find.CameraDriver.CurrentZoom != CameraZoomRange.Closest)
                return;

            // Calculate position near the pawn
            Vector2 screenPos = Pawn.DrawPos.MapToUIPosition();
            float leftX = screenPos.x - 100f;
            float bottomY = screenPos.y - 30f;

            // Draw custom controls that show the current width
            Rect winRect = new(leftX, bottomY - 90f, 200f, 90f);
            Find.WindowStack.ImmediateWindow(19485739, winRect, WindowLayer.GameUI, delegate
            {
                Widgets.DrawWindowBackground(winRect.AtZero());
                Rect labelRect = new(0f, 5f, winRect.width, 25f);
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(labelRect, $"Cone Width: {currentLineWidth:F0}°");
                Text.Anchor = TextAnchor.UpperLeft;
                Rect leftButtonRect = new(winRect.width / 2f - 64f - 5f, 30f, 64f, 64f);
                Widgets.ButtonImage(leftButtonRect, TexUI.RotLeftTex);
                Rect rightButtonRect = new(winRect.width / 2f + 5f, 30f, 64f, 64f);
                Widgets.ButtonImage(rightButtonRect, TexUI.RotRightTex);
                if (!SteamDeck.IsSteamDeck)
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(leftButtonRect, KeyBindingDefOf.Designator_RotateLeft.MainKeyLabel);
                    Widgets.Label(rightButtonRect, KeyBindingDefOf.Designator_RotateRight.MainKeyLabel);
                    Text.Anchor = TextAnchor.UpperLeft;
                }
            });
        }

        private List<IntVec3> AffectedCells(LocalTargetInfo target)
        {
            tmpCells.Clear();

            if (Pawn == null || PawnMap == null || Props == null || !target.IsValid)
                return tmpCells;

            Vector3 startVec = Pawn.Position.ToVector3Shifted().Yto0();
            Vector3 targetVec = target.Cell.ToVector3Shifted().Yto0();

            if (startVec == targetVec)
                return tmpCells;

            Vector3 direction = (targetVec - startVec).normalized;

            int numCells = GenRadial.NumCellsInRadius(Props.range);
            for (int i = 0; i < numCells; i++)
            {
                IntVec3 cell = Pawn.Position + GenRadial.RadialPattern[i];
                if (!cell.InBounds(PawnMap) || cell == Pawn.Position)
                    continue;

                Vector3 cellVec = cell.ToVector3Shifted().Yto0();
                Vector3 toCell = (cellVec - startVec).normalized;

                float distance = Vector3.Distance(cellVec, startVec);
                if (distance > Props.range)
                    continue;

                float currentConeAngle = currentLineWidth * (distance / Props.range);
                float angle = Vector3.Angle(direction, toCell);

                if (angle <= currentConeAngle * 0.5f)
                {
                    tmpCells.Add(cell);
                }
            }

            return tmpCells;
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!target.Cell.IsValid || Pawn == null)
                return false;

            if (target.Cell.DistanceTo(Pawn.Position) > Props.range)
            {
                if (throwMessages)
                    Messages.Message("AbilityTargetOutOfRange".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (!GenSight.LineOfSight(Pawn.Position, target.Cell, Pawn.Map, true))
            {
                if (throwMessages)
                    Messages.Message("AbilityTargetNoLineOfSight".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }

        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            if (Props?.effecterDef != null && Pawn != null)
            {
                yield return new PreCastAction
                {
                    action = delegate (LocalTargetInfo a, LocalTargetInfo b)
                    {
                        if (!a.IsValid || PawnMap == null)
                            return;

                        var effecter = Props.effecterDef.Spawn(Pawn.Position, a.Cell, PawnMap);
                        parent.AddEffecterToMaintain(effecter, Pawn.Position, a.Cell, 60, PawnMap);
                    },
                    ticksAwayFromCast = 17
                };
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentLineWidth, "currentLineWidth", Props?.lineWidthEnd ?? 45f);
        }
    }

    public class CompProperties_AbilityGiveHediffInCone : CompProperties_AbilityGiveHediff
    {
        public float range;
        public float lineWidthEnd;
        public EffecterDef effecterDef;

        public bool factionHostileOnly = false;
        public bool factionNonHostileOnly = false;

        public BodyPartGroupDef bodyPartGroup;
        public BodyPartTagDef bodyPartTag;
        public bool affectRandomPartFromGroup = false;
        public bool affectWholeBodyIfNoPartFound = true;

        public CompProperties_AbilityGiveHediffInCone()
        {
            compClass = typeof(CompAbilityEffect_GiveHediffInCone);
        }
    }
}