using RimWorld;
using System;
using Verse;

namespace TheForce_Standalone.Apprenticeship.Ritual
{
    internal class RitualOutcomeComp_forceLevelScaled : RitualOutcomeComp_QualitySingleOffset
    {
        [NoTranslate]
        public string roleId;

        public float scaledBy = 1f;

        public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            return Count(ritual, data);
        }

        protected float forceLevelValue(Pawn pawn)
        {
            var forceUser = pawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null) return 0f;

            if (curve == null)
            {
                return forceUser.forceLevel;
            }
            return curve.Evaluate(forceUser.forceLevel);
        }

        public override float Count(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            Pawn pawn = ritual.PawnWithRole(roleId);
            if (pawn == null)
            {
                return 0f;
            }

            var forceUser = pawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null || !forceUser.IsValidForceUser)
            {
                return 0f;
            }

            return forceLevelValue(pawn) * scaledBy;
        }

        public override string GetDesc(LordJob_Ritual ritual = null, RitualOutcomeComp_Data data = null)
        {
            if (ritual == null)
            {
                return labelAbstract;
            }

            Pawn pawn = ritual?.PawnWithRole(roleId);
            if (pawn == null)
            {
                return null;
            }

            var forceUser = pawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null || !forceUser.IsValidForceUser)
            {
                return null;
            }

            float num = forceLevelValue(pawn);
            float num2 = num * scaledBy;
            string text = ((num2 < 0f) ? "" : "+");
            return LabelForDesc.Formatted(pawn.Named("PAWN")) + ": " + "OutcomeBonusDesc_QualitySingleOffset".Translate(text + num2.ToStringPercent()) + ".";
        }

        public override QualityFactor GetQualityFactor(Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation, RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            Pawn pawn = assignments.FirstAssignedPawn(roleId);
            if (pawn == null)
            {
                return null;
            }

            var forceUser = pawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null || !forceUser.IsValidForceUser)
            {
                return null;
            }

            float f = forceUser.forceLevel;
            float num = forceLevelValue(pawn) * scaledBy;

            return new QualityFactor
            {
                label = label.Formatted(pawn.Named("PAWN")),
                count = f.ToString(),
                qualityChange = ((Math.Abs(num) > float.Epsilon) ? "OutcomeBonusDesc_QualitySingleOffset".Translate(num.ToStringWithSign("0.#%")).Resolve() : " - "),
                positive = (num >= 0f),
                quality = num,
                priority = 0f
            };
        }

        public override bool Applies(LordJob_Ritual ritual)
        {
            return true;
        }
    }
}

