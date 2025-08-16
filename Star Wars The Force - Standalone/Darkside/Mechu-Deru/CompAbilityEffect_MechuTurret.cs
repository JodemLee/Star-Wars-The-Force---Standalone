using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Darkside.Mechu_Deru
{
    internal class CompAbilityEffect_MechuTurret : CompAbilityEffect
    {
        private static readonly ThingDef ShieldDef = ThingDef.Named("Force_NanoShieldMechuDeru");
        private const float ShieldEnergyCost = 150f;
        private const float ShieldUpkeepCost = 0.15f;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (target.Thing == null || parent.pawn == null || parent.pawn.Map == null)
                return;

            var mechuDeruComp = GetMechuDeruComp();
            if (!ValidateMechuDeru(mechuDeruComp))
                return;

            if (!(target.Thing is Building targetBuilding) || !ValidatePowerConduit(targetBuilding))
                return;

            var powerComp = targetBuilding.GetComp<CompPower>();
            if (powerComp == null)
                return;

            foreach (var building in PowerNetUtility.GetConnectedBuildings(powerComp))
            {
                if (building.def.building.IsTurret)
                {
                    SpawnShieldAround(building, mechuDeruComp);
                    mechuDeruComp.StoredEnergy -= ShieldEnergyCost;
                    Messages.Message("Force.MechuTurret_ShieldSpawned".Translate(building.LabelCap),
                                  parent.pawn,
                                  MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.pawn == null) return;

            var mechuDeruComp = GetMechuDeruComp();
            if (mechuDeruComp == null) return;

            foreach (var shield in mechuDeruComp.spawnedShields.ToList())
            {
                mechuDeruComp.StoredEnergy -= ShieldUpkeepCost;
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
                return false;

            var mechuDeruComp = GetMechuDeruComp();
            if (!ValidateMechuDeru(mechuDeruComp, throwMessages))
                return false;

            if (!(target.Thing is Building targetBuilding) || !ValidatePowerConduit(targetBuilding, throwMessages))
                return false;

            if (mechuDeruComp.StoredEnergy < ShieldEnergyCost)
            {
                if (throwMessages)
                    Messages.Message("Force.MechuTurret_InsufficientEnergy".Translate(),
                                  parent.pawn,
                                  MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }

        private HediffComp_MechuDeru GetMechuDeruComp()
        {
            return parent.pawn.health.hediffSet
                .GetFirstHediffOfDef(ForceDefOf.Force_MechuLinkImplant)?
                .TryGetComp<HediffComp_MechuDeru>();
        }

        private bool ValidateMechuDeru(HediffComp_MechuDeru comp, bool showMessages = false)
        {
            if (comp != null) return true;

            if (showMessages)
                Messages.Message("Force.MechuTurret_MissingImplant".Translate(),
                              parent.pawn,
                              MessageTypeDefOf.RejectInput);
            return false;
        }

        private bool ValidatePowerConduit(Building building, bool showMessages = false)
        {
            if (building.def.building?.isPowerConduit == true) return true;

            if (showMessages)
                Messages.Message("Force.MechuTurret_InvalidTarget".Translate(),
                              parent.pawn,
                              MessageTypeDefOf.RejectInput);
            return false;
        }

        private void SpawnShieldAround(Building turret, HediffComp_MechuDeru mechuDeruComp)
        {
            if (ShieldDef == null)
            {
                Log.Error("Force.MechuTurret_ShieldDefMissing".Translate());
                return;
            }

            if (!turret.Position.InBounds(parent.pawn.Map))
                return;

            Thing shield = GenSpawn.Spawn(ShieldDef, turret.Position, turret.Map);
            MoteMaker.ThrowText(turret.Position.ToVector3(),
                              turret.Map,
                              "Force.MechuTurret_ShieldMote".Translate(),
                              Color.white,
                              3f);
            mechuDeruComp?.RegisterShield(shield);
        }
    }
}