using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TheForce_Standalone.Generic
{
    internal class CompAbilityEffect_OnlyTargetSelf : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target == parent.pawn)
            {
                return true;
            }
            return false;
        }

        public override bool Valid(GlobalTargetInfo target, bool throwMessages = false)
        {
            if (target == parent.pawn)
            {
                return true;
            }
            return false;
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            base.AICanTargetNow(target);
            if (target == parent.pawn)
            {
                return true;
            }
            return false;
        }
    }
}
