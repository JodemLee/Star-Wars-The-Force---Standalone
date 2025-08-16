using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TheForce_Standalone.Generic
{
    internal class CompAbilityEffect_OnlyTargetAllies : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (parent.pawn != null)
            {
                return !parent.pawn.HostileTo(target.Thing);
            }
            return false;
        }

        public override bool Valid(GlobalTargetInfo target, bool throwMessages = false)
        {
            if (parent.pawn != null)
            {
                return !parent.pawn.HostileTo(target.Thing);
            }
            return false;
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            base.AICanTargetNow(target);
            if (parent.pawn != null)
            {
                return !parent.pawn.HostileTo(target.Thing);
            }
            return false;
        }
    }
}

