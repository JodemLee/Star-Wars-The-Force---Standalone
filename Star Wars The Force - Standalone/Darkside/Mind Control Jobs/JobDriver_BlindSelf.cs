using RimWorld;
using Verse;

namespace TheForce_Standalone.Darkside.Mind_Control_Jobs
{
    public class JobDriver_BlindSelf : JobDriver_RemoveBodyPart
    {
        protected override BodyPartDef TargetBodyPart => BodyPartDefOf.Eye;
        protected override DamageDef DamageType => DamageDefOf.SurgicalCut;
        protected override HistoryEventDef HistoryEvent => HistoryEventDefOf.GotBlinded;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
    }
}
