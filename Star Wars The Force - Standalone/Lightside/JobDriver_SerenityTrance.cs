using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace TheForce_Standalone.Lightside
{
    internal class JobDriver_SerenityTrance : JobDriver
    {
        public override string GetReport()
        {
            var forceUser = pawn.GetComp<CompClass_ForceUser>();
            if (forceUser != null && forceUser.IsValidForceUser)
            {
                return "SerenityTrance".Translate();
            }
            return "Meditating".Translate();
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil meditate = ToilMaker.MakeToil("MakeNewToils");
            meditate.socialMode = RandomSocialMode.Off;
            meditate.initAction = delegate
            {
                pawn.rotationTracker.FaceCell(pawn.Position + Rot4.South.FacingCell);
            };
            meditate.defaultCompleteMode = ToilCompleteMode.Delay;
            meditate.defaultDuration = job.def.joyDuration;
            meditate.FailOn(() => !MeditationUtility.CanMeditateNow(pawn));
            meditate.AddPreTickAction(delegate
            {
                var forceUser = pawn.GetComp<CompClass_ForceUser>();

                // Check if pawn is a valid Force user
                if (forceUser == null || !forceUser.IsValidForceUser)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // Auto-end if FP is full
                if (forceUser.currentFP >= forceUser.MaxFP * 0.99f)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                MeditationTick();
            });

            yield return meditate;
        }

        protected void MeditationTick()
        {
            pawn.skills.Learn(SkillDefOf.Intellectual, 0.009f);
            pawn.GainComfortFromCellIfPossible(1);
            if (pawn.needs.joy != null)
            {
                JoyUtility.JoyTickCheckEnd(pawn, 1, JoyTickFullJoyAction.None);
            }
            var forceUser = pawn.GetComp<CompClass_ForceUser>();
            if (forceUser != null && forceUser.IsValidForceUser)
            {
                // Recover FP through serenity trance
                if (forceUser.currentFP < forceUser.MaxFP)
                {
                    forceUser.RecoverFP(forceUser.Pawn.GetStatValue(StatDef.Named("Force_FPRecovery")) * 1.5f);
                }
            }
            pawn.rotationTracker.FaceCell(pawn.Position + Rot4.South.FacingCell);
        }
    }
}
