using RimWorld;
using Verse;

namespace TheForce_Standalone.Telekinesis
{
    internal class CompAbilityEffect_ForcePush : CompAbilityEffect
    {
        public float baseDamage = 1f;
        public bool usePsycastStat = false;
        public int offsetMultiplier = 1;

        Force_ModSettings modSettings = new Force_ModSettings();

        public CompAbilityEffect_ForcePush()
        {
            modSettings = new Force_ModSettings();
        }

        public int GetOffsetMultiplier()
        {
            if (Force_ModSettings.usePsycastStat)
            {
                offsetMultiplier = (int)(offsetMultiplier * parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity));
                return offsetMultiplier;
            }
            else
            {
                offsetMultiplier = (int)Force_ModSettings.offSetMultiplier;
            }
            return offsetMultiplier;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (!target.IsValid || parent.pawn?.Map == null) return;

            IntVec3 pushPos = TelekinesisUtility.CalculatePushPosition(
                parent.pawn.Position,
                target.Cell,
                GetOffsetMultiplier(),
                parent.pawn.Map,
                out bool hitWall
            );

            if (target.Thing is Pawn pawnTarget)
            {
                TelekinesisUtility.LaunchPawn(
                    pawnTarget,
                    pushPos,
                    parent,
                    ForceDefOf.Force_ThrownPawnPush,
                    parent.pawn.Map,
                    hitWall,
                    hitWall ? pushPos : (IntVec3?)null
                );
            }
            else if (target.Thing != null)
            {
                target.Thing.Position = pushPos;
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
            return true;
        }
    }
}