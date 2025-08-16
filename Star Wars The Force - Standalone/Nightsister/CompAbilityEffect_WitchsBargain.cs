using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TheForce_Standalone.Nightsister
{
    public class CompAbilityEffect_WitchsBargain : CompAbilityEffect
    {
        private const int DurationTicks = 60000;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (!(target.Thing is Pawn targetPawn)) return;

            Find.WindowStack.Add(new Dialog_WitchsBargain(targetPawn, this));
        }

        public void ApplySwap(Pawn target, SkillDef firstSkill, SkillDef secondSkill)
        {
            var firstSkillRecord = target.skills.GetSkill(firstSkill);
            var secondSkillRecord = target.skills.GetSkill(secondSkill);

            var hediff = (Hediff_WitchsBargain)HediffMaker.MakeHediff(HediffDef.Named("Force_WitchBargainHediff"), target);
            hediff.firstSkill = firstSkill;
            hediff.secondSkill = secondSkill;
            hediff.originalFirstSkillLevel = firstSkillRecord.Level;
            hediff.originalSecondSkillLevel = secondSkillRecord.Level;

            hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = DurationTicks;

            // Swap skill levels
            int tempLevel = firstSkillRecord.Level;
            firstSkillRecord.Level = secondSkillRecord.Level;
            secondSkillRecord.Level = tempLevel;

            target.health.AddHediff(hediff);

            // Visual effect
            FleckMaker.ThrowLightningGlow(target.DrawPos, target.Map, 1.5f);
            SoundDefOf.PsychicPulseGlobal.PlayOneShot(new TargetInfo(target.Position, target.Map));
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!(target.Thing is Pawn pawn))
            {
                if (throwMessages)
                    Messages.Message("MustTargetPawn".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            if (pawn.skills == null)
            {
                if (throwMessages)
                    Messages.Message("TargetHasNoSkills".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            return base.Valid(target, throwMessages);
        }
    }

    public class Hediff_WitchsBargain : HediffWithComps
    {
        public SkillDef firstSkill;
        public SkillDef secondSkill;
        public int originalFirstSkillLevel;
        public int originalSecondSkillLevel;

        public override void PostRemoved()
        {
            base.PostRemoved();
            RevertSkills();
        }

        private void RevertSkills()
        {
            if (firstSkill != null && secondSkill != null)
            {
                var firstSkillRecord = pawn.skills.GetSkill(firstSkill);
                var secondSkillRecord = pawn.skills.GetSkill(secondSkill);
                firstSkillRecord.Level = originalFirstSkillLevel;
                secondSkillRecord.Level = originalSecondSkillLevel;

                // Visual effect when reverting
                FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1f);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref firstSkill, "firstSkill");
            Scribe_Defs.Look(ref secondSkill, "secondSkill");
            Scribe_Values.Look(ref originalFirstSkillLevel, "originalFirstSkillLevel");
            Scribe_Values.Look(ref originalSecondSkillLevel, "originalSecondSkillLevel");
        }
    }

    public class Dialog_WitchsBargain : Window
    {
        private Pawn targetPawn;
        private CompAbilityEffect_WitchsBargain abilityComp;
        private SkillDef selectedFirstSkill;
        private SkillDef selectedSecondSkill;

        public override Vector2 InitialSize => new Vector2(500f, 600f);

        public Dialog_WitchsBargain(Pawn targetPawn, CompAbilityEffect_WitchsBargain abilityComp)
        {
            this.targetPawn = targetPawn;
            this.abilityComp = abilityComp;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 40f), "Witch's Bargain - Select Skills to Swap");
            Text.Font = GameFont.Small;

            float y = 50f;
            foreach (var skillDef in DefDatabase<SkillDef>.AllDefs)
            {
                var skillRecord = targetPawn.skills.GetSkill(skillDef);
                Rect skillRect = new Rect(0f, y, inRect.width, 30f);

                // Highlight selected skills
                if (skillDef == selectedFirstSkill || skillDef == selectedSecondSkill)
                {
                    Widgets.DrawHighlightSelected(skillRect);
                }

                // Skill label with level
                Widgets.Label(skillRect, $"{skillDef.LabelCap}: {skillRecord.Level}");

                // Button to select as first skill
                if (Widgets.ButtonInvisible(skillRect))
                {
                    if (selectedFirstSkill == null)
                    {
                        selectedFirstSkill = skillDef;
                    }
                    else if (selectedSecondSkill == null && skillDef != selectedFirstSkill)
                    {
                        selectedSecondSkill = skillDef;
                    }
                    else
                    {
                        selectedFirstSkill = skillDef;
                        selectedSecondSkill = null;
                    }
                }

                y += 32f;
            }

            // Confirm button when two skills are selected
            if (selectedFirstSkill != null && selectedSecondSkill != null)
            {
                Rect confirmRect = new Rect(0f, y + 20f, inRect.width, 40f);
                if (Widgets.ButtonText(confirmRect, "Swap Skills"))
                {
                    abilityComp.ApplySwap(targetPawn, selectedFirstSkill, selectedSecondSkill);
                    this.Close();
                }
            }
        }
    }
}
