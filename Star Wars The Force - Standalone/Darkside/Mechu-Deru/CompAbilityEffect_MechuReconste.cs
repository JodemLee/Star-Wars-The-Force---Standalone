using RimWorld;
using System.Collections.Generic;
using Verse;

namespace TheForce_Standalone.Darkside.Mechu_Deru
{
    internal class CompAbilityEffect_MechuReconste : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            var targetBuilding = target.Thing as Building;
            if (targetBuilding == null || !targetBuilding.def.building.isPowerConduit)
            {
                Log.Error("Force.Mechu_InvalidTarget".Translate());
                return;
            }

            CompPower powerComp = targetBuilding.GetComp<CompPower>();
            if (powerComp == null) return;

            HashSet<Building> connectedBuildings = PowerNetUtility.GetConnectedBuildings(powerComp);
            foreach (var building in connectedBuildings)
            {
                if (building.HitPoints < building.MaxHitPoints)
                {
                    building.HitPoints = building.MaxHitPoints;
                    MoteMaker.MakeStaticMote(building.Position.ToVector3().ToIntVec3(), parent.pawn.Map, ThingDefOf.Mote_MechCharging, 3f);
                }
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
                return false;

            var targetBuilding = target.Thing as Building;
            if (targetBuilding == null || !targetBuilding.def.building.isPowerConduit)
            {
                if (throwMessages)
                {
                    Messages.Message("Force.Mechu_TargetMustBeConduit".Translate(),
                                  parent.pawn,
                                  MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            return true;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            if (target.Thing is Building building && building.def.building.isPowerConduit)
            {
                return "Force.Mechu_RepairConnected".Translate();
            }
            return null;
        }
    }
}