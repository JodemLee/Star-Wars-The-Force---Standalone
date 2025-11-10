using RimWorld;
using Verse;

namespace TheForce_Standalone.Telekinesis
{
    public class CompAbilityEffect_ForceThrow : CompAbilityEffect_WithDest
    {
        public float baseDamage = 1f;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (parent.pawn == null || parent.pawn.Map == null) return;

            var projectileDef = DefDatabase<ThingDef>.GetNamed("Force_ThrowItem");
            if (projectileDef == null)
            {
                Log.Error("ForceThrow: Missing projectile def");
                return;
            }

            int damageAmount = projectileDef.projectile?.GetDamageAmount(baseDamage, null, null) ?? 0;

            if (!target.IsValid || target.Thing == null) return;

            switch (target.Thing)
            {
                case Pawn targetPawn:
                    TelekinesisUtility.LaunchThing(targetPawn, dest, dest.Cell, parent.pawn, projectileDef, damageAmount);
                    break;
                case Building building when TelekinesisUtility.TryMinifyBuilding(building, out var minified):
                    TelekinesisUtility.LaunchThing(minified, dest, dest.Cell, parent.pawn, projectileDef, damageAmount);
                    break;
                case Thing thing:
                    TelekinesisUtility.LaunchThing(thing, dest, dest.Cell, parent.pawn, projectileDef, damageAmount);
                    break;
            }
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (parent?.pawn == null || !base.ValidateTarget(target, showMessages))
                return false;

            if (!selectedTarget.IsValid)
            {
                if (target.Thing is Building building && !building.def.Minifiable)
                {
                    if (showMessages)
                        Messages.Message("Force.Throw_NonMinifiableBuilding".Translate(),
                        parent.pawn,
                        MessageTypeDefOf.RejectInput);
                    return false;
                }
            }

            return true;
        }


        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref baseDamage, "baseDamage", 1f);
        }
    }

    public class CompProperties_ForceThrow : CompProperties_EffectWithDest
    {
        public float baseDamage = 1f;

        public CompProperties_ForceThrow()
        {
            compClass = typeof(CompAbilityEffect_ForceThrow);
            destination = AbilityEffectDestination.Selected;
        }
    }
}