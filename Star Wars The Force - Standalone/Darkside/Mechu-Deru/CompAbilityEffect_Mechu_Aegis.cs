using System.Collections.Generic;
using Verse;

namespace TheForce_Standalone.Darkside.Mechu_Deru
{
    internal class CompAbilityEffect_Mechu_Aegis : CompAbilityEffect_GiveHediff
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (parent.pawn == null || !MechanitorUtility.ShouldBeMechanitor(parent.pawn))
            {
                Log.Warning("Caster is not a valid mechanitor.");
                return;
            }
            List<Pawn> controlledMechs = parent.pawn.mechanitor.ControlledPawns;
            if (controlledMechs == null || controlledMechs.Count == 0)
            {
                Log.Warning("No mechanoids are controlled by the caster.");
                return;
            }

            foreach (Pawn mechanoid in controlledMechs)
            {
                if (mechanoid == null || mechanoid.health == null || mechanoid.health.hediffSet == null)
                {
                    continue;
                }

                if (mechanoid.health.hediffSet.HasHediff(Props.hediffDef))
                {
                    continue;
                }

                ApplyInner(mechanoid, parent.pawn);
            }
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (parent.pawn == null || !MechanitorUtility.ShouldBeMechanitor(parent.pawn) ||
                parent.pawn.mechanitor == null || parent.pawn.mechanitor.ControlledPawns.Count == 0)
            {
                return false;
            }

            foreach (Pawn mechanoid in parent.pawn.mechanitor.ControlledPawns)
            {
                if (mechanoid != null && mechanoid.health != null &&
                    !mechanoid.health.hediffSet.HasHediff(Props.hediffDef))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
