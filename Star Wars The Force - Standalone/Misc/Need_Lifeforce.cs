using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TheForce_Standalone
{
    [StaticConstructorOnStartup]
    public class Need_Lifeforce : Need
    {
        public const float BaseLifeforceFallPerTick = 2.6666667E-05f; // Same as food for consistency

        private const float CriticalThreshold = 0.2f; // Mental break threshold
        private const float ExtremeThreshold = 0.05f; // Physical deterioration threshold
        private const float WarningThreshold = 0.5f; // Warning level

        private static readonly Texture2D BarInstantMarkerTex = ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarker");

        private static readonly Texture2D NeedUnitDividerTex = ContentFinder<Texture2D>.Get("UI/Misc/NeedUnitDivider");

        public enum LifeforceCategory
        {
            Sated,
            Hungry,
            UrgentlyHungry,
            Starving
        }

        public bool Starving => CurCategory == LifeforceCategory.Starving;

        public float PercentageThreshUrgentlyHungry => CriticalThreshold;
        public float PercentageThreshHungry => WarningThreshold;

        public LifeforceCategory CurCategory
        {
            get
            {
                if (base.CurLevelPercentage <= ExtremeThreshold)
                {
                    return LifeforceCategory.Starving;
                }
                if (base.CurLevelPercentage <= CriticalThreshold)
                {
                    return LifeforceCategory.UrgentlyHungry;
                }
                if (base.CurLevelPercentage <= WarningThreshold)
                {
                    return LifeforceCategory.Hungry;
                }
                return LifeforceCategory.Sated;
            }
        }

        public float LifeforceFallPerTick => LifeforceFallPerTickAssumingCategory(CurCategory);

        public override int GUIChangeArrow
        {
            get
            {
                if (IsFrozen) return 0;
                return -1; // Always decaying when not frozen
            }
        }

        public override float MaxLevel => 1f;

        public float LifeforceWanted => MaxLevel - CurLevel;

        public Need_Lifeforce(Pawn pawn) : base(pawn)
        {
            threshPercents = new List<float> { WarningThreshold, CriticalThreshold, ExtremeThreshold };
        }

        public override void NeedInterval()
        {
            if (!IsFrozen)
            {
                CurLevel -= LifeforceFallPerTick * 150f;
            }
            ApplyLifeforceEffects();
        }

        private void ApplyLifeforceEffects()
        {
            if (pawn.Dead) return;

            // Apply effects based on current category
            switch (CurCategory)
            {
                case LifeforceCategory.Starving:
                    HandleStarvingEffects();
                    break;
                case LifeforceCategory.UrgentlyHungry:
                    HandleCriticalEffects();
                    break;
                case LifeforceCategory.Hungry:
                    HandleWarningEffects();
                    break;
                case LifeforceCategory.Sated:
                    HandleSatedState();
                    break;
            }
        }

        private void HandleStarvingEffects()
        {
            // Extreme level - physical deterioration and mental breaks
            if (Rand.MTBEventOccurs(0.5f, 1f, 150f)) // MTB 30 minutes
            {
                pawn.mindState.mentalStateHandler.TryStartMentalState(
                    MentalStateDefOf.Berserk,
                    "ExtremeLifeforceStarvation",
                    forced: true
                );
            }

            if (Rand.MTBEventOccurs(1f, 1f, 150f)) // MTB 1 hour
            {
                BodyPartRecord part = pawn.health.hediffSet.GetRandomNotMissingPart(
                    DamageDefOf.Psychic,
                    BodyPartHeight.Undefined,
                    BodyPartDepth.Undefined
                );
                if (part != null)
                {
                    pawn.TakeDamage(new DamageInfo(
                        DamageDefOf.Psychic,
                        8f,
                        1f,
                        -1f,
                        null,
                        part
                    ));
                }
            }

            HealthUtility.AdjustSeverity(pawn, HediffDef.Named("LifeforceDrainPain"), 0.5f);
        }

        private void HandleCriticalEffects()
        {
            // Critical level - mental breaks and pain
            if (Rand.MTBEventOccurs(2f, 1f, 150f)) // MTB 2 hours
            {
                pawn.mindState.mentalStateHandler.TryStartMentalState(
                    MentalStateDefOf.Berserk,
                    "CriticalLifeforceStarvation",
                    forced: true
                );
            }

        }

        private void HandleWarningEffects()
        {
            // Warning level - minor effects

        }

        private void HandleSatedState()
        {
            // No negative effects when sated
        }

        public float LifeforceFallPerTickAssumingCategory(LifeforceCategory category)
        {
            float baseRate = BaseLifeforceFallRate(pawn.ageTracker.CurLifeStage, pawn.def);

            // Apply multipliers based on category if needed
            float multiplier = 1f;
            switch (category)
            {
                case LifeforceCategory.Starving:
                    multiplier = 1.2f; // Faster decay when starving
                    break;
                case LifeforceCategory.UrgentlyHungry:
                    multiplier = 1.1f;
                    break;
            }

            return baseRate * multiplier;
        }

        // Method to feed on another pawn's lifeforce
        public void FeedOnPawn(Pawn target)
        {
            if (target == null || target.Dead) return;

            float lifeforceGained = CalculateLifeforceFromPawn(target);
            if (target.GetStatValue(StatDefOf.PsychicSensitivity) > 0.5f)
            {
                lifeforceGained *= 3f;
            }

            CurLevel += lifeforceGained;
            ApplyFeedingDamage(target, lifeforceGained);
            FeedEffects(target, lifeforceGained);
        }

        public float CalculateLifeforceFromPawn(Pawn target)
        {
            float baseAmount = 0.1f;
            baseAmount *= target.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
            baseAmount *= target.GetStatValue(StatDefOf.PsychicSensitivity);
            if (target.health.hediffSet.BleedRateTotal > 0.1f)
                baseAmount *= 0.5f;
            return Mathf.Clamp(baseAmount, 0.01f, 0.3f);
        }

        private void ApplyFeedingDamage(Pawn target, float lifeforceTaken)
        {
            float psychicSensitivity = target.GetStatValue(StatDefOf.PsychicSensitivity);
            if (psychicSensitivity <= 0f)
            {
                return;
            }
            float damage = 50f * psychicSensitivity;
            BodyPartRecord part = target.health.hediffSet.GetRandomNotMissingPart(
                DamageDefOf.Psychic,
                BodyPartHeight.Undefined,
                BodyPartDepth.Undefined
            );

            if (part != null)
            {
                target.TakeDamage(new DamageInfo(
                    DamageDefOf.Psychic,
                    damage,
                    1f,
                    -1f,
                    pawn,
                    part
                ));
            }

            if (target.Dead && target.Corpse != null)
            {
                CompRottable compRottable = target.Corpse.TryGetComp<CompRottable>();
                if (compRottable != null)
                {
                    compRottable.RotProgress = float.MaxValue; // Force immediate rotting to dessicated
                }
                target.Corpse.RotStageChanged();
            }
        }

        private void FeedEffects(Pawn target, float amount)
        {
            Messages.Message(
                $"{pawn.LabelShort} drains lifeforce from {target.LabelShort} (+{amount.ToStringPercent()})",
                pawn,
                MessageTypeDefOf.NegativeEvent
            );
        }

        private void AreaFeedEffects(Map map, IntVec3 center, float radius, int victims, float totalAmount)
        {
            Messages.Message(
                $"{pawn.LabelShort} devours the lifeforce of {victims} beings in the area (+{totalAmount.ToStringPercent()})",
                pawn,
                MessageTypeDefOf.ThreatBig
            );
        }

        public override string GetTipString()
        {
            string text = (LabelCap + ": " + CurLevelPercentage.ToStringPercent()).Colorize(ColoredText.TipSectionTitleColor) + " (" + CurLevel.ToString("0.##") + " / " + MaxLevel.ToString("0.##") + ")\n" + def.description;

            switch (CurCategory)
            {
                case LifeforceCategory.Starving:
                    text += "\n\nYour very essence is fading! You must feed immediately or face destruction.";
                    break;
                case LifeforceCategory.UrgentlyHungry:
                    text += "\n\nThe hunger consumes you from within. Find lifeforce to sustain yourself.";
                    break;
                case LifeforceCategory.Hungry:
                    text += "\n\nThe void within you grows. Seek out lifeforce soon.";
                    break;
                case LifeforceCategory.Sated:
                    text += "\n\nTemporarily sated, but the eternal hunger will return.";
                    break;
            }

            return text;
        }



        public override void DrawOnGUI(Rect rect, int maxThresholdMarkers = int.MaxValue, float customMargin = -1f, bool drawArrows = true, bool doTooltip = true, Rect? rectForTooltip = null, bool drawLabel = true)
        {
            if (rect.height > 70f)
            {
                float num = (rect.height - 70f) / 2f;
                rect.height = 70f;
                rect.y += num;
            }
            Rect rect2 = rectForTooltip ?? rect;
            if (Mouse.IsOver(rect2))
            {
                Widgets.DrawHighlight(rect2);
            }
            if (doTooltip && Mouse.IsOver(rect2))
            {
                TooltipHandler.TipRegion(rect2, new TipSignal(() => GetTipString(), rect2.GetHashCode()));
            }
            float num2 = 14f;
            float num3 = ((customMargin >= 0f) ? customMargin : (num2 + 15f));
            if (rect.height < 50f)
            {
                num2 *= Mathf.InverseLerp(0f, 50f, rect.height);
            }
            if (drawLabel)
            {
                Text.Font = ((rect.height > 55f) ? GameFont.Small : GameFont.Tiny);
                Text.Anchor = TextAnchor.LowerLeft;
                Widgets.Label(new(rect.x + num3 + rect.width * 0.1f, rect.y, rect.width - num3 - rect.width * 0.1f, rect.height / 2f), LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;
            }
            Rect rect3 = rect;
            if (drawLabel)
            {
                rect3.y += rect.height / 2f;
                rect3.height -= rect.height / 2f;
            }
            rect3 = new(rect3.x + num3, rect3.y, rect3.width - num3 * 2f, rect3.height - num2);
            if (DebugSettings.ShowDevGizmos)
            {
                float lineHeight = Text.LineHeight;
                Rect rect4 = new(rect3.xMax - lineHeight, rect3.y - lineHeight, lineHeight, lineHeight);
                if (Widgets.ButtonImage(rect4.ContractedBy(4f), TexButton.Plus))
                {
                    OffsetDebugPercent(0.1f);
                }
                if (Mouse.IsOver(rect4))
                {
                    TooltipHandler.TipRegion(rect4, "+ 10%");
                }
                Rect rect5 = new(rect4.xMin - lineHeight, rect3.y - lineHeight, lineHeight, lineHeight);
                if (Widgets.ButtonImage(rect5.ContractedBy(4f), TexButton.Minus))
                {
                    OffsetDebugPercent(-0.1f);
                }
                if (Mouse.IsOver(rect5))
                {
                    TooltipHandler.TipRegion(rect5, "- 10%");
                }
            }

            // Set up thresholds for drawing
            if (threshPercents == null)
            {
                threshPercents = new List<float>();
            }
            threshPercents.Clear();
            threshPercents.Add(PercentageThreshHungry);
            threshPercents.Add(PercentageThreshUrgentlyHungry);
            threshPercents.Add(ExtremeThreshold);

            Rect rect6 = rect3;
            float num4 = 1f;
            if (def.scaleBar && MaxLevel < 1f)
            {
                num4 = MaxLevel;
            }
            rect6.width *= num4;

            // Save original color and set to red for lifeforce bar
            Color originalColor = GUI.color;
            GUI.color = new Color(0.8f, 0.1f, 0.1f, 1f); // Dark red color for lifeforce

            Rect barRect = Widgets.FillableBar(rect6, CurLevelPercentage);

            // Restore original color
            GUI.color = originalColor;

            if (drawArrows)
            {
                Widgets.FillableBarChangeArrows(rect6, GUIChangeArrow);
            }
            if (threshPercents != null)
            {
                for (int num5 = 0; num5 < Mathf.Min(threshPercents.Count, maxThresholdMarkers); num5++)
                {
                    DrawBarThreshold(barRect, threshPercents[num5] * num4);
                }
            }
            if (def.showUnitTicks)
            {
                for (int num6 = 1; (float)num6 < MaxLevel; num6++)
                {
                    DrawBarDivision(barRect, (float)num6 / MaxLevel * num4);
                }
            }
            float curInstantLevelPercentage = CurInstantLevelPercentage;
            if (curInstantLevelPercentage >= 0f)
            {
                DrawBarInstantMarkerAt(rect3, curInstantLevelPercentage * num4);
            }
            if (!def.tutorHighlightTag.NullOrEmpty())
            {
                UIHighlighter.HighlightOpportunity(rect, def.tutorHighlightTag);
            }
            Text.Font = GameFont.Small;
        }

        public static float BaseLifeforceFallRate(LifeStageDef lifeStage, ThingDef pawnDef)
        {
            // Use similar calculation to food need but adjusted for lifeforce
            return lifeStage.hungerRateFactor * pawnDef.race.baseHungerRate * BaseLifeforceFallPerTick;
        }

        public override void SetInitialLevel()
        {
            CurLevelPercentage = 0.75f; // Start mostly sated
        }

        private void DrawBarDivision(Rect barRect, float threshPct)
        {
            float num = 5f;
            Rect rect = new(barRect.x + barRect.width * threshPct - (num - 1f), barRect.y, num, barRect.height);
            if (threshPct < CurLevelPercentage)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.9f);
            }
            else
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
            Rect position = rect;
            position.yMax = position.yMin + 4f;
            GUI.DrawTextureWithTexCoords(position, NeedUnitDividerTex, new(0f, 0.5f, 1f, 0.5f));
            Rect position2 = rect;
            position2.yMin = position2.yMax - 4f;
            GUI.DrawTextureWithTexCoords(position2, NeedUnitDividerTex, new(0f, 0f, 1f, 0.5f));
            Rect position3 = rect;
            position3.yMin = position.yMax;
            position3.yMax = position2.yMin;
            if (position3.height > 0f)
            {
                GUI.DrawTextureWithTexCoords(position3, NeedUnitDividerTex, new(0f, 0.4f, 1f, 0.2f));
            }
            GUI.color = Color.white;
        }
    }
}