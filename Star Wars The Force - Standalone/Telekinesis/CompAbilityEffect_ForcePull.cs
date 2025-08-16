using RimWorld;
using Verse;
using Verse.Sound;

namespace TheForce_Standalone.Telekinesis
{
    internal class CompAbilityEffect_ForcePull : CompAbilityEffect
    {
        private float maxPullDistance = 10f;
        Force_ModSettings modSettings = new Force_ModSettings();

        public CompAbilityEffect_ForcePull()
        {
            modSettings = new Force_ModSettings();
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (!target.IsValid || parent.pawn?.Map == null) return;

            IntVec3 pullPos = TelekinesisUtility.CalculatePullPosition(
                parent.pawn.Position,
                target.Cell,
                maxPullDistance,
                parent.pawn.Map
            );

            if (target.Thing is Pawn pawnTarget)
            {
                TelekinesisUtility.LaunchPawn(
                    pawnTarget,
                    pullPos,
                    parent,
                    ForceDefOf.Force_ThrownPawnPull,
                    parent.pawn.Map
                );
            }
            else if (target.Thing != null && target.Thing.def.equipmentType == EquipmentType.Primary && parent.pawn.equipment.Primary == null)
            {
                Equip(parent.pawn, target.Thing as ThingWithComps);
            }
        }

        private void Equip(Pawn equipper, ThingWithComps thingWithComps)
        {
            ThingWithComps thingWithComps2;
            if (thingWithComps.def.stackLimit > 1 && thingWithComps.stackCount > 1)
            {
                thingWithComps2 = (ThingWithComps)thingWithComps.SplitOff(1);
            }
            else
            {
                thingWithComps2 = thingWithComps;
                thingWithComps2.DeSpawn();
            }
            equipper.equipment.MakeRoomFor(thingWithComps2);
            equipper.equipment.AddEquipment(thingWithComps2);
            if (thingWithComps.def.soundInteract != null)
            {
                thingWithComps.def.soundInteract.PlayOneShot(new TargetInfo(equipper.Position, equipper.Map));
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return base.CanApplyOn(target, dest) && Valid(target);
        }

        public override bool Valid(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.Valid(target, showMessages))
            {
                return false;
            }

            if (target.Thing != null)
            {
                if (target.Thing.def.equipmentType == EquipmentType.Primary)
                {
                    if (parent.pawn.equipment.Primary != null && showMessages)
                    {
                        Messages.Message("Force.Pull_AlreadyEquipped".Translate(),
                                        parent.pawn, MessageTypeDefOf.RejectInput);
                        return false;
                    }
                }
                else if (!(target.Thing is Pawn))
                {
                    if (showMessages)
                    {
                        Messages.Message("Force.Pull_InvalidTarget".Translate(),
                                      parent.pawn, MessageTypeDefOf.RejectInput);
                    }
                    return false;
                }
            }

            return true;
        }
    }
}