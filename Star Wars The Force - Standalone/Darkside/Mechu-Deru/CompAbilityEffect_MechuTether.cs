using RimWorld;
using Verse;

namespace TheForce_Standalone.Darkside.Mechu_Deru
{
    internal class CompAbilityEffect_MechuTether : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (target.Thing == null || parent.pawn == null)
                return;

            var hediffComp = parent.pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_MechuLinkImplant)?.TryGetComp<HediffComp_MechuDeru>();
            if (hediffComp == null)
                return;

            if (hediffComp.linkedTarget == target.Thing)
            {
                hediffComp.Unlink();
                Messages.Message("Force_MechuPower.Unlinked".Translate(target.Thing.LabelShort), MessageTypeDefOf.NeutralEvent, false);
            }
            else
            {
                hediffComp.LinkTo(target.Thing);
                Messages.Message("Force_MechuPower.Linked".Translate(target.Thing.LabelShort), MessageTypeDefOf.PositiveEvent, false);
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
                return false;

            if (target.Thing?.TryGetComp<CompPowerTrader>() is { PowerOutput: < 0f })
                return true;

            if (ModsConfig.BiotechActive &&
                target.Thing is Pawn { RaceProps.IsMechanoid: true, needs.energy: { } } p &&
                p.IsColonyMech)
                return true;

            if (target.Thing is Building_Battery || target.Thing?.TryGetComp<CompPowerBattery>() != null)
                return true;

            if (throwMessages)
                Messages.Message("Force.MustConsumePower".Translate(), MessageTypeDefOf.RejectInput, false);

            return false;
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return Valid(target, false);
        }
    }
}
