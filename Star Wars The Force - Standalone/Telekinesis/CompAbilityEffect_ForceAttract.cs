using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace TheForce_Standalone.Telekinesis
{
    internal class CompAbilityEffect_ForceAttract : CompAbilityEffect
    {
        private float maxPullDistance = 10f;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (!target.IsValid || parent.pawn.Map == null) return;

            var things = ThingsInRange();
            foreach (Thing thing in things)
            {
                if (thing is Pawn targetPawn && thing != parent.pawn)
                {
                    IntVec3 pullPosition = TelekinesisUtility.CalculatePullPosition(
                        parent.pawn.Position,
                        targetPawn.Position,
                        maxPullDistance,
                        parent.pawn.Map
                    );

                    TelekinesisUtility.LaunchPawn(
                        targetPawn,
                        pullPosition,
                        parent,
                        ForceDefOf.Force_ThrownPawnAttract,
                        parent.pawn.Map,
                        false,
                        null
                    );
                }
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return base.CanApplyOn(target, dest) && Valid(target);
        }

        private List<Thing> ThingsInRange()
        {
            try
            {
                IEnumerable<Thing> thingsInRange = GenRadial.RadialDistinctThingsAround(
                    parent.pawn.Position,
                    parent.pawn.Map,
                    parent.def.EffectRadius,
                    useCenter: true);
                var result = new List<Thing>(thingsInRange);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"[ForceRepulsion] Error in ThingsInRange: {ex}");
                return new List<Thing>();
            }
        }

        public override bool Valid(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.Valid(target, showMessages))
            {
                return false;
            }

            if (target.Thing is not Pawn)
            {
                return false;
            }

            return true;
        }
    }
}
