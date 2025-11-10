using RimWorld;
using Verse;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    public class CompAbilityEffect_DrainForce : CompAbilityEffect
    {
        public new CompProperties_DrainForce Props => (CompProperties_DrainForce)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (!target.HasThing || !(target.Thing is Corpse corpse))
                return;

            Pawn innerPawn = corpse.InnerPawn;
            float drainAmount = GetDrainAmount(innerPawn);

            ApplyForceDrainHediff(drainAmount);
            Messages.Message("MessageForceDrained".Translate(innerPawn, drainAmount.ToString("F1")), parent.pawn, MessageTypeDefOf.PositiveEvent);

            corpse.Destroy();
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!target.HasThing || !(target.Thing is Corpse corpse))
            {
                if (throwMessages)
                    Messages.Message("MessageMustTargetCorpse".Translate(), parent.pawn, MessageTypeDefOf.RejectInput);
                return false;
            }

            if (corpse.GetRotStage() == RotStage.Dessicated)
            {
                if (throwMessages)
                    Messages.Message("MessageCannotDrainDessicatedCorpse".Translate(), corpse, MessageTypeDefOf.RejectInput);
                return false;
            }

            return base.Valid(target, throwMessages);
        }

        private float GetDrainAmount(Pawn pawn)
        {
            // Prioritize force level
            if (ForceSensitivityUtils.IsValidForceUser(pawn))
            {
                return ForceSensitivityUtils.GetForceLevel(pawn);
            }

            // Fall back to psychic sensitivity
            return pawn.GetStatValue(StatDefOf.PsychicSensitivity);
        }

        private void ApplyForceDrainHediff(float drainAmount)
        {
            HediffDef forceDrainDef = HediffDef.Named("Force_Drain");
            Hediff existingHediff = parent.pawn.health.hediffSet.GetFirstHediffOfDef(forceDrainDef);

            if (existingHediff != null)
            {
                existingHediff.Severity += drainAmount * Props.severityPerLevel;
            }
            else
            {
                Hediff newHediff = HediffMaker.MakeHediff(forceDrainDef, parent.pawn);
                newHediff.Severity = drainAmount * Props.severityPerLevel;
                parent.pawn.health.AddHediff(newHediff);
            }
        }
    }

    public class CompProperties_DrainForce : CompProperties_AbilityEffect
    {
        public float severityPerLevel = 1.0f;

        public CompProperties_DrainForce()
        {
            compClass = typeof(CompAbilityEffect_DrainForce);
        }
    }
}