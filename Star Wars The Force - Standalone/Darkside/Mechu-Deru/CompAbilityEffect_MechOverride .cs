using RimWorld;
using Verse;

namespace TheForce_Standalone.Darkside.Mechu_Deru
{
    internal class CompAbilityEffect_MechOverride : CompAbilityEffect_GiveHediff
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (target.Thing is Pawn mechanoid && mechanoid.RaceProps.IsMechanoid)
            {
                AssignMechToCaster(mechanoid, parent.pawn);
            }
        }

        private void AssignMechToCaster(Pawn mech, Pawn caster)
        {
            if (caster != null && MechanitorUtility.IsMechanitor(caster))
            {
                mech.SetFaction(Faction.OfPlayer);
                caster.relations.AddDirectRelation(PawnRelationDefOf.Overseer, mech);
                Messages.Message(
                    "MessageMechanitorAssignedToMech".Translate(caster.LabelShort, mech.LabelShort),
                    new LookTargets(new[] { caster, mech }),
                    MessageTypeDefOf.PositiveEvent
                );
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            return target.Thing is Pawn pawn &&
                   pawn.RaceProps.IsMechanoid &&
                   base.Valid(target, throwMessages);
        }

        protected override bool TryResist(Pawn pawn)
        {
            return false;
        }
    }
}
