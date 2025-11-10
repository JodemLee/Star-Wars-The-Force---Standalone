using RimWorld;
using System.Collections.Generic;
using TheForce_Standalone.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Abilities.Darkside
{
    public class CompAbilityEffect_ForceDestruction : RimWorld.CompAbilityEffect_Explosion
    {
        public new CompProperties_AbilityForceDestruction Props => (CompProperties_AbilityForceDestruction)props;

        private float GetRadiusForPawn(float fpPercentage)
        {
            float baseRadius = Props.explosionRadius;
            return baseRadius * fpPercentage;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = parent.pawn;
            if (!Find.Selector.IsSelected(pawn))
            {
                ExecuteDestruction(target, 1f);
            }
            else if (!pawn.IsColonistPlayerControlled || Find.Selector.SingleSelectedObject == pawn)
            {
                ShowForceDrainMenu(target);
                return;
            }
        }

        private void ShowForceDrainMenu(LocalTargetInfo target)
        {
            var menuOptions = Utility_FPCostMenu.CreateStandardPercentages(ExecuteDestruction);
            Utility_FPCostMenu.ShowForcePercentageMenu(parent.pawn, target, menuOptions, "Force.Destruction_NotEnoughFP");
        }

        private void ExecuteDestruction(LocalTargetInfo target, float fpPercentage)
        {
            var pawn = parent.pawn;
            var forceUser = pawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null) return;

            float fpToDrain;
            if (fpPercentage >= 1f)
            {
                fpToDrain = forceUser.currentFP;
            }
            else
            {
                fpToDrain = forceUser.MaxFP * fpPercentage;
            }

            if (!forceUser.TrySpendFP(fpToDrain)) return;

            // Calculate actual percentage based on drained FP for explosion scaling
            float actualPercentage = fpToDrain / forceUser.MaxFP;

            float explosionRadius = GetRadiusForPawn(actualPercentage);
            float damageFactor = actualPercentage * 2f;
            int baseDamage = Props.damageAmount == -1 ? Props.damageDef.defaultDamage : Props.damageAmount;
            int actualDamage = Mathf.RoundToInt(baseDamage * damageFactor);
            List<Thing> thingstoIgnore = new List<Thing> { this.parent.pawn };

            MakeStaticFleck(target.Cell, target.Pawn.Map, Props.fleckDef, explosionRadius / 2, 1);

            GenExplosion.DoExplosion(
                center: this.parent.pawn.InteractionCell,
                map: pawn.Map,
                radius: explosionRadius,
                damType: Props.damageDef,
                instigator: pawn,
                damAmount: actualDamage,
                armorPenetration: Props.armorPenetration,
                explosionSound: Props.soundExplode,
                weapon: parent.verb?.EquipmentSource?.def,
                ignoredThings: thingstoIgnore
            );

            if (actualPercentage >= 0.66f)
            {
                Messages.Message("Force.Destruction_PowerfulEffect".Translate(pawn.LabelShort),
                                pawn, MessageTypeDefOf.NeutralEvent);
            }
        }

        private void MakeStaticFleck(IntVec3 cell, Map map, FleckDef fleckDef, float scale, float rotationRate)
        {
            if (map == null || fleckDef == null) return;

            FleckCreationData data = FleckMaker.GetDataStatic(cell.ToVector3Shifted(), map, fleckDef, scale);
            data.rotationRate = rotationRate;
            map.flecks.CreateFleck(data);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            var pawn = parent.pawn;
            var forceUser = pawn.GetComp<CompClass_ForceUser>();

            if (forceUser == null || !forceUser.IsValidForceUser)
            {
                if (throwMessages)
                {
                    Messages.Message("Force.Ability_NotForceUser".Translate(),
                                    pawn, MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            return base.Valid(target, throwMessages);
        }
    }

    public class CompProperties_AbilityForceDestruction : CompProperties_AbilityExplosion
    {
        public FleckDef fleckDef;

        public CompProperties_AbilityForceDestruction()
        {
            compClass = typeof(CompAbilityEffect_ForceDestruction);
        }
    }
}