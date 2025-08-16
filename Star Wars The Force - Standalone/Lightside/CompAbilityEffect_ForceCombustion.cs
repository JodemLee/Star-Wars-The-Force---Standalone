using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Lightside
{
    public class CompAbilityEffect_ForceCombustion : CompAbilityEffect
    {
        private new CompProperties_AbilityExplosion Props => (CompProperties_AbilityExplosion)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn targetPawn = target.Thing as Pawn;
            if (targetPawn == null || targetPawn == parent.pawn)
            {
                Messages.Message("Force.Combustion.InvalidTarget".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            List<ThingWithComps> eligibleItems = new List<ThingWithComps>();
            eligibleItems.AddRange(targetPawn.equipment.AllEquipmentListForReading);
            eligibleItems.AddRange(targetPawn.apparel.WornApparel);
            eligibleItems.AddRange(targetPawn.inventory.innerContainer.OfType<ThingWithComps>());

            if (eligibleItems.Count == 0)
            {
                Messages.Message("Force.Combustion.NoItems".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            if (Find.Selector.IsSelected(parent.pawn))
            {
                // Show selection menu if player is controlling
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (ThingWithComps item in eligibleItems)
                {
                    float itemMass = item.GetStatValue(StatDefOf.Mass);
                    string label = "Force.Combustion.ItemSelectionLabel".Translate(item.LabelCap, itemMass);
                    ThingWithComps capturedItem = item;
                    options.Add(new FloatMenuOption(label, () => ExecuteCombustion(target, capturedItem)));
                }
                Find.WindowStack.Add(new FloatMenu(options, "Force.Combustion.SelectItem".Translate()));
            }
            else
            {
                ThingWithComps randomItem = eligibleItems.RandomElement();
                ExecuteCombustion(target, randomItem);
            }
        }

        private void ExecuteCombustion(LocalTargetInfo target, ThingWithComps selectedItem)
        {
            Pawn targetPawn = target.Thing as Pawn;
            if (targetPawn == null || selectedItem.Destroyed) return;

            float itemMass = selectedItem.GetStatValue(StatDefOf.Mass);
            float explosionRadius = Mathf.Clamp(itemMass, 1f, 4f);
            int damageAmount = (int)(Props.damageAmount * itemMass);

            // Create explosion
            GenExplosion.DoExplosion(
                target.Cell,
                targetPawn.Map,
                explosionRadius,
                Props.damageDef,
                parent.pawn,
                damageAmount,
                Props.armorPenetration,
                Props.soundExplode,
                null, null, selectedItem
            );

            // Remove and destroy item
            if (selectedItem.def.IsApparel && targetPawn.apparel.WornApparel.Contains(selectedItem as Apparel))
            {
                targetPawn.apparel.Remove(selectedItem as Apparel);
                selectedItem.Destroy();
            }
            else if (selectedItem.def.IsWeapon && targetPawn.equipment.AllEquipmentListForReading.Contains(selectedItem))
            {
                targetPawn.equipment.DestroyEquipment(selectedItem);
            }
            else if (targetPawn.inventory.innerContainer.Contains(selectedItem))
            {
                targetPawn.inventory.innerContainer.Remove(selectedItem);
                selectedItem.Destroy();
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Thing == parent.pawn)
            {
                if (throwMessages)
                {
                    Messages.Message("Force.Combustion.CannotTargetSelf".Translate(), MessageTypeDefOf.RejectInput);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }
    }
}