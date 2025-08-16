using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Darkside.Mechu_Deru
{
    public class HediffComp_MechuDeru : HediffComp
    {
        public List<Thing> spawnedShields = new List<Thing>();
        private List<HediffDef> studiedImplants = new List<HediffDef>();
        public Thing linkedTarget;
        private HashSet<Building> connectedBuildings = new HashSet<Building>();
        private CompPowerTrader compPower;
        private CompPowerBattery compPowerBattery;
        private float storedEnergy;
        private float maxStoredEnergy = 1000f;
        private float energyTransferRate = 10f;

        public float StoredEnergy
        {
            get => storedEnergy;
            set
            {
                storedEnergy = Mathf.Clamp(value, 0, maxStoredEnergy);
                if (storedEnergy <= 0)
                {
                    DestroySpawnedShields();
                    Messages.Message("Force.MechuDeru_EnergyDepleted".Translate(),
                                  Pawn,
                                  MessageTypeDefOf.NegativeEvent);
                }
            }
        }

        public float MaxStoredEnergy => maxStoredEnergy;

        public float EnergyTransferRate
        {
            get => energyTransferRate;
            set => energyTransferRate = Mathf.Max(value, 0);
        }

        public void LinkTo(Thing target)
        {
            if (target == null)
            {
                Log.Error("Force.MechuDeru_NullLinkTarget".Translate());
                return;
            }

            Unlink();
            linkedTarget = target;
            compPower = target.TryGetComp<CompPowerTrader>();
            compPowerBattery = target.TryGetComp<CompPowerBattery>();
            Messages.Message("Force.MechuDeru_LinkEstablished".Translate(Pawn.LabelShort, target.Label),
                          Pawn,
                          MessageTypeDefOf.PositiveEvent);
        }

        public void Unlink()
        {
            if (linkedTarget == null) return;
            compPower?.SetUpPowerVars();
            Messages.Message("Force.MechuDeru_LinkTerminated".Translate(Pawn.LabelShort, linkedTarget.Label),
                          Pawn,
                          MessageTypeDefOf.NeutralEvent);
            linkedTarget = null;
            compPower = null;
            compPowerBattery = null;
        }

        private Need_MechEnergy GetLinkedMechEnergyNeed()
        {
            if (linkedTarget is Pawn linkedPawn)
            {
                return linkedPawn.needs?.TryGetNeed<Need_MechEnergy>();
            }
            return null;
        }

        public void StudyImplant(HediffDef implant)
        {
            if (implant != null && !studiedImplants.Contains(implant))
            {
                studiedImplants.Add(implant);
                Messages.Message("Force.MechuDeru_ImplantStudied".Translate(Pawn.LabelShort, implant.LabelCap),
                              Pawn,
                              MessageTypeDefOf.PositiveEvent);
            }
        }

        public bool HasStudied(HediffDef implant)
        {
            return implant != null && studiedImplants.Contains(implant);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            try
            {
                if (Pawn?.mechanitor == null)
                {
                    Unlink();
                    return;
                }

                maxStoredEnergy = 1000f * Mathf.Max(0, Pawn.mechanitor.TotalBandwidth - Pawn.mechanitor.UsedBandwidth);

                float efficiencyFactor = Pawn.GetStatValue(StatDef.Named("Force_MechuDeruEnergyEfficiency"));

                bool batteriesCharging = AreBatteriesCharging();
                float energyToGenerate = (maxStoredEnergy / 60000f) * efficiencyFactor;
                StoredEnergy += batteriesCharging ? energyToGenerate * 1.01f : energyToGenerate;

                if (linkedTarget != null)
                {
                    bool isEnergyAvailable = StoredEnergy > 0;

                    var powerComp = linkedTarget.TryGetComp<CompPowerTrader>();
                    if (powerComp != null && isEnergyAvailable)
                    {
                        float powerDraw = powerComp.Props.PowerConsumption / 100f;
                        float energyUsed = Mathf.Min(StoredEnergy, powerDraw);
                        StoredEnergy -= energyUsed / 2f;
                        powerComp.PowerOn = StoredEnergy > powerComp.Props.PowerConsumption ||
                                         powerComp.PowerNet?.CanPowerNow(powerComp) == true;
                    }

                    var batteryComp = linkedTarget.TryGetComp<CompPowerBattery>();
                    if (batteryComp != null && isEnergyAvailable)
                    {
                        ChargeBattery(batteryComp);
                        batteryComp.PowerNet?.batteryComps?
                            .Where(b => b != batteryComp && b.StoredEnergy < b.Props.storedEnergyMax)
                            .Do(ChargeBattery);
                    }

                    if (linkedTarget is Pawn targetPawn)
                    {
                        var need = GetLinkedMechEnergyNeed();
                        if (need != null && need.CurLevel <= need.MaxLevel && StoredEnergy > 0)
                        {
                            float transferAmount = 10f / need.FallPerDay;
                            need.CurLevel += transferAmount;
                            StoredEnergy -= transferAmount;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Force.MechuDeru_TickError".Translate(Pawn?.LabelShort ?? "null pawn", ex));
                Unlink();
            }
        }

        private void DestroySpawnedShields()
        {
            for (int i = spawnedShields.Count - 1; i >= 0; i--)
            {
                Thing shield = spawnedShields[i];
                if (shield != null && !shield.Destroyed)
                {
                    shield.Destroy();
                }
                spawnedShields.RemoveAt(i);
            }
        }

        public void RegisterShield(Thing shield)
        {
            if (shield != null && !spawnedShields.Contains(shield))
            {
                spawnedShields.Add(shield);
            }
        }

        public void UnregisterShield(Thing shield)
        {
            if (shield != null && spawnedShields.Contains(shield))
            {
                spawnedShields.Remove(shield);
            }
        }

        private bool AreBatteriesCharging()
        {
            if (linkedTarget == null || compPowerBattery == null) return false;
            var powerNet = compPowerBattery.PowerNet;
            return powerNet?.batteryComps.Any(b => b.StoredEnergy < b.Props.storedEnergyMax) ?? false;
        }

        private void ChargeBattery(CompPowerBattery battery)
        {
            if (StoredEnergy <= 0.999f || battery.StoredEnergy >= battery.Props.storedEnergyMax) return;

            float availableCapacity = battery.Props.storedEnergyMax - battery.StoredEnergy;
            float maxChargeableEnergy = Mathf.Min(EnergyTransferRate, StoredEnergy / battery.Props.efficiency);
            float chargeAmount = Mathf.Min(availableCapacity, maxChargeableEnergy);

            if (StoredEnergy >= chargeAmount * battery.Props.efficiency)
            {
                battery.AddEnergy(chargeAmount);
                StoredEnergy -= chargeAmount * battery.Props.efficiency;
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            Unlink();
            Messages.Message("Force.MechuDeru_ImplantRemoved".Translate(Pawn.LabelShort),
                          Pawn,
                          MessageTypeDefOf.NeutralEvent);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Collections.Look(ref studiedImplants, "studiedImplants", LookMode.Def);
            Scribe_Values.Look(ref storedEnergy, nameof(storedEnergy));
            Scribe_Values.Look(ref maxStoredEnergy, nameof(maxStoredEnergy));
            Scribe_Values.Look(ref energyTransferRate, nameof(energyTransferRate));
            Scribe_References.Look(ref linkedTarget, nameof(linkedTarget));
            studiedImplants ??= new List<HediffDef>();
        }

        public override string CompTipStringExtra
        {
            get
            {
                string tip = "Force.MechuDeru_EnergyStatus".Translate(StoredEnergy.ToString("F2"), maxStoredEnergy.ToString("F2"));

                if (linkedTarget != null)
                {
                    tip += "\n" + "Force.MechuDeru_LinkedTarget".Translate(linkedTarget.Label);

                    if (compPower != null)
                    {
                        tip += "\n" + "Force.MechuDeru_PowerConsumption".Translate(compPower.Props.PowerConsumption);
                    }

                    if (compPowerBattery != null)
                    {
                        tip += "\n" + "Force.MechuDeru_BatteryStatus".Translate(
                            compPowerBattery.StoredEnergy.ToString("F2"),
                            compPowerBattery.Props.storedEnergyMax.ToString("F2"));
                    }
                }

                return tip;
            }
        }
    }

    public class HediffCompProperties_MechuDeru : HediffCompProperties
    {
        public HediffCompProperties_MechuDeru() => compClass = typeof(HediffComp_MechuDeru);
    }
}