using RimWorld;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    public class CompAbilityEffect_SithResurrection : CompAbilityEffect_Resurrect
    {
        public new CompProperties_SithResurrection Props => (CompProperties_SithResurrection)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (target.HasThing && target.Thing is Corpse corpse)
            {
                Pawn innerPawn = corpse.InnerPawn;
                if (innerPawn != null)
                {
                    ResurrectionUtility.TryResurrect(innerPawn);
                    ApplySithEffects(innerPawn);
                }
            }
        }

        private void ApplySithEffects(Pawn resurrectedPawn)
        {

            Hediff hediff = HediffMaker.MakeHediff(ForceDefOf.Force_SithZombie, resurrectedPawn);
            resurrectedPawn.health.AddHediff(hediff);

            resurrectedPawn.SetFaction(parent.pawn.Faction);
            if ( resurrectedPawn.IsPrisoner && ModsConfig.IdeologyActive)
            {
                resurrectedPawn.guest.SetGuestStatus(parent.pawn.Faction, GuestStatus.Slave);
            }

            MoteMaker.MakeAttachedOverlay(resurrectedPawn, ThingDefOf.Mote_ResurrectFlash, Vector3.zero);
            Messages.Message("Force.SithResurrection_Success".Translate(resurrectedPawn.LabelShortCap),
                            resurrectedPawn,
                            MessageTypeDefOf.PositiveEvent);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
            {
                return false;
            }

            if (target.HasThing && target.Thing is Corpse corpse)
            {
                if (corpse.InnerPawn == null)
                {
                    if (throwMessages)
                    {
                        Messages.Message("CannotResurrectNoPawn".Translate(), corpse, MessageTypeDefOf.RejectInput);
                    }
                    return false;
                }

                if (corpse.InnerPawn.RaceProps.IsMechanoid)
                {
                    if (throwMessages)
                    {
                        Messages.Message("CannotResurrectMechanoids".Translate(), corpse, MessageTypeDefOf.RejectInput);
                    }
                    return false;
                }
            }

            return true;
        }
    }

    public class CompProperties_SithResurrection : CompProperties_Resurrect
    {
        public CompProperties_SithResurrection()
        {
            compClass = typeof(CompAbilityEffect_SithResurrection);
        }
    }
}

