using RimWorld;
using Verse;

namespace TheForce_Standalone.Darkside.Mechu_Deru
{
    public class HediffComp_MechOverride : HediffComp
    {
        private Faction originalFaction;
        public HediffCompProperties_MechOverride Props => (HediffCompProperties_MechOverride)props;

        public override void CompPostMake()
        {
            base.CompPostMake();
            Pawn pawn = this.Pawn;
            if (pawn != null && pawn.RaceProps.IsMechanoid)
            {
                originalFaction = pawn.Faction;
                pawn.SetFaction(Faction.OfPlayer);
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            RevertFaction();
        }

        private void RevertFaction()
        {
            if (Pawn != null && !Pawn.Dead && Pawn.Faction == Faction.OfPlayer)
            {
                Pawn.SetFaction(originalFaction);
            }
        }
    }

    public class HediffCompProperties_MechOverride : HediffCompProperties
    {
        public HediffCompProperties_MechOverride()
        {
            this.compClass = typeof(HediffComp_MechOverride);
        }
    }
}
