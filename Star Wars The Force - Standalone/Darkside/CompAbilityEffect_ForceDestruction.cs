using RimWorld;
using System.Collections.Generic;
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
            var pawn = parent.pawn;
            var forceUser = pawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null) return;

            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                new FloatMenuOption("Force.Destruction_Drain25".Translate(), () => ExecuteDestruction(target, 0.25f)),
                new FloatMenuOption("Force.Destruction_Drain33".Translate(), () => ExecuteDestruction(target, 0.33f)),
                new FloatMenuOption("Force.Destruction_Drain50".Translate(), () => ExecuteDestruction(target, 0.5f)),
                new FloatMenuOption("Force.Destruction_Drain66".Translate(), () => ExecuteDestruction(target, 0.66f)),
                new FloatMenuOption("Force.Destruction_Drain100".Translate(), () => ExecuteDestruction(target, 1f))
            };

            for (int i = 0; i < options.Count; i++)
            {
                float percentage = 0.25f + i * 0.16f;
                if (percentage > 1f) percentage = 1f;

                float fpToDrain = forceUser.MaxFP * percentage;
                if (forceUser.currentFP < fpToDrain)
                {
                    options[i].Disabled = true;
                    options[i].tooltip = "Force.Destruction_NotEnoughFP".Translate(pawn.LabelShort, fpToDrain);
                }
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void ExecuteDestruction(LocalTargetInfo target, float fpPercentage)
        {
            var pawn = parent.pawn;
            var forceUser = pawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null) return;

            float fpToDrain = forceUser.MaxFP * fpPercentage;
            if (!forceUser.TrySpendFP(fpToDrain)) return;

            float explosionRadius = GetRadiusForPawn(fpPercentage);
            float damageFactor = fpPercentage * 2f;
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

            if (fpPercentage >= 0.66f)
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