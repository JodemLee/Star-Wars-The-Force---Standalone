using RimWorld;
using Verse;

namespace TheForce_Standalone.Darkside.Mind_Control_Jobs
{
    internal class JobDriver_RemoveHeart : JobDriver_RemoveBodyPart
    {
        protected override BodyPartDef TargetBodyPart => BodyPartDefOf.Heart;
        protected override DamageDef DamageType => DamageDefOf.SurgicalCut;
        protected override int InitialWaitTicks => 100;
        protected override int FinalWaitTicks => 200;

    }
}
