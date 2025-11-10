using RimWorld;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Lightside
{
    public class CompAbilityEffect_ForceLight : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect_ForceLight Props => (CompProperties_AbilityEffect_ForceLight)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if ((target.Pawn.IsMutant || target.Pawn.IsEntity) && ModsConfig.AnomalyActive)
            {
                var targetForceUser = target.Pawn?.GetComp<CompClass_ForceUser>();
                if (targetForceUser == null) return;
                float darksideAlignment = targetForceUser.Alignment.DarkSideAttunement;
                float scalingFactor = GetScalingFactor(100);
                ApplyScaledEffect(target, scalingFactor);
            }

            if (!target.Pawn.IsMutant)
            {
                var targetForceUser = target.Pawn?.GetComp<CompClass_ForceUser>();
                if (targetForceUser == null) return;
                float darksideAlignment = targetForceUser.Alignment.DarkSideAttunement;
                float scalingFactor = GetScalingFactor(darksideAlignment);
                ApplyScaledEffect(target, scalingFactor);
            }
        }

        private float GetScalingFactor(float darksideAlignment)
        {
            return Mathf.Clamp01(darksideAlignment / 100f);
        }

        private void ApplyScaledEffect(LocalTargetInfo target, float scalingFactor)
        {
            // Check if target pawn is valid and alive
            if (target.Pawn == null || !target.Pawn.Spawned || target.Pawn.Destroyed)
                return;

            if (Props.damageDef != null && scalingFactor > 0)
            {
                float scaledDamage = Props.baseDamage * scalingFactor;
                if (scaledDamage > 0)
                {
                    DamageInfo damageInfo = new DamageInfo(
                        Props.damageDef,
                        scaledDamage,
                        armorPenetration: Props.armorPenetration,
                        instigator: parent.pawn,
                        hitPart: target.Pawn.health?.hediffSet?.GetRandomNotMissingPart(Props.damageDef, BodyPartHeight.Undefined, BodyPartDepth.Undefined, null)
                    );

                    target.Pawn.TakeDamage(damageInfo);
                }
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages)) return false;
            return true;
        }
    }

    public class CompProperties_AbilityEffect_ForceLight : CompProperties_AbilityEffect
    {
        public float baseDamage = 10f;
        public DamageDef damageDef;
        public float armorPenetration = 0f;
        public HediffDef hediffDef;

        public CompProperties_AbilityEffect_ForceLight()
        {
            compClass = typeof(CompAbilityEffect_ForceLight);
        }
    }
}