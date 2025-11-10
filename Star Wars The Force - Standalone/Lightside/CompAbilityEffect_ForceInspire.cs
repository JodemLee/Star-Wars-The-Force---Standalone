using RimWorld;
using Verse;

namespace TheForce_Standalone.Lightside
{
    internal class CompAbilityEffect_ForceInspire : CompAbilityEffect
    {
        public new CompProperties_AbilityStopMentalState Props => (CompProperties_AbilityStopMentalState)props;

        public override bool HideTargetPawnTooltip => true;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.CatatonicBreakdown);
            if (firstHediffOfDef != null)
            {
                pawn.health.RemoveHediff(firstHediffOfDef);
            }
            pawn?.MentalState?.RecoverFromState();
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return Valid(target);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = target.Pawn;
            if (pawn != null)
            {
                if (pawn.MentalStateDef != null && Props.exceptions.Contains(pawn.MentalStateDef))
                {
                    if (throwMessages)
                    {
                        Messages.Message("AbilityDoesntWorkOnMentalState".Translate(parent.def.label, pawn.MentalStateDef.label), pawn, MessageTypeDefOf.RejectInput, historical: false);
                    }
                    return false;
                }
            }
            return true;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            return null;
        }
    }
}
