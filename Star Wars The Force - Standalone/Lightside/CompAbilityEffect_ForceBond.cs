using RimWorld;
using System.Collections.Generic;
using Verse;

namespace TheForce_Standalone.Lightside
{
    internal class CompAbilityEffect_ForceBond : CompAbilityEffect
    {
        public new CompProperties_ForceBond Props => (CompProperties_ForceBond)props;

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            base.AICanTargetNow(target);
            if (target.Pawn == null) return false;
            return !target.Pawn.RaceProps.Humanlike;
        }


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

            if (pawn.MentalState != null)
            {
                pawn.MentalState.RecoverFromState();
            }
            else
            {
                ShowForceBondOptions(pawn);
            }
        }

        private void ShowForceBondOptions(Pawn targetPawn)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            options.Add(new FloatMenuOption("Force.Bond_ManhunterOption".Translate(), () =>
            {
                targetPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, forced: true);
                Messages.Message("Force.Bond_ManhunterSuccess".Translate(parent.pawn.LabelShort, targetPawn.LabelShort),
                                targetPawn, MessageTypeDefOf.NegativeEvent);
            }));

            options.Add(new FloatMenuOption("Force.Bond_CreateOption".Translate(), () =>
            {
                if (parent.pawn.relations.DirectRelationExists(PawnRelationDefOf.Bond, targetPawn))
                {
                    Messages.Message("Force.Bond_AlreadyExists".Translate(),
                                  targetPawn, MessageTypeDefOf.RejectInput);
                    return;
                }

                parent.pawn.relations.AddDirectRelation(PawnRelationDefOf.Bond, targetPawn);
                targetPawn.relations.AddDirectRelation(PawnRelationDefOf.Bond, parent.pawn);
                targetPawn.SetFaction(Faction.OfPlayer);
                Messages.Message("Force.Bond_CreatedSuccess".Translate(parent.pawn.LabelShort, targetPawn.LabelShort),
                                targetPawn, MessageTypeDefOf.PositiveEvent);
            }));

            Find.WindowStack.Add(new FloatMenu(options));
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return Valid(target);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = target.Pawn;
            if (pawn != null && !pawn.RaceProps.intelligence.HasFlag(Intelligence.Animal))
            {
                return false;
            }
            return true;
        }

        public MentalBreakIntensity TargetMentalBreakIntensity(LocalTargetInfo target)
        {
            Pawn pawn = target.Pawn;
            if (pawn != null)
            {
                MentalStateDef mentalStateDef = pawn.MentalStateDef;
                if (mentalStateDef != null)
                {
                    List<MentalBreakDef> allDefsListForReading = DefDatabase<MentalBreakDef>.AllDefsListForReading;
                    for (int i = 0; i < allDefsListForReading.Count; i++)
                    {
                        if (allDefsListForReading[i].mentalState == mentalStateDef)
                        {
                            return allDefsListForReading[i].intensity;
                        }
                    }
                }
                else if (pawn.health.hediffSet.HasHediff(HediffDefOf.CatatonicBreakdown))
                {
                    return MentalBreakIntensity.Extreme;
                }
            }
            return MentalBreakIntensity.Minor;
        }
    }

    public class CompProperties_ForceBond : CompProperties_AbilityEffect
    {
        public List<MentalStateDef> exceptions;

        public CompProperties_ForceBond()
        {
            compClass = typeof(CompAbilityEffect_ForceBond);
        }
    }
}