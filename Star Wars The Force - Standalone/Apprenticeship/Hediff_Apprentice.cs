using RimWorld;
using TheForce_Standalone.Dialogs;
using Verse;

namespace TheForce_Standalone.Apprenticeship
{
    public class Hediff_Apprentice : HediffWithComps
    {
        public Pawn master;
        public int ticksSinceLastXPGain;
        private int xpGainInterval;
        private const int ApplyBondCooldown = 60000; // 1 in-game day
        public int ticksSinceLastBondAttempt;

        public override string Label => "Force.Apprentice_Label".Translate(base.Label, master?.LabelShort ?? "Force.Apprentice_UnknownMaster".Translate());

        public Hediff_Apprentice()
        {
            xpGainInterval = Rand.Range(GenDate.TicksPerDay * 2, GenDate.TicksPerDay * 5);
        }

        public override void Tick()
        {
            base.Tick();
            ticksSinceLastXPGain++;
            ticksSinceLastBondAttempt++;

            if (ticksSinceLastXPGain >= xpGainInterval)
            {
                GainExperience();
                ticksSinceLastXPGain = 0;
            }

            if (ticksSinceLastBondAttempt >= ApplyBondCooldown)
            {
                TryApplyForceBond();
                ticksSinceLastBondAttempt = 0;
            }
        }

        private void GainExperience()
        {
            if (master == null || pawn == null) return;
            var masterHediff = master.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Master) as Hediff_Master;

            int levelDifference = master.GetComp<CompClass_ForceUser>().forceLevel - pawn.GetComp<CompClass_ForceUser>().forceLevel;
            if (levelDifference > 0)
            {
                pawn.GetComp<CompClass_ForceUser>().Leveling.AddForceExperience(levelDifference * 10);
                Messages.Message("Force.Apprentice_XPGain".Translate(pawn.LabelShort, levelDifference * 10),
                              pawn,
                              MessageTypeDefOf.PositiveEvent);
            }

            if (pawn.GetComp<CompClass_ForceUser>().forceLevel >= master.GetComp<CompClass_ForceUser>().forceLevel)
            {
                Messages.Message("Force.Apprentice_GraduationReady".Translate(pawn.LabelShort),
                              pawn,
                              MessageTypeDefOf.PositiveEvent);
                EndApprenticeship();
            }
        }

        private void TryApplyForceBond()
        {
            if (Rand.Chance(0.1f) && master != null && pawn != null)
            {
                if (!master.health.hediffSet.HasHediff(ForceDefOf.ForceBond_MasterApprentice))
                {
                    Hediff hediff = HediffMaker.MakeHediff(ForceDefOf.ForceBond_MasterApprentice, master);
                    master.health.AddHediff(hediff);
                    pawn.health.AddHediff(hediff);
                    Messages.Message("Force.Apprentice_BondFormed".Translate(master.LabelShort, pawn.LabelShort),
                                  pawn,
                                  MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        private void EndApprenticeship()
        {
            var masterHediff = master.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Master) as Hediff_Master;
            pawn?.health?.RemoveHediff(this);
            masterHediff.apprentices.Remove(pawn);
            masterHediff.graduatedApprenticesCount++;
            masterHediff.CheckAndPromoteMasterBackstory();
            pawn.relations.RemoveDirectRelation(ForceDefOf.Force_MasterRelation, master);
            master.relations.RemoveDirectRelation(ForceDefOf.Force_ApprenticeRelation, pawn);

            Messages.Message("Force.Apprentice_Graduated".Translate(pawn.LabelShort, master.LabelShort),
                          pawn,
                          MessageTypeDefOf.PositiveEvent);

            Find.WindowStack.Add(new Dialog_SelectBackstory(pawn));
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            if (master != null)
            {
                var masterHediff = master.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Master) as Hediff_Master;
                masterHediff?.apprentices.Remove(pawn);
                Messages.Message("Force.Apprentice_Removed".Translate(pawn.LabelShort),
                              pawn,
                              MessageTypeDefOf.NeutralEvent);
            }
        }

        public override void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
        {
            base.Notify_KilledPawn(victim, dinfo);
            if (victim == master && pawn.GetStatValue(ForceDefOf.Force_Darkside_Attunement) > pawn.GetStatValue(ForceDefOf.Force_Lightside_Attunement))
            {
                Messages.Message("Force.Apprentice_MasterKilled".Translate(pawn.LabelShort, master.LabelShort),
                              pawn,
                              MessageTypeDefOf.NegativeEvent);
                EndApprenticeship();
                Find.WindowStack.Add(new Dialog_SelectBackstory(pawn));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref master, "master");
            Scribe_Values.Look(ref xpGainInterval, "xpGainInterval", 180000);
            Scribe_Values.Look(ref ticksSinceLastXPGain, "ticksSinceLastXPGain", 0);
            Scribe_Values.Look(ref ticksSinceLastBondAttempt, "ticksSinceLastBondAttempt", 0);
        }
    }
}