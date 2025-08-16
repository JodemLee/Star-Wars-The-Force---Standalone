using Verse;

namespace TheForce_Standalone.HediffComps
{
    internal class HediffComp_Vanish : HediffComp
    {
        public HediffCompProperties_Vanish Props => (HediffCompProperties_Vanish)props;

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (Props.vanish)
            {
                parent.pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
                parent.pawn.Destroy();
                Find.WorldPawns.RemovePawn(parent.pawn);
            }
            else { parent.pawn.Kill(null); }
        }

        public override void Notify_PawnKilled()
        {
            base.Notify_PawnKilled();
            if (Props.vanish)
            {
                parent.pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
                parent.pawn.Destroy();
                Find.WorldPawns.RemovePawn(parent.pawn);
            }
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (Props.vanish)
            {
                parent.pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
                parent.pawn.Destroy();
                Find.WorldPawns.RemovePawn(parent.pawn);
            }
        }


    }

    public class HediffCompProperties_Vanish : HediffCompProperties
    {
        public bool vanish;
        public HediffCompProperties_Vanish()
        {
            compClass = typeof(HediffComp_Vanish);
        }
    }
}
