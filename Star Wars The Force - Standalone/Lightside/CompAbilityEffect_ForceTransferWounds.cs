using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Lightside
{
    internal class CompAbilityEffect_ForceTransferWounds : CompAbilityEffect_ForcePower
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (!(target.Pawn is Pawn targetPawn) || !(parent.pawn is Pawn caster))
            {
                Log.Error("Invalid targets for Force_TransferWounds.");
                return;
            }

            TransferWounds(targetPawn, caster);
        }

        private void TransferWounds(Pawn source, Pawn dest)
        {
            List<Hediff_Injury> sourceInjuries = GetWoundsToTransfer(source);
            foreach (var injury in sourceInjuries)
            {
                source.health.RemoveHediff(injury);
            }

            AddWounds(dest, sourceInjuries);
        }

        private List<Hediff_Injury> GetWoundsToTransfer(Pawn pawn)
        {
            return pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(hediff => hediff.CanHealNaturally() && hediff.Visible && !hediff.IsPermanent())
                .ToList();
        }

        private void AddWounds(Pawn pawn, List<Hediff_Injury> injuries)
        {
            foreach (var injury in injuries)
            {
                try
                {
                    
                    Hediff_Injury newInjury = (Hediff_Injury)HediffMaker.MakeHediff(injury.def, pawn);
                    newInjury.Severity = injury.Severity;
                    newInjury.Part = injury.Part;
                    pawn.health.AddHediff(newInjury);
                }
                catch (Exception e)
                {
                    Log.Error($"Error while adding hediff to {pawn.Label}: {e}");
                }
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
            {
                return false;
            }

            Pawn targetPawn = target.Pawn;
            if (targetPawn == null)
            {
                return false;
            }

           
            if (!GetWoundsToTransfer(targetPawn).Any())
            {
                if (throwMessages)
                {
                    Messages.Message("Force.Heal_NoWoundsToTransfer".Translate(),
                 targetPawn,
                 MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            return true;
        }
    }
}
