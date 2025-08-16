using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Darkside.Mechu_Deru
{
    internal class CompAbilityEffect_Mechu_FluxInfusis : CompAbilityEffect
    {
        private const float FPToPowerRatio = 0.1f;
        private const float PowerToFPRatio = 10f;
        private const float MaxPowerRestore = 2500f;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (target.Thing == null || parent.pawn == null || parent.pawn.Map == null)
            {
                return;
            }

            var targetBuilding = target.Thing as Building;
            if (targetBuilding == null || !Valid(target, true))
            {
                return;
            }

            var forceUser = parent.pawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null)
            {
                Messages.Message("Force.MechuFlux_NotForceUser".Translate(),
                                parent.pawn,
                                MessageTypeDefOf.RejectInput);
                return;
            }

            List<FloatMenuOption> primaryOptions = new List<FloatMenuOption>
            {
                new FloatMenuOption("Force.MechuFlux_InfuseOption".Translate(),
                                  () => ShowInfuseOptions(targetBuilding, forceUser)),
                new FloatMenuOption("Force.MechuFlux_AbsorbOption".Translate(),
                                  () => ShowAbsorbOptions(targetBuilding, forceUser))
            };

            Find.WindowStack.Add(new FloatMenu(primaryOptions));
        }

        private void ShowAbsorbOptions(Building targetBuilding, CompClass_ForceUser forceUser)
        {
            CompPower powerComp = targetBuilding.GetComp<CompPower>();
            if (powerComp == null || powerComp.PowerNet == null) return;

            float totalPowerAvailable = powerComp.PowerNet.batteryComps.Sum(b => b.StoredEnergy);
            float maxFPRestore = forceUser.MaxFP - forceUser.currentFP;

            List<FloatMenuOption> absorbOptions = new List<FloatMenuOption>();

            foreach (float percentage in new float[] { 0.25f, 0.5f, 0.75f, 1f })
            {
                float powerToDrain = Math.Min(totalPowerAvailable * percentage, maxFPRestore * PowerToFPRatio);
                float fpToRestore = powerToDrain / PowerToFPRatio;

                string label = percentage < 1f
                    ? "Force.MechuFlux_AbsorbPartial".Translate(percentage * 100f, powerToDrain, fpToRestore)
                    : "Force.MechuFlux_AbsorbFull".Translate(powerToDrain, fpToRestore);

                absorbOptions.Add(new FloatMenuOption(label, () =>
                {
                    AbsorbPower(targetBuilding, percentage, forceUser);
                }));
            }

            string totalPowerLabel = "Force.MechuFlux_TotalAvailable".Translate(totalPowerAvailable, totalPowerAvailable / PowerToFPRatio);
            absorbOptions.Add(new FloatMenuOption(totalPowerLabel, null) { Disabled = true });

            Find.WindowStack.Add(new FloatMenu(absorbOptions));
        }

        private void ShowInfuseOptions(Building targetBuilding, CompClass_ForceUser forceUser)
        {
            List<FloatMenuOption> infuseOptions = new List<FloatMenuOption>();
            float maxPossibleInfusion = forceUser.currentFP / FPToPowerRatio;

            foreach (float percentage in new float[] { 0.25f, 0.5f, 0.75f, 1f })
            {
                float fpCost = forceUser.currentFP * percentage;
                float powerRestored = fpCost / FPToPowerRatio;

                infuseOptions.Add(new FloatMenuOption(
                    "Force.MechuFlux_InfuseStandard".Translate(percentage * 100f, fpCost, powerRestored),
                    () => InfusePower(targetBuilding, percentage, false, forceUser)
                ));
            }

            infuseOptions.Add(new FloatMenuOption(
                "Force.MechuFlux_InfuseOverload".Translate(forceUser.currentFP, maxPossibleInfusion),
                () => InfusePower(targetBuilding, 1f, true, forceUser)
            ));

            Find.WindowStack.Add(new FloatMenu(infuseOptions));
        }

        private void InfusePower(Building targetBuilding, float percentage, bool causeExplosion, CompClass_ForceUser forceUser)
        {
            CompPower powerComp = targetBuilding.GetComp<CompPower>();
            if (powerComp == null || powerComp.PowerNet == null)
            {
                Messages.Message("Force.MechuFlux_InvalidPowerNetwork".Translate(),
                              parent.pawn,
                              MessageTypeDefOf.RejectInput);
                return;
            }

            float fpCost = forceUser.currentFP * percentage;
            float powerRestored = fpCost / FPToPowerRatio;

            if (fpCost > 0 && forceUser.TrySpendFP(fpCost))
            {
                float remainingPowerToAdd = powerRestored;
                foreach (var battery in powerComp.PowerNet.batteryComps)
                {
                    if (remainingPowerToAdd <= 0)
                        break;

                    float freeCapacity = battery.Props.storedEnergyMax - battery.StoredEnergy;
                    float powerToAdd = Math.Min(remainingPowerToAdd, freeCapacity);
                    battery.AddEnergy(powerToAdd);
                    remainingPowerToAdd -= powerToAdd;
                }

                if (causeExplosion)
                {
                    ShortCircuitUtility.DoShortCircuit(targetBuilding);
                    Messages.Message("Force.MechuFlux_OverloadExplosion".Translate(),
                                  parent.pawn,
                                  MessageTypeDefOf.NegativeEvent);
                    GenExplosion.DoExplosion(targetBuilding.Position, parent.pawn.Map, 3.9f, DamageDefOf.Flame, parent.pawn);
                }

                Messages.Message("Force.MechuFlux_InfuseSuccess".Translate(fpCost, powerRestored),
                              parent.pawn,
                              MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("Force.MechuFlux_InsufficientFP".Translate(),
                              parent.pawn,
                              MessageTypeDefOf.RejectInput);
            }
        }

        private void AbsorbPower(Building targetBuilding, float percentage, CompClass_ForceUser forceUser)
        {
            CompPower powerComp = targetBuilding.GetComp<CompPower>();
            if (powerComp == null || powerComp.PowerNet == null)
            {
                Messages.Message("Force.MechuFlux_InvalidPowerNetwork".Translate(),
                              parent.pawn,
                              MessageTypeDefOf.RejectInput);
                return;
            }

            float totalPowerAvailable = powerComp.PowerNet.batteryComps.Sum(b => b.StoredEnergy);
            float maxFPRestore = forceUser.MaxFP - forceUser.currentFP;
            float powerDrained = Math.Min(totalPowerAvailable * percentage, maxFPRestore * PowerToFPRatio);
            float fpRestored = powerDrained / PowerToFPRatio;

            if (fpRestored > 0)
            {
                powerComp.PowerNet.batteryComps.ForEach(battery =>
                {
                    if (powerDrained <= 0)
                        return;

                    float batteryEnergy = battery.StoredEnergy;
                    float energyDrainedFromBattery = Math.Min(powerDrained, batteryEnergy);
                    battery.DrawPower(energyDrainedFromBattery);
                    powerDrained -= energyDrainedFromBattery;
                });

                forceUser.RecoverFP(fpRestored);
                Messages.Message("Force.MechuFlux_AbsorbSuccess".Translate(fpRestored, powerDrained),
                              parent.pawn,
                              MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("Force.MechuFlux_InsufficientPower".Translate(),
                              parent.pawn,
                              MessageTypeDefOf.RejectInput);
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            base.Valid(target, throwMessages);

            if (parent.pawn.GetComp<CompClass_ForceUser>() == null)
            {
                if (throwMessages)
                {
                    Messages.Message("Force.MechuFlux_NotForceUser".Translate(),
                                  parent.pawn,
                                  MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            Thing targetThing = target.Thing;
            var targetBuilding = targetThing as Building;
            if (targetBuilding == null || !targetBuilding.def.building.isPowerConduit)
            {
                if (throwMessages)
                {
                    Messages.Message("Force.MechuFlux_InvalidTarget".Translate(),
                                  parent.pawn,
                                  MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            CompPower powerComp = targetBuilding.GetComp<CompPower>();
            if (powerComp == null || powerComp.PowerNet == null)
            {
                if (throwMessages)
                {
                    Messages.Message("Force.MechuFlux_InvalidPowerNetwork".Translate(),
                                  parent.pawn,
                                  MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            return true;
        }
    }
}