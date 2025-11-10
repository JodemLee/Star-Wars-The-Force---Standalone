using RimWorld;
using System.Linq;
using Verse;

namespace TheForce_Standalone
{
    public class CompAbilityEffect_ForcePower : CompAbilityEffect
    {
        protected new CompProperties_AbilityEffect_ForcePower Props =>
            (CompProperties_AbilityEffect_ForcePower)props;

        public override bool ShouldHideGizmo
        {
            get
            {
                try
                {
                    var forceUser = parent.pawn.TryGetComp<CompClass_ForceUser>();
                    if (forceUser != null && parent.def.HasModExtension<ForceAbilityDefExtension>() && forceUser.unlockedAbiliities.Contains(parent.def.ToString()))
                    {
                        bool shouldHide = !forceUser.Abilities.IsAbilityActive(parent.def.defName);
                        return shouldHide;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }


        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (parent.OnCooldown) return false;

            var forceUser = parent.pawn.TryGetComp<CompClass_ForceUser>();
            if (forceUser?.Abilities.OnGlobalCooldown == true) return false;

            return true;
        }

        protected virtual bool TryConsumeResources()
        {
            var forceUser = parent.pawn.TryGetComp<CompClass_ForceUser>();
            if (forceUser == null || !forceUser.IsValidForceUser)
                return false;

            float cost = GetForceCost();
            if (!forceUser.TrySpendFP(cost))
                return false;

            ApplyForceEffects(forceUser);
            return true;
        }

        protected virtual void ApplyForceEffects(CompClass_ForceUser forceUser)
        {
            if (parent.def.statBases == null) return;

            if (StatDef.Named("Force_AbilityForceEXP") is StatDef forceExpStat)
            {
                float exp = parent.def.GetStatValueAbstract(forceExpStat);
                if (exp >= 0) forceUser.Leveling.AddForceExperience(exp);
            }

            if (StatDef.Named("Force_AbilityDarksideStat") is StatDef darkStat)
            {
                forceUser.Alignment.AddDarkSideAttunement(parent.def.GetStatValueAbstract(darkStat));
            }

            if (StatDef.Named("Force_AbilityLightsideStat") is StatDef lightStat)
            {
                forceUser.Alignment.AddLightSideAttunement(parent.def.GetStatValueAbstract(lightStat));
            }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!TryConsumeResources()) return;

            parent.StartCooldown(parent.def.cooldownTicksRange.RandomInRange);
            if (Props.globalCooldownTicks > 0)
            {
                parent.pawn.TryGetComp<CompClass_ForceUser>()?.Abilities.StartGlobalCooldown(Props.globalCooldownTicks);
            }

            base.Apply(target, dest);
        }

        public override bool GizmoDisabled(out string reason)
        {
            if (base.GizmoDisabled(out reason))
                return true;

            var forceUser = parent.pawn.TryGetComp<CompClass_ForceUser>();

            if (forceUser == null || !forceUser.IsValidForceUser)
            {
                reason = "Force.NotAForceUser".Translate();
                return true;
            }

            float cost = GetForceCost();
            if (forceUser.currentFP < cost)
            {
                reason = "Force.NotEnoughForcePoints".Translate(forceUser.currentFP.ToString("F0"), cost.ToString("F0"));
                return true;
            }

            reason = null;
            return false;
        }

        protected virtual float GetForceCost()
        {
            if (parent.def.statBases == null) return 0f;

            if (StatDef.Named("Force_AbilityForcePoolCost") is StatDef forceCostStat)
            {
                return parent.def.GetStatValueAbstract(forceCostStat);
            }
            return 0f;
        }
    }

    public class CompProperties_AbilityEffect_ForcePower : CompProperties_AbilityEffect
    {
        public int globalCooldownTicks = 120;

        public CompProperties_AbilityEffect_ForcePower()
        {
            compClass = typeof(CompAbilityEffect_ForcePower);
        }
    }
}