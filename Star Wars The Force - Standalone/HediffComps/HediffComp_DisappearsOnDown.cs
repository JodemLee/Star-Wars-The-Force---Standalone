using Verse;

namespace TheForce_Standalone.HediffComps
{
    internal class HediffComp_DisappearsOnDown : HediffComp
    {
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            base.Pawn.health.RemoveHediff(parent);
        }
        public override bool CompShouldRemove => base.Pawn.DeadOrDowned;
    }

    public class HediffCompProperties_DisappearsOnDownedOrDeath : HediffCompProperties
    {
        public HediffCompProperties_DisappearsOnDownedOrDeath()
        {
            compClass = typeof(HediffComp_DisappearsOnDown);
        }
    }
}
