using RimWorld;
using UnityEngine.UIElements;
using Verse;

namespace TheForce_Standalone.Nightsister
{
    internal class CompAbilityEffect_ChantofResurrection : CompAbilityEffect_Resurrect
    {
        public new CompProperties_ChantofResurrection Props => (CompProperties_ChantofResurrection)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            var effecter = new Effecter(ForceDefOf.Force_Magick_Entry);
            effecter.Trigger(new TargetInfo(target.Cell, target.Pawn.Map, true), TargetInfo.Invalid);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.HasThing && target.Thing is Corpse corpse && corpse.GetRotStage() == RotStage.Dessicated)
            {
                if (throwMessages)
                {
                    Messages.Message("MessageCannotResurrectDessicatedCorpse".Translate(), corpse, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }
    }

    public class CompProperties_ChantofResurrection : CompProperties_AbilityEffect
    {
        public CompProperties_ChantofResurrection()
        {
            compClass = typeof(CompAbilityEffect_ChantofResurrection);
        }
    }

    public class SoulIchor : Skyfaller
    {
        public Corpse target;

        protected override void Impact()
        {
            var pawn = target.InnerPawn;
            var hediffs = pawn.health.hediffSet.hediffs;

            if (ResurrectionUtility.TryResurrectWithSideEffects(pawn))
            {
                if (!pawn.Spawned) GenSpawn.Spawn(pawn, Position, MapHeld);
                var effecter = new Effecter(ForceDefOf.Force_Magick_Entry);
                effecter.Trigger(new TargetInfo(target.Position, target.Map, true), TargetInfo.Invalid);
                Messages.Message("MessagePawnResurrected".Translate(pawn), pawn, MessageTypeDefOf.PositiveEvent);
            }
            Destroy();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref target, "target");
        }
    }
}
