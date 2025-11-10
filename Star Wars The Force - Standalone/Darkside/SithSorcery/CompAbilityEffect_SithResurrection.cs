using RimWorld;
using TheForce_Standalone.Darkside.SithSorcery.Alchemy;
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
                    ApplySithEffects(innerPawn);
                }
            }
        }

        private void ApplySithEffects(Pawn resurrectedPawn, int lifespanTicks = -1, Faction faction = null)
        {
            if (ModsConfig.IsActive("lee.theforce.sithsorcery"))
            {
                RotStage rotStage = resurrectedPawn.Corpse.GetRotStage();
                CompRottable compRottable = resurrectedPawn.Corpse.TryGetComp<CompRottable>();
                if (compRottable != null)
                {
                    compRottable.RotProgress = float.MaxValue; // Force immediate rotting to dessicated
                    resurrectedPawn.Corpse.RotStageChanged();
                }
                var zombieDef = DefDatabase<MutantDef>.GetNamed("Force_KorribanZombie");
                resurrectedPawn.mutant = new Pawn_MutantTracker(resurrectedPawn, zombieDef, rotStage);
                Hediff_KorribanZombie obj = resurrectedPawn.health.AddHediff(zombieDef.hediff) as Hediff_KorribanZombie;
                if (obj != null)
                {
                    obj.LinkedMaster = parent.pawn;
                }

                HediffComp_DisappearsAndKills hediffComp_DisappearsAndKills = obj.TryGetComp<HediffComp_DisappearsAndKills>();
                if (hediffComp_DisappearsAndKills != null)
                {
                    if (lifespanTicks > 0)
                    {
                        hediffComp_DisappearsAndKills.disappearsAfterTicks = lifespanTicks;
                        hediffComp_DisappearsAndKills.ticksToDisappear = lifespanTicks;
                    }
                    else
                    {
                        hediffComp_DisappearsAndKills.disabled = true;
                    }
                }
                obj?.StartRising(lifespanTicks);
                if (faction == null && zombieDef.defaultFaction != null)
                {
                    faction = Find.FactionManager.FirstFactionOfDef(obj.LinkedMaster.Faction.def);
                }
                if (faction != null && resurrectedPawn.Faction != faction)
                {
                    resurrectedPawn.SetFaction(faction);
                }
            }
            else
            {
                ResurrectionUtility.TryResurrect(resurrectedPawn);
                Hediff hediff = HediffMaker.MakeHediff(ForceDefOf.Force_SithZombie, resurrectedPawn);
                resurrectedPawn.health.AddHediff(hediff);

                resurrectedPawn.SetFaction(parent.pawn.Faction);
                if (resurrectedPawn.IsPrisoner && ModsConfig.IdeologyActive)
                {
                    resurrectedPawn.guest.SetGuestStatus(parent.pawn.Faction, GuestStatus.Slave);
                }
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

