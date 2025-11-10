using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TheForce_Standalone.Alignment
{
    public class JobDriver_TransformCrystal : JobDriver
    {
        private Thing TargetCrystal => job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetCrystal, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.WaitWith(TargetIndex.A, 60, true);

            yield return new Toil
            {
                initAction = () => ExecuteTransformation(),
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private void ExecuteTransformation()
        {
            if (TargetCrystal == null || TargetCrystal.Destroyed)
                return;

            var forceUser = pawn.GetComp<CompClass_ForceUser>();
            if (forceUser != null)
            {
                // Determine transformation type based on the target crystal
                var transformationType = forceUser.Alignment.CrystalTransformation.GetTransformationTypeForCrystal(TargetCrystal.def);

                switch (transformationType)
                {
                    case TransformationType.DarkSide:
                        forceUser.Alignment.CrystalTransformation.DoDarkSideTransformation(TargetCrystal);
                        break;
                    case TransformationType.LightSide:
                        forceUser.Alignment.CrystalTransformation.DoLightSideTransformation(TargetCrystal);
                        break;
                }
            }
        }
    }
}