using RimWorld;
using Verse;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    internal class CompAbilityEffect_SithLightningStrike : CompAbilityEffect
    {
        public new CompProperties_SithLightningStrike Props => (CompProperties_SithLightningStrike)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (!target.IsValid || target.Cell.Fogged(parent.pawn.Map))
                return;



            Map map = parent.pawn.Map;
            IntVec3 targetCell = target.Cell;

            map.weatherManager.eventHandler.AddEvent(new SithLightningEvent(map, targetCell, UnityEngine.Color.red));

            float damage = Props.baseDamage * Props.damageMultiplier;
            if (Props.damageStat != null)
            {
                damage *= parent.pawn.GetStatValue(Props.damageStat);
            }

            SoundDef explosionSound = SoundDefOf.Thunder_OnMap ?? SoundDefOf.FlashstormAmbience;
            GenExplosion.DoExplosion(
                targetCell,
                map,
                GetRadiusForPawn(),
                ForceDefOf.Force_Lightning,
                parent.pawn,
                explosionSound: explosionSound);

        }

        private float GetRadiusForPawn()
        {
            return Props.radius;
        }
    }

    public class CompProperties_SithLightningStrike : CompProperties_AbilityEffect
    {
        public float baseDamage = 30f;
        public float damageMultiplier = 1f;
        public StatDef damageStat;
        public float radius;

        public CompProperties_SithLightningStrike()
        {
            compClass = typeof(CompAbilityEffect_SithLightningStrike);
        }
    }
}

