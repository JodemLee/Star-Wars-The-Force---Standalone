using RimWorld;
using System.Linq;
using Verse;

namespace TheForce_Standalone.HediffComps
{
    internal class HediffComp_Enlighten : HediffComp
    {
        private SkillDef affectedSkill;
        private int originalLevel;

        public HediffCompProperties_Enlighten Props => (HediffCompProperties_Enlighten)props;

        public override void CompPostMake()
        {
            base.CompPostMake();
            FindAndMaxHighestSkill();
        }

        private void FindAndMaxHighestSkill()
        {
            var skills = parent.pawn.skills?.skills;
            if (skills == null || skills.Count == 0) return;

            var highestSkill = skills.OrderByDescending(s => s.Level).FirstOrDefault();
            if (highestSkill == null) return;

            affectedSkill = highestSkill.def;
            originalLevel = highestSkill.Level;
            highestSkill.Level = 20;
            Messages.Message(
                "Force.Enlighten.SkillIncreased".Translate(
                    parent.pawn.LabelShortCap,
                    affectedSkill.LabelCap,
                    originalLevel,
                    20
                ),
                parent.pawn,
                MessageTypeDefOf.PositiveEvent
            );
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            RestoreOriginalSkill();
        }

        private void RestoreOriginalSkill()
        {
            if (affectedSkill == null) return;

            var skillRecord = parent.pawn.skills?.GetSkill(affectedSkill);
            if (skillRecord != null)
            {
                skillRecord.Level = originalLevel;
                Messages.Message(
                    "Force.Enlighten.SkillReverted".Translate(
                        parent.pawn.LabelShortCap,
                        affectedSkill.LabelCap,
                        originalLevel
                    ),
                    parent.pawn,
                    MessageTypeDefOf.NeutralEvent
                );
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Defs.Look(ref affectedSkill, "affectedSkill");
            Scribe_Values.Look(ref originalLevel, "originalLevel");
        }
    }

    internal class HediffCompProperties_Enlighten : HediffCompProperties
    {
        public HediffCompProperties_Enlighten()
        {
            compClass = typeof(HediffComp_Enlighten);
        }
    }
}