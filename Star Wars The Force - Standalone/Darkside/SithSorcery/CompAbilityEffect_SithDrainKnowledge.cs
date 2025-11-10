using RimWorld;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    public class CompAbilityEffect_SithDrainKnowledge : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            var targetPawn = target.Pawn;
            if (targetPawn == null) return;

            Find.WindowStack.Add(new Dialog_SithDrainKnowledge(targetPawn, this));
        }

        public void ApplyDrain(Pawn target, SkillDef skillToDrain, int drainAmount)
        {
            var targetSkill = target.skills.GetSkill(skillToDrain);
            var casterSkill = parent.pawn.skills.GetSkill(skillToDrain);

            if (targetSkill == null || casterSkill == null) return;

            targetSkill.Level -= drainAmount;
            casterSkill.Level += drainAmount;

            Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.PsychicShock, target);
            target.health.AddHediff(hediff);

            Messages.Message("Force.SithDrain_Success".Translate(
                parent.pawn.LabelShortCap,
                target.LabelShortCap,
                skillToDrain.LabelCap,
                drainAmount
            ), parent.pawn, MessageTypeDefOf.PositiveEvent);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return target.Pawn != null &&
                   target.Pawn != parent.pawn &&
                   !target.Pawn.Dead;
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Pawn == null)
            {
                if (throwMessages)
                {
                    Messages.Message("Force.SithDrain_InvalidTarget".Translate(),
                                  parent.pawn,
                                  MessageTypeDefOf.RejectInput);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }
    }

    public class Dialog_SithDrainKnowledge : Window
    {
        private Pawn targetPawn;
        private CompAbilityEffect_SithDrainKnowledge abilityEffect;
        private SkillDef selectedSkill;
        private Vector2 scrollPosition;
        private float drainPercentage = 0.5f;
        private int maxDrainable;

        public Dialog_SithDrainKnowledge(Pawn targetPawn, CompAbilityEffect_SithDrainKnowledge abilityEffect)
        {
            this.targetPawn = targetPawn;
            this.abilityEffect = abilityEffect;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(450f, 650f);

        public override void DoWindowContents(Rect inRect)
        {
            // Title Section
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.8f, 0.2f, 0.2f);
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new(0, 5f, inRect.width, 30f), "Force.SithDrain_WindowTitle".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // Target Info Section
            Rect targetRect = new(0, 40f, inRect.width, 30f);
            Widgets.DrawHighlightIfMouseover(targetRect);
            Widgets.Label(targetRect, "Force.SithDrain_TargetLabel".Translate(targetPawn.LabelShortCap));
            if (Mouse.IsOver(targetRect))
            {
                TooltipHandler.TipRegion(targetRect, targetPawn.GetTooltip());
            }

            // Warning Section
            GUI.color = new Color(1f, 0.9f, 0.3f);
            Widgets.Label(new(0, 70f, inRect.width, 40f), "Force.SithDrain_Warning".Translate());
            GUI.color = Color.white;

            // Skill Selection Section
            Rect skillSelectionOuter = new(0, 110f, inRect.width, 300f);
            Widgets.DrawBoxSolid(skillSelectionOuter, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            Rect skillSelectionInner = skillSelectionOuter.ContractedBy(5f);

            GUI.BeginGroup(skillSelectionInner);
            Rect scrollOutRect = new(0, 0, skillSelectionInner.width, skillSelectionInner.height);
            Rect scrollViewRect = new(0, 0, skillSelectionInner.width - 16f, DefDatabase<SkillDef>.AllDefs.Count() * 30f);

            Widgets.BeginScrollView(scrollOutRect, ref scrollPosition, scrollViewRect);

            float y = 0;
            foreach (var skillDef in DefDatabase<SkillDef>.AllDefs.OrderBy(s => s.listOrder))
            {
                var skill = targetPawn.skills.GetSkill(skillDef);
                if (skill == null || skill.TotallyDisabled) continue;

                Rect rowRect = new(0, y, scrollViewRect.width, 28f);
                Widgets.DrawHighlightIfMouseover(rowRect);

                if (skillDef == selectedSkill)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }

                float levelWidth = 40f;
                float barWidth = 120f;
                float labelWidth = scrollViewRect.width - levelWidth - barWidth - 35f;

                Widgets.Label(new(35f, y, labelWidth, 28f), skillDef.LabelCap);

                Rect barRect = new(35f + labelWidth, y + 5f, barWidth, 18f);
                Widgets.FillableBar(barRect, skill.Level / 20f);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(barRect, "Force.SithDrain_SkillLevel".Translate(skill.Level));
                Text.Anchor = TextAnchor.UpperLeft;

                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedSkill = skillDef;
                    UpdateMaxDrainable();
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }

                y += 28f;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();

            // Explanation Section
            Rect explanationRect = new(0, 420f, inRect.width, 80f);
            string explanationText = selectedSkill == null
                ? "Force.SithDrain_SelectPrompt".Translate()
                : GetDrainExplanationText();

            Widgets.Label(explanationRect, explanationText);

            // Drain Controls Section
            if (selectedSkill != null)
            {
                int drainAmount = Mathf.FloorToInt(maxDrainable * drainPercentage);

                Rect explanationRect2 = new(0, 420f, inRect.width, 100f);
                Widgets.Label(explanationRect2, GetDrainExplanationText());

                if (drainPercentage > 0.8f && maxDrainable > 3)
                {
                    Rect warningRect = new(0, 520f, inRect.width, 30f);
                    GUI.color = Color.yellow;
                    Widgets.Label(warningRect, "Force.SithDrain_HighRiskWarning".Translate());
                    GUI.color = Color.white;
                }

                drainPercentage = Widgets.HorizontalSlider(
                    new(10f, 550f, inRect.width - 20f, 30f),
                    drainPercentage,
                    0f,
                    1f,
                    true,
                    roundTo: 0.1f);

                Rect buttonRect = new(inRect.width / 2 - 100f, 590f, 200f, 35f);

                if (drainAmount > 0)
                {
                    float riskFactor = Mathf.Clamp01(drainPercentage * 1.5f);
                    GUI.color = Color.Lerp(new Color(0.5f, 0.2f, 0.2f), new Color(0.8f, 0.1f, 0.1f), riskFactor);

                    if (Widgets.ButtonText(buttonRect, "Force.SithDrain_Button".Translate(drainAmount)))
                    {
                        abilityEffect.ApplyDrain(targetPawn, selectedSkill, drainAmount);
                        Close();
                    }
                }
                else
                {
                    GUI.color = Color.gray;
                    if (Widgets.ButtonText(buttonRect, "Force.SithDrain_CannotDrain".Translate()))
                    {
                        SoundDefOf.ClickReject.PlayOneShotOnCamera();
                    }
                }
                GUI.color = Color.white;
            }
        }

        private string GetDrainExplanationText()
        {
            if (selectedSkill == null)
                return "Force.SithDrain_SelectPrompt".Translate();

            var targetSkill = targetPawn.skills.GetSkill(selectedSkill);
            var casterSkill = abilityEffect.parent.pawn.skills.GetSkill(selectedSkill);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Force.SithDrain_AnalysisHeader".Translate());

            sb.AppendLine("Force.SithDrain_TargetHeader".Translate(targetPawn.LabelShortCap));
            sb.AppendLine(targetSkill.Level <= 1
                ? "Force.SithDrain_TargetMinSkill".Translate()
                : "Force.SithDrain_TargetAvailable".Translate(targetSkill.Level - 1));

            sb.AppendLine("Force.SithDrain_CasterHeader".Translate(abilityEffect.parent.pawn.LabelShortCap));
            sb.AppendLine(casterSkill.Level >= 20
                ? "Force.SithDrain_CasterMaxSkill".Translate()
                : "Force.SithDrain_CasterCapacity".Translate(20 - casterSkill.Level));

            sb.AppendLine("Force.SithDrain_PsychicHeader".Translate());
            float targetSensitivity = targetPawn.GetStatValue(StatDefOf.PsychicSensitivity);
            float casterPower = abilityEffect.parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity);

            sb.AppendLine("Force.SithDrain_TargetSensitivity".Translate(targetSensitivity.ToStringPercent()));
            sb.AppendLine("Force.SithDrain_CasterPower".Translate(casterPower.ToStringPercent()));
            sb.AppendLine("Force.SithDrain_TransferEfficiency".Translate((targetSensitivity * casterPower).ToStringPercent()));

            if (maxDrainable <= 0)
            {
                sb.AppendLine("Force.SithDrain_NoTransferPossible".Translate());
            }
            else
            {
                sb.AppendLine("Force.SithDrain_MaxSafeTransfer".Translate(maxDrainable));
                sb.AppendLine("Force.SithDrain_RiskWarning".Translate());
            }

            return sb.ToString();
        }

        private void UpdateMaxDrainable()
        {
            if (selectedSkill == null)
            {
                maxDrainable = 0;
                return;
            }

            var targetSkill = targetPawn.skills.GetSkill(selectedSkill);
            var casterSkill = abilityEffect.parent.pawn.skills.GetSkill(selectedSkill);

            int targetAvailable = Mathf.Max(0, targetSkill.Level - 1);
            int casterCapacity = Mathf.Max(0, 20 - casterSkill.Level);

            float efficiency = Mathf.Clamp(
                targetPawn.GetStatValue(StatDefOf.PsychicSensitivity) *
                abilityEffect.parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity) * 2f,
                0.5f, 2f);

            maxDrainable = Mathf.FloorToInt(Mathf.Min(
                targetAvailable,
                casterCapacity,
                targetSkill.Level * efficiency
            ));
        }
    }
}