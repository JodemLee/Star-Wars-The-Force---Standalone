using RimWorld;
using System.Collections.Generic;
using TheForce_Standalone.Darkside.SithSorcery.Artifacts;
using TheForce_Standalone.Generic;
using Verse;

namespace TheForce_Standalone.Darkside
{
    public class CompAbilityEffect_EssenceTransfer : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect_EssenceTransfer Props => (CompProperties_AbilityEffect_EssenceTransfer)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            // Check if target is a possession mask
            var possessionMask = target.Thing?.TryGetComp<CompPossessionMask>();
            var casterForce = parent.pawn.GetComp<CompClass_ForceUser>();

            if (possessionMask != null && possessionMask.CanBeTargetedByEssenceTransfer())
            {
                if (possessionMask.TryCaptureGhost(parent.pawn))
                {
                    casterForce?.RecoverFP(casterForce.MaxFP);
                }
                return;
            }

            // Use utility for pawn swapping
            Pawn targetPawn = target.Pawn;
            if (targetPawn == null || targetPawn.Dead ||
                targetPawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) >= Props.requiredConsciousnessCapacity)
            {
                return;
            }

            if (PawnIdentitySwapper.CanSwapWithPawn(parent.pawn, targetPawn))
            {
                HandleGhostCaster(parent.pawn);
                PawnIdentitySwapper.SwapPawnIdentities(parent.pawn, targetPawn);
                casterForce?.RecoverFP(casterForce.MaxFP);
            }
        }

        private static void HandleGhostCaster(Pawn casterPawn)
        {
            if (!ForceGhostUtility.IsForceGhost(casterPawn)) return;

            var forceUser = casterPawn.GetComp<CompClass_ForceUser>();
            forceUser?.GhostMechanics.LinkedObject = null;

            var ghostHediff = casterPawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Ghost);
            if (ghostHediff != null) casterPawn.health.RemoveHediff(ghostHediff);

            var sithGhostHediff = casterPawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_SithGhost);
            if (sithGhostHediff != null) casterPawn.health.RemoveHediff(sithGhostHediff);

            var zombieHediff = casterPawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_SithZombie);
            if (zombieHediff != null) casterPawn.health.RemoveHediff(zombieHediff);

            casterPawn.apparel?.DropAll(casterPawn.Position);
            casterPawn.Destroy(DestroyMode.Vanish);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            var possessionMask = target.Thing?.TryGetComp<CompPossessionMask>();
            if (possessionMask != null)
            {
                if (!possessionMask.CanBeTargetedByEssenceTransfer())
                {
                    if (throwMessages)
                        Messages.Message("Cannot target mask while it is possessing someone.", target.Thing, MessageTypeDefOf.RejectInput);
                    return false;
                }

                if (ForceGhostUtility.IsForceGhost(parent.pawn))
                    return true;

                if (throwMessages)
                    Messages.Message("Only ghosts can transfer their essence into the mask.", target.Thing, MessageTypeDefOf.RejectInput);
                return false;
            }

            Pawn pawn = target.Pawn;
            if (pawn == null) return false;

            if (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) >= Props.requiredConsciousnessCapacity)
            {
                if (throwMessages)
                    Messages.Message("Target's consciousness is too high for essence transfer.", pawn, MessageTypeDefOf.RejectInput);
                return false;
            }

            return PawnIdentitySwapper.CanSwapWithPawn(parent.pawn, pawn);
        }
    }

    public class CompProperties_AbilityEffect_EssenceTransfer : CompProperties_AbilityEffect
    {
        public float requiredConsciousnessCapacity = 1f;

        public CompProperties_AbilityEffect_EssenceTransfer()
        {
            compClass = typeof(CompAbilityEffect_EssenceTransfer);
        }
    }
}