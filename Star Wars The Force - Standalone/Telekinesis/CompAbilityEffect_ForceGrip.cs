using RimWorld;
using TheForce_Standalone.HediffComps;
using Verse;

namespace TheForce_Standalone.Telekinesis
{
    internal class CompAbilityEffect_ForceGrip : CompAbilityEffect
    {
        public new CompProperties_AbilityForceGrip Props => (CompProperties_AbilityForceGrip)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (target.Thing == null || !target.Thing.Spawned) return;

            var hediff = parent.pawn.health.hediffSet.GetFirstHediffOfDef(Props.holdingHediff);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(Props.holdingHediff, parent.pawn);
                parent.pawn.health.AddHediff(hediff);
            }

            if (hediff.TryGetComp<HediffComp_ThingHolder>() is HediffComp_ThingHolder holder)
            {
                if (holder.HeldThing != null)
                {
                    holder.TryReleaseThing(out _);
                }
                target.Thing.DeSpawn(DestroyMode.Vanish);
                holder.TryStoreThing(target.Thing);

            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            return base.Valid(target, throwMessages);
        }
    }

    internal class CompProperties_AbilityForceGrip : CompProperties_AbilityEffect
    {
        public HediffDef holdingHediff;
        public CompProperties_AbilityForceGrip()
        {
            compClass = typeof(CompAbilityEffect_ForceGrip);
        }
    }
}
