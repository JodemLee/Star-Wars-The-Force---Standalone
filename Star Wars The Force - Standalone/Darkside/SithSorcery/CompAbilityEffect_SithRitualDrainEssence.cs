using RimWorld;
using TheForce_Standalone.HediffComps;
using Verse;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    internal class CompAbilityEffect_SithRitualDrainEssence : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (target.Thing is Pawn enemy)
            {
                // Apply the hediff to the enemy
                Hediff hediff = enemy.health.AddHediff(ForceDefOf.Force_SithRitualDrainEssence);
                var comp = hediff.TryGetComp<HediffComp_SithRitualDrainEssence>();
                comp?.Initialize(parent.pawn);
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Pawn == null)
            {
                if (throwMessages)
                {
                    Messages.Message("AbilityMustTargetPawn".Translate(), MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            return base.Valid(target, throwMessages);
        }
    }
}