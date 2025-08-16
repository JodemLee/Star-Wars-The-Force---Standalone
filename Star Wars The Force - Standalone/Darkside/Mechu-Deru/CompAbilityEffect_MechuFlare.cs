using RimWorld;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Darkside.Mechu_Deru
{
    internal class CompAbilityEffect_MechuFlare : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (target.Pawn is Pawn targetPawn)
            {
                WeakenImplants(targetPawn);
            }
        }

        private void WeakenImplants(Pawn targetPawn)
        {
            var implantHediffs = targetPawn.health.hediffSet.hediffs
                .Where(hediff => IsImplantOrProsthetic(hediff))
                .ToList();

            foreach (var implantHediff in implantHediffs)
            {
                var bodyPart = implantHediff.Part;
                if (bodyPart != null)
                {
                    targetPawn.health.AddHediff(ForceDefOf.Force_ImplantFlare, bodyPart);
                }
            }
        }

        private bool IsImplantOrProsthetic(Hediff hediff)
        {
            return hediff.def?.countsAsAddedPartOrImplant ?? false;
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
                return false;

            if (target.Pawn == null)
            {
                if (throwMessages)
                    Messages.Message("MustTargetPawn".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }
    }
}
