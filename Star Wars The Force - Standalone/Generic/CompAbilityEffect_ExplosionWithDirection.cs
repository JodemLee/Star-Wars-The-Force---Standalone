using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.Steam;

namespace TheForce_Standalone.Generic
{
    internal class CompAbilityEffect_ExplosionWithDirection : CompAbilityEffect
    {
        private readonly List<IntVec3> tmpCells = new List<IntVec3>();
        private Effecter maintainedEffecter;
        private int lastDamageTick = -1;
        private const int DamageInterval = 15;
        private int effecterEndTick = -1;
        private const int EffecterDuration = 60;
        private IntVec3 currentTarget;
        private bool isActive = false;

        // Add these fields for width adjustment
        private float currentLineWidth;
        private const float MinLineWidth = 10f;
        private const float MaxLineWidth = 100f;
        private const float WidthIncrement = 10f;

        public new CompProperties_ExplosionWithDirection Props => (CompProperties_ExplosionWithDirection)props;

        // Use currentLineWidth instead of directly reading from props
        float coneAngle => currentLineWidth;

        private Pawn Pawn => parent?.pawn;
        private Map PawnMap => Pawn?.Map;

        public override void Initialize(AbilityCompProperties props)
        {
            base.Initialize(props);
            currentLineWidth = Props?.lineWidthEnd ?? 45f;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (!target.IsValid || PawnMap == null)
                return;

            currentTarget = target.Cell;
            isActive = true;
            effecterEndTick = Find.TickManager.TicksGame + EffecterDuration;
            ApplyDamageToArea(target);
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

            GenDraw.DrawFieldEdges(AffectedCells(target));
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
                Widgets.Label(labelRect, $"Line Width: {currentLineWidth:F0}°");
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

        private void ApplyDamageToArea(LocalTargetInfo target)
        {
            if (!target.IsValid || PawnMap == null || Props == null)
                return;

            List<IntVec3> affectedCells = AffectedCells(target);
            if (affectedCells == null || affectedCells.Count == 0)
                return;

            StunPawnsInArea(affectedCells);

            SoundDef explosionSound = Props.soundExplode ?? SoundDefOf.Tick_Tiny;

            List<Thing> ignoredThings = null;
            if (!Props.affectAllies && Pawn != null)
            {
                ignoredThings = new List<Thing>();
                foreach (IntVec3 cell in affectedCells)
                {
                    if (!cell.InBounds(PawnMap))
                        continue;

                    List<Thing> thingsInCell = cell.GetThingList(PawnMap);
                    foreach (Thing thing in thingsInCell)
                    {
                        if (thing is Pawn targetPawn && targetPawn.Faction != null && Pawn.Faction != null &&
                            targetPawn.Faction == Pawn.Faction)
                        {
                            ignoredThings.Add(thing);
                        }
                    }
                }
            }

            GenExplosion.DoExplosion(
                center: target.Cell,
                map: PawnMap,
                radius: 0f,
                damType: Props.damageDef,
                instigator: Pawn,
                damAmount: Props.damAmount,
                armorPenetration: Props.armorPenetration,
                explosionSound: explosionSound,
                weapon: null,
                projectile: null,
                intendedTarget: null,
                overrideCells: affectedCells,
                ignoredThings: ignoredThings
            );
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!isActive || PawnMap == null)
                return;

            bool effecterActive = effecterEndTick == -1 || Find.TickManager.TicksGame < effecterEndTick;

            if (effecterActive && Find.TickManager.TicksGame >= lastDamageTick + DamageInterval)
            {
                ApplyDamageToArea(new LocalTargetInfo(currentTarget));
                lastDamageTick = Find.TickManager.TicksGame;
            }
            else if (!effecterActive)
            {
                maintainedEffecter?.Cleanup();
                maintainedEffecter = null;
                isActive = false;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastDamageTick, "lastDamageTick", -1);
            Scribe_Values.Look(ref currentTarget, "currentTarget");
            Scribe_Values.Look(ref effecterEndTick, "effecterEndTick", -1);
            Scribe_Values.Look(ref currentLineWidth, "currentLineWidth", Props?.lineWidthEnd ?? 45f);
        }

        private void StunPawnsInArea(List<IntVec3> affectedCells)
        {
            if (PawnMap == null || affectedCells == null)
                return;

            foreach (IntVec3 cell in affectedCells)
            {
                if (!cell.InBounds(PawnMap))
                    continue;

                foreach (Thing thing in cell.GetThingList(PawnMap))
                {
                    if (thing is Pawn targetPawn && targetPawn.health?.hediffSet?.hediffs != null)
                    {
                        bool hasAddedParts = targetPawn.health.hediffSet.hediffs.Any(h => h.def.countsAsAddedPartOrImplant);
                        if (hasAddedParts)
                        {
                            targetPawn.stances?.stunner?.StunFor((int)(Props.stunDuration * 60), Pawn);
                        }
                    }
                }
            }
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
            return target.Cell.IsValid &&
                   Pawn != null &&
                   target.Cell.DistanceTo(Pawn.Position) <= Props.range &&
                   GenSight.LineOfSight(Pawn.Position, target.Cell, Pawn.Map);
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

                        maintainedEffecter = Props.effecterDef.Spawn(Pawn.Position, a.Cell, PawnMap);
                        parent.AddEffecterToMaintain(maintainedEffecter, Pawn.Position, a.Cell, EffecterDuration, PawnMap);
                        currentTarget = a.Cell;
                        effecterEndTick = Find.TickManager.TicksGame + EffecterDuration;
                    },
                    ticksAwayFromCast = 17
                };
            }
        }
    }

    public class CompProperties_ExplosionWithDirection : CompProperties_AbilityEffect
    {
        public float range;
        public float lineWidthEnd;
        public int damAmount = -1;
        public DamageDef damageDef;
        public float armorPenetration = -1f;
        public SoundDef soundExplode;
        public float stunDuration = 3f;
        public EffecterDef effecterDef;
        public bool canHitFilledCells;
        public bool affectAllies = false;

        public CompProperties_ExplosionWithDirection()
        {
            compClass = typeof(CompAbilityEffect_ExplosionWithDirection);
        }
    }
}