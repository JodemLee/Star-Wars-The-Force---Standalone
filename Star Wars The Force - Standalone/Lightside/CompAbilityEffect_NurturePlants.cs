using RimWorld;
using RimWorld.Planet;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Lightside
{
    internal class CompAbilityEffect_NurturePlants : CompAbilityEffect_ForcePower
    {
        public new CompProperties_AbilityWithStat Props => (CompProperties_AbilityWithStat)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target.Thing);
            var scaledStat = parent.pawn.GetStatValue(Props.scalingStat);
            foreach (var plant in GenRadial.RadialDistinctThingsAround(target.Cell, target.Thing.Map, parent.def.EffectRadius, true)
                        .OfType<Plant>()
                        .Distinct())
            {
                plant.Growth += plant.GrowthRate * scaledStat / plant.def.plant.growDays;
                plant.DirtyMapMesh(plant.Map);
            }
        }

        public override bool CanApplyOn(GlobalTargetInfo target)
        {
            return base.CanApplyOn(target);
        }

    }
}
