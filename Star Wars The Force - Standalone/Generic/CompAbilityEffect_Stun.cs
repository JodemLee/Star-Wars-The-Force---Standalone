using Verse;

namespace TheForce_Standalone.Generic
{
    internal class CompAbilityEffect_Stun : CompAbilityEffect_WithParentDuration
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (target.HasThing)
            {
                base.Apply(target, dest);
                if (target.Thing is Pawn pawn)
                {
                    pawn.stances.stunner.StunFor(GetDurationSeconds(pawn).SecondsToTicks(), parent.pawn, addBattleLog: false);
                }
            }
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            base.AICanTargetNow(target);
            if (target.Pawn == null || target.Pawn.ThingID == parent.pawn.ThingID)
                return false;
            return target.Pawn.stances.stunner.StunTicksLeft <= 5;

        }
    }
}
