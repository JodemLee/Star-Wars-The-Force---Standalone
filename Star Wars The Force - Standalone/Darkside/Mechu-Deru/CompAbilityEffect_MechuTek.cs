using RimWorld;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Darkside.Mechu_Deru
{
    internal class CompAbilityEffect_MechuTek : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (target.Thing is Pawn targetPawn)
            {
                RemoveImplantsUntilSafe(targetPawn);
            }
        }

        private void RemoveImplantsUntilSafe(Pawn targetPawn)
        {
            var implantHediffs = targetPawn.health.hediffSet.hediffs
                .Where(hediff => IsImplantOrProsthetic(hediff))
                .ToList();
            if (!implantHediffs.Any()) return;

            while (implantHediffs.Any())
            {
                var randomHediff = implantHediffs.RandomElement();
                var implantDef = randomHediff.def.spawnThingOnRemoved;
                var statMultiplier = parent.pawn.GetStatValue(ForceDefOf.Force_Darkside_Attunement);

                if (implantDef != null)
                {
                    TechLevel implantTechLevel = implantDef.techLevel;
                    float dropChance = GetDropChance(implantTechLevel);
                    if (Rand.Value <= dropChance * statMultiplier)
                    {
                        var implantItem = ThingMaker.MakeThing(implantDef);
                        GenPlace.TryPlaceThing(implantItem, targetPawn.Position, targetPawn.Map, ThingPlaceMode.Near);
                    }
                }
                var bodyPart = randomHediff.Part;
                if (bodyPart != null)
                {
                    targetPawn.health.RemoveHediff(randomHediff);
                    targetPawn.TakeDamage(new DamageInfo(DamageDefOf.SurgicalCut, 10f, 999f, -1f, null, bodyPart));
                    if (targetPawn.health.ShouldBeDead())
                    {
                        break;
                    }
                }
                implantHediffs = targetPawn.health.hediffSet.hediffs
                    .Where(hediff => IsImplantOrProsthetic(hediff))
                    .ToList();
            }
        }

        private bool IsImplantOrProsthetic(Hediff hediff)
        {
            var implantDef = hediff.def;
            return implantDef != null && implantDef.countsAsAddedPartOrImplant;
        }

        private float GetDropChance(TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.Undefined:
                case TechLevel.Animal:
                    return 0.75f;
                case TechLevel.Neolithic:
                    return 0.60f;
                case TechLevel.Medieval:
                    return 0.45f;
                case TechLevel.Industrial:
                    return 0.30f;
                case TechLevel.Spacer:
                    return 0.15f;
                case TechLevel.Ultra:
                    return 0.05f;
                default:
                    return 0.50f;
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
                return false;

            if (target.Thing is not Pawn targetPawn)
            {
                if (throwMessages)
                {
                    Messages.Message("Force.MechuTek_InvalidTarget".Translate(),
                                    parent.pawn,
                                    MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            bool hasImplants = targetPawn.health.hediffSet.hediffs.Any(hediff => IsImplantOrProsthetic(hediff));
            if (!hasImplants && throwMessages)
            {
                Messages.Message("Force.MechuTek_NoImplants".Translate(targetPawn.LabelShort),
                                parent.pawn,
                                MessageTypeDefOf.RejectInput);
            }

            return hasImplants;
        }
    }
}