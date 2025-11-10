using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TheForce_Standalone.Darkside.Mind_Control_Jobs
{
    public class JobDriver_SelfIgnite : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_General.WaitWith(TargetIndex.A, 30, true, true);
            Toil igniteSelf = ToilMaker.MakeToil("SelfIgnite");
            igniteSelf.initAction = delegate
            {
                if (pawn.Spawned && !pawn.Dead)
                {

                    FleckMaker.ThrowSmoke(pawn.DrawPos, pawn.Map, 2f);
                    FleckMaker.ThrowFireGlow(pawn.DrawPos, pawn.Map, 1.5f);

                    pawn.TryAttachFire(Rand.Range(0.5f, 1.2f), pawn);
                }
            };
            igniteSelf.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return igniteSelf;
            yield return Toils_General.Wait(60);
        }
    }
}
