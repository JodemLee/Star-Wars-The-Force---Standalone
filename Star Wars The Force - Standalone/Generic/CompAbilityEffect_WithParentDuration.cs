using RimWorld;
using System.Linq;
using TheForce_Standalone.HediffComps;
using Verse;

namespace TheForce_Standalone
{
    internal class CompAbilityEffect_WithParentDuration : CompAbilityEffect
    {
        public new CompProperties_AbilityEffectWithDuration Props => (CompProperties_AbilityEffectWithDuration)props;

        public float GetDurationSeconds(Pawn target)
        {
            if (Props.durationSecondsOverride != FloatRange.Zero)
            {
                return Props.durationSecondsOverride.RandomInRange;
            }
            float num = parent.def.GetStatValueAbstract(StatDefOf.Ability_Duration, parent.pawn);
            if (Props.durationMultiplier != null)
            {
                num *= parent.pawn.GetStatValue(Props.durationMultiplier);
            }
            return num;
        }
    }

    internal class CompAbilityEffect_GiveHediff : CompAbilityEffect_WithParentDuration
    {
        public new CompProperties_AbilityGiveHediff Props => (CompProperties_AbilityGiveHediff)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (!Props.ignoreSelf || target.Pawn != parent.pawn)
            {
                if (!Props.onlyApplyToSelf && Props.applyToTarget)
                {
                    ApplyInner(target.Pawn, parent.pawn);
                }
                if (Props.applyToSelf || Props.onlyApplyToSelf)
                {
                    ApplyInner(parent.pawn, target.Pawn);
                }
            }
        }

        protected void ApplyInner(Pawn target, Pawn other)
        {
            if (target == null)
            {
                return;
            }
            if (TryResist(target))
            {
                MoteMaker.ThrowText(target.DrawPos, target.Map, "Resisted".Translate());
                return;
            }
            if (Props.replaceExisting)
            {
                Hediff firstHediffOfDef = target.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
                if (firstHediffOfDef != null)
                {
                    target.health.RemoveHediff(firstHediffOfDef);
                }
            }
            Hediff hediff = HediffMaker.MakeHediff(Props.hediffDef, target, Props.onlyBrain ? target.health.hediffSet.GetBrain() : null);
            HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (hediffComp_Disappears != null)
            {
                hediffComp_Disappears.ticksToDisappear = GetDurationSeconds(target).SecondsToTicks();
            }
            if (Props.severity >= 0f)
            {
                hediff.Severity = Props.severity;
            }
            HediffComp_Link hediffComp_Link = hediff.TryGetComp<HediffComp_Link>();
            if (hediffComp_Link != null)
            {
                hediffComp_Link.other = other;
                hediffComp_Link.drawConnection = target == parent.pawn;
            }
            HediffComp_LinkWithEffect hediffComp_LinkWithEffect = hediff.TryGetComp<HediffComp_LinkWithEffect>();
            if (hediffComp_LinkWithEffect != null)
            {
                hediffComp_LinkWithEffect.drawConnection = true;
                hediffComp_LinkWithEffect.other = other;
                hediffComp_LinkWithEffect.drawConnection = target == parent.pawn;
            }
            target.health.AddHediff(hediff);
        }

        protected virtual bool TryResist(Pawn pawn)
        {
            return false;
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            base.AICanTargetNow(target);

            if (target.Pawn == null)
            {
                return false;
            }

            bool foundTarget = false;
            bool isHostileAbility = this.Props.hediffDef.isBad;

            foreach (Pawn pawn in this.parent.GetAffectedTargets(target).Select(v => (Pawn)v ?? null))
            {

                if (pawn.Faction != null &&
                    ((pawn.Faction.AllyOrNeutralTo(this.parent.pawn.Faction) && isHostileAbility) ||
                    (pawn.Faction.HostileTo(this.parent.pawn.Faction) && !isHostileAbility)))
                {
                    return false;
                }

                if (!pawn.health.hediffSet.HasHediff(this.Props.hediffDef))
                {
                    foundTarget = true;
                }
            }

            return foundTarget;
        }

       

    }

    internal class CompAbilityEffect_GiveHediffToSelf : CompAbilityEffect_GiveHediff
    {

        public override bool CanCast
        {
            get
            {
                if (ValidatePawnTarget(parent.pawn, Props))
                {
                    return base.CanCast;
                }
                return false;
            }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            ApplyInner(parent.pawn, null);
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            base.AICanTargetNow(target);
            return !parent.pawn.health.hediffSet.HasHediff(Props.hediffDef);
        }

        protected bool ValidatePawnTarget(Pawn pawn, CompProperties_AbilityGiveHediff compProps)
        {
            if (pawn == null)
            {
                return false;
            }
            if (compProps.onlyApplyToSelf && pawn != parent.pawn)
            {
                return false;
            }
            return true;
        }
    }
}
