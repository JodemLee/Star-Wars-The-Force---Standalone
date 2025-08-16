using RimWorld;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Nightsister
{
    public class CompAbilityEffect_NightsisterTeleport : CompAbilityEffect_Teleport
    {
        private const float BaseFPCost = 8f;
        private const float FPCostPerTile = 0.05f;
        private const float MapTransitionFPCost = 15f;
        private const float MinFPCost = 5f;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!target.HasThing) return;

            var forceUser = parent.pawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null) return;

            float distance = target.Cell.DistanceTo(dest.Cell);
            float fpCost = CalculateFPCost(distance, false);

            if (!forceUser.TrySpendFP(fpCost))
            {
                Messages.Message("Force.NightsisterTeleport.NotEnoughFP".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            base.Apply(target, dest);
            LocalTargetInfo destination = GetDestination(dest.IsValid ? dest : target);
            if (!destination.IsValid) return;

            // Visual effects
            Pawn pawn = parent.pawn;
            if (!parent.def.HasAreaOfEffect)
            {
                parent.AddEffecterToMaintain(ForceDefOf.Force_Magick_Entry.Spawn(target.Thing, pawn.Map), target.Thing.Position, 60);
                parent.AddEffecterToMaintain(ForceDefOf.Force_Magick_Exit.Spawn(destination.Cell, pawn.Map), destination.Cell, 60);
            }

            // Handle the teleport
            target.Thing.TryGetComp<CompCanBeDormant>()?.WakeUp();
            target.Thing.Position = destination.Cell;

            if (target.Thing is Pawn pawn2)
            {
                if ((pawn2.Faction == Faction.OfPlayer || pawn2.IsPlayerControlled) &&
                    pawn2.Position.Fogged(pawn2.Map))
                {
                    FloodFillerFog.FloodUnfog(pawn2.Position, pawn2.Map);
                }
                pawn2.stances.stunner.StunFor(Props.stunTicks.RandomInRange, parent.pawn, addBattleLog: false, showMote: false);
                pawn2.Notify_Teleported();
                SendSkipUsedSignal(pawn2.Position, pawn2);
            }

            if (Props.destClamorType != null)
            {
                GenClamor.DoClamor(pawn, target.Cell, Props.destClamorRadius, Props.destClamorType);
            }
        }

        private float CalculateFPCost(float distance, bool changingMaps)
        {
            float cost = BaseFPCost + (distance * FPCostPerTile);
            if (changingMaps) cost += MapTransitionFPCost;
            return Mathf.Max(cost, MinFPCost);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            var forceUser = parent.pawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null || forceUser.currentFP < MinFPCost)
            {
                if (throwMessages)
                {
                    Messages.Message(
                        "Force.NightsisterTeleport.MinFPRequired".Translate(MinFPCost),
                        MessageTypeDefOf.RejectInput);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }
    }
}