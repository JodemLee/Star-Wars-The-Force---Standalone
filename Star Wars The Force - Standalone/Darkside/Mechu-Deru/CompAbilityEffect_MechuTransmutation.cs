using RimWorld;
using Verse;

namespace TheForce_Standalone.Darkside.Mechu_Deru
{
    internal class CompAbilityEffect_MechuTransmutation : CompAbilityEffect
    {
        private static readonly PawnKindDef SpecificMechanoidKind = ForceDefOf.Force_Mech_Inquisitor;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn targetPawn = target.Pawn;
            if (targetPawn == null || targetPawn.Dead)
            {
                Log.Error("Force.MechuTrans_InvalidTarget".Translate());
                return;
            }

            targetPawn.Kill(null, null);
            targetPawn.Corpse?.Destroy();

            // Spawn mechanoid
            Pawn mechanoid = SpawnSpecificMechanoid(targetPawn.Position, parent.pawn.Map, targetPawn.Name);
            if (mechanoid == null)
            {
                Log.Error("Force.MechuTrans_FailedSpawn".Translate());
                return;
            }

            FleckMaker.ThrowSmoke(targetPawn.DrawPos, parent.pawn.Map, 2f);
            FleckMaker.ThrowLightningGlow(targetPawn.DrawPos, parent.pawn.Map, 2f);
            AssignMechToCaster(mechanoid, parent.pawn);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
                return false;

            // Check target is living pawn
            if (target.Pawn == null || target.Pawn.Dead)
            {
                if (throwMessages)
                    Messages.Message("Force.MechuTrans_TargetLivingPawn".Translate(),
                                    parent.pawn,
                                    MessageTypeDefOf.RejectInput);
                return false;
            }

            // Check mechanitor bandwidth
            if (parent.pawn.mechanitor == null)
            {
                if (throwMessages)
                    Messages.Message("Force.MechuTrans_RequiresMechanitor".Translate(),
                                    parent.pawn,
                                    MessageTypeDefOf.RejectInput);
                return false;
            }

            float mechanoidBandwidthCost = SpecificMechanoidKind.race.GetStatValueAbstract(StatDefOf.BandwidthCost);
            float availableBandwidth = parent.pawn.mechanitor.TotalBandwidth - parent.pawn.mechanitor.UsedBandwidth;

            if (availableBandwidth < mechanoidBandwidthCost)
            {
                if (throwMessages)
                {
                    Messages.Message(
                        "Force.MechuTrans_InsufficientBandwidth".Translate(
                            SpecificMechanoidKind.LabelCap,
                            mechanoidBandwidthCost,
                            availableBandwidth
                        ),
                        parent.pawn,
                        MessageTypeDefOf.RejectInput
                    );
                }
                return false;
            }

            return true;
        }

        public static void AssignMechToCaster(Pawn mech, Pawn caster)
        {
            if (caster != null && MechanitorUtility.IsMechanitor(caster))
            {
                caster.relations.AddDirectRelation(PawnRelationDefOf.Overseer, mech);
                Messages.Message(
                    "Force.MechuTrans_MechanoidAssigned".Translate(caster.LabelShort, mech.LabelShort),
                    new LookTargets(new[] { caster, mech }),
                    MessageTypeDefOf.PositiveEvent
                );
            }
        }

        private Pawn SpawnSpecificMechanoid(IntVec3 position, Map map, Name targetName)
        {
            Pawn mechanoid = PawnGenerator.GeneratePawn(SpecificMechanoidKind, Faction.OfPlayer);

            if (targetName != null)
            {
                mechanoid.Name = targetName;
            }

            GenSpawn.Spawn(mechanoid, position, map, WipeMode.Vanish);
            return mechanoid;
        }
    }
}