using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TheForce_Standalone.Telekinesis
{
    public class CompAbilityEffect_ForceDisassemble : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Thing targetThing = target.Thing;
            if (targetThing == null)
                return;

            float forceCost = CalculateForceCost(targetThing);
            if (!TryConsumeForce(forceCost))
            {
                Messages.Message("Force_NotEnoughForce".Translate(forceCost.ToString("F1")), MessageTypeDefOf.RejectInput);
                return;
            }

            if (targetThing is Pawn pawn && IsMechanoid(pawn))
            {
                if (CheckIntelligenceSkillForMechanoid(pawn))
                {
                    TryButcherMechanoid(pawn);
                }
                else
                {
                    Messages.Message("Force_IntelligenceSkillTooLow".Translate(GetRequiredIntelligenceSkillLevel(pawn)), MessageTypeDefOf.RejectInput);
                }
            }
            else if (targetThing is Building building && building.def.building.IsDeconstructible)
            {
                TryDeconstructBuilding(building);
            }
            else if (targetThing.def.smeltable || targetThing.def.IsWeapon || targetThing.def.IsApparel)
            {
                TryDisassembleItem(targetThing);
            }
        }

        private bool TryConsumeForce(float amount)
        {
            CompClass_ForceUser compForceUser = parent.pawn.GetComp<CompClass_ForceUser>();
            if (compForceUser != null && compForceUser.currentFP >= amount)
            {
                compForceUser.DrainFP(amount);
                return true;
            }
            return false;
        }

        private float CalculateForceCost(Thing target)
        {
            float baseCost = 10f;

            if (target is Pawn pawn && IsMechanoid(pawn))
            {
                return baseCost * GetMechanoidWeightMultiplier(pawn);
            }
            else
            {
                float mass = target.GetStatValue(StatDefOf.Mass);
                return baseCost * Mathf.Max(1f, mass / 10f);
            }
        }

        private float GetMechanoidWeightMultiplier(Pawn mechanoid)
        {
            if (mechanoid.def.race.mechWeightClass == null)
                return 1f;

            string weightClassName = mechanoid.def.race.mechWeightClass.defName;

            if (weightClassName == "Light")
                return 1f;
            else if (weightClassName == "Medium")
                return 2f;
            else if (weightClassName == "Heavy")
                return 4f;
            else if (weightClassName == "UltraHeavy")
                return 8f;
            else
                return 1f;
        }

        private bool CheckIntelligenceSkillForMechanoid(Pawn mechanoid)
        {
            int requiredSkillLevel = GetRequiredIntelligenceSkillLevel(mechanoid);
            int pawnSkillLevel = parent.pawn.skills.GetSkill(SkillDefOf.Intellectual).Level;
            return pawnSkillLevel >= requiredSkillLevel;
        }

        private int GetRequiredIntelligenceSkillLevel(Pawn mechanoid)
        {
            if (mechanoid.def.race.mechWeightClass == null)
                return 6;

            string weightClassName = mechanoid.def.race.mechWeightClass.defName;

            if (weightClassName == "Light")
                return 6;
            else if (weightClassName == "Medium")
                return 10;
            else if (weightClassName == "Heavy")
                return 14;
            else if (weightClassName == "UltraHeavy")
                return 18;
            else
                return 6;
        }

        private bool IsMechanoid(Pawn pawn)
        {
            return !pawn.RaceProps.IsFlesh;
        }

        private void TryButcherMechanoid(Pawn mechanoid)
        {
            Map map = parent.pawn.Map;

            if (!mechanoid.Dead)
            {
                mechanoid.Kill(new DamageInfo(DamageDefOf.Crush, 999f, -1f));
            }

            if (mechanoid.def.butcherProducts != null)
            {
                foreach (ThingDefCountClass butcherProduct in mechanoid.def.butcherProducts)
                {
                    Thing product = ThingMaker.MakeThing(butcherProduct.thingDef);
                    product.stackCount = butcherProduct.count;
                    GenPlace.TryPlaceThing(product, mechanoid.Position, map, ThingPlaceMode.Near);
                }
            }

            mechanoid.Corpse?.Destroy();
            FleckMaker.ThrowMicroSparks(mechanoid.Position.ToVector3(), map);
        }

        private void TryDeconstructBuilding(Building building)
        {
            if (building.def != null)
            {
                building.Destroy(DestroyMode.Deconstruct);
            }
            else
            {
                Messages.Message("Force_CannotDeconstruct".Translate(), MessageTypeDefOf.RejectInput);
            }
        }

        private void TryDisassembleItem(Thing item)
        {
            Map map = parent.pawn.Map;
            if (item.def.smeltable || item.def.PotentiallySmeltable)
            {
                SmeltItem(item, map);
            }
            else
            {
                Messages.Message("Force_CannotDisassemble".Translate(item.Label), MessageTypeDefOf.RejectInput);
            }
        }

        private void SmeltItem(Thing item, Map map)
        {
            IEnumerable<Thing> smeltProducts = item.def.smeltProducts?.Select(productDef =>
                ThingMaker.MakeThing(productDef.thingDef, productDef.stuff ?? item.Stuff)
            ) ?? Enumerable.Empty<Thing>();

            float efficiency = item.def.smeltable ? 1f : 0.75f;

            foreach (Thing product in smeltProducts)
            {
                product.stackCount = Mathf.Max(1, Mathf.RoundToInt(product.stackCount * efficiency));
                GenPlace.TryPlaceThing(product, item.Position, map, ThingPlaceMode.Near);
            }

            item.Destroy();
            FleckMaker.ThrowSmoke(item.Position.ToVector3(), map, 1f);
            SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(item.Position, map));
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
                return false;

            Thing targetThing = target.Thing;
            if (targetThing == null)
                return true;

            if (targetThing is Pawn pawn && IsMechanoid(pawn))
            {
                int requiredSkill = GetRequiredIntelligenceSkillLevel(pawn);
                int currentSkill = parent.pawn.skills.GetSkill(SkillDefOf.Intellectual).Level;

                if (currentSkill < requiredSkill)
                {
                    if (throwMessages)
                    {
                        Messages.Message("Force_IntelligenceSkillTooLow".Translate(requiredSkill), MessageTypeDefOf.RejectInput);
                    }
                    return false;
                }
            }

            float forceCost = CalculateForceCost(targetThing);
            CompClass_ForceUser compForceUser = parent.pawn.GetComp<CompClass_ForceUser>();
            if (compForceUser != null && compForceUser.currentFP < forceCost)
            {
                if (throwMessages)
                {
                    Messages.Message("Force_NotEnoughForce".Translate(forceCost.ToString("F1")), MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            return true;
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!base.CanApplyOn(target, dest))
                return false;

            Thing targetThing = target.Thing;
            if (targetThing == null)
                return false;

            if (targetThing is Pawn pawn && IsMechanoid(pawn))
            {
                return pawn.def.butcherProducts != null && pawn.def.butcherProducts.Count > 0;
            }

            else if (targetThing is Building building)
            {
                return building.def.building.IsDeconstructible;
            }

            else if (targetThing.def.smeltable)
            {
                return targetThing.def.smeltable || targetThing.def.PotentiallySmeltable;
            }

            return false;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            Thing targetThing = target.Thing;
            if (targetThing == null)
                return null;

            if (targetThing is Pawn pawn && IsMechanoid(pawn))
            {
                int requiredSkill = GetRequiredIntelligenceSkillLevel(pawn);
                int currentSkill = parent.pawn.skills.GetSkill(SkillDefOf.Intellectual).Level;
                float forceCost = CalculateForceCost(pawn);

                string skillInfo = $" (Int: {currentSkill}/{requiredSkill})";
                string forceInfo = $" ({forceCost:F1} force)";
                string baseText = "Force_DisassembleMechanoid".Translate();

                if (currentSkill < requiredSkill)
                {
                    return baseText + skillInfo + forceInfo + " - " + "Force_SkillTooLow".Translate();
                }
                else
                {
                    return baseText + skillInfo + forceInfo;
                }
            }
            else if (targetThing is Building)
            {
                float forceCost = CalculateForceCost(targetThing);
                return "Force_Deconstruct".Translate() + $" ({forceCost:F1} force)";
            }
            else if (targetThing.def.IsStuff || targetThing.def.IsWeapon || targetThing.def.IsApparel)
            {
                float forceCost = CalculateForceCost(targetThing);
                return "Force_Disassemble".Translate() + $" ({forceCost:F1} force)";
            }

            return null;
        }
    }

    public class CompProperties_AbilityForceDisassemble : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityForceDisassemble()
        {
            compClass = typeof(CompAbilityEffect_ForceDisassemble);
        }
    }
}