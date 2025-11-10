using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Darkside.Mechu_Deru
{
    internal class CompAbilityEffect_MechuTek : CompAbilityEffect
    {

        private float GetSurgicalDamage() => 10f; // Make configurable
        private float GetArmorPenetration() => 999f; // Make configurable


        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            try
            {
                if (target.Thing is Pawn targetPawn && targetPawn.Spawned)
                {
                    RemoveImplantsUntilSafe(targetPawn);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"MechuTek ability failed on {target.Thing?.Label}: {ex}");
            }
        }

        private bool ShouldContinueRemoving(Pawn targetPawn)
        {
            return !targetPawn.health.ShouldBeDead() &&
                   targetPawn.Spawned &&
                   !targetPawn.Downed;
        }

        private void RemoveImplantsUntilSafe(Pawn targetPawn)
        {
            var hediffSet = targetPawn.health.hediffSet;
            var implantHediffs = hediffSet.hediffs
                .Where(hediff => IsImplantOrProsthetic(hediff))
                .ToList();

            if (!implantHediffs.Any()) return;

            // Process in random order but cache the list
            var randomOrder = implantHediffs.OrderBy(x => Rand.Value).ToList();

            foreach (var hediff in randomOrder)
            {
                if (targetPawn.health.ShouldBeDead()) break;

                RemoveSingleImplant(targetPawn, hediff);

                // Check if we should continue after each removal
                if (!ShouldContinueRemoving(targetPawn)) break;
            }
        }

        private void RemoveSingleImplant(Pawn targetPawn, Hediff hediff)
        {
            var implantDef = hediff.def.spawnThingOnRemoved;
            var statMultiplier = parent.pawn.GetStatValue(ForceDefOf.Force_Darkside_Attunement);

            // Try to drop item first
            if (implantDef != null && ShouldDropImplant(implantDef, statMultiplier))
            {
                var implantItem = ThingMaker.MakeThing(implantDef);
                GenPlace.TryPlaceThing(implantItem, targetPawn.Position, targetPawn.Map, ThingPlaceMode.Near);
            }

            // Remove hediff and apply damage
            var bodyPart = hediff.Part;
            if (bodyPart != null)
            {
                targetPawn.health.RemoveHediff(hediff);

                // Configurable damage
                var damageInfo = new DamageInfo(
                    DamageDefOf.SurgicalCut,
                    GetSurgicalDamage(),
                    GetArmorPenetration(),
                    -1f, null, bodyPart);
                targetPawn.TakeDamage(damageInfo);
            }
        }

        private bool IsImplantOrProsthetic(Hediff hediff)
        {
            var implantDef = hediff.def;
            return implantDef != null && implantDef.countsAsAddedPartOrImplant;
        }

        private float GetDropChance(TechLevel techLevel, float statMultiplier)
        {
            float baseChance = techLevel switch
            {
                TechLevel.Undefined or TechLevel.Animal => 0.75f,
                TechLevel.Neolithic => 0.60f,
                TechLevel.Medieval => 0.45f,
                TechLevel.Industrial => 0.30f,
                TechLevel.Spacer => 0.15f,
                TechLevel.Ultra => 0.05f,
                _ => 0.50f
            };

            // Apply diminishing returns for very high multipliers
            float effectiveMultiplier = Mathf.Min(statMultiplier, 3f);
            return Mathf.Clamp01(baseChance * effectiveMultiplier);
        }

        private bool ShouldDropImplant(ThingDef implantDef, float statMultiplier)
        {
            TechLevel implantTechLevel = implantDef.techLevel;
            float dropChance = GetDropChance(implantTechLevel, statMultiplier);
            return Rand.Value <= dropChance;
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
                                    parent.pawn, MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            // Check if target is immune (mechanoids, etc.)
            if (IsTargetImmune(targetPawn))
            {
                if (throwMessages)
                {
                    Messages.Message("Force.MechuTek_ImmuneTarget".Translate(targetPawn.LabelShort),
                                    parent.pawn, MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            return HasValidImplants(targetPawn, throwMessages);
        }

        private bool IsTargetImmune(Pawn pawn) =>
            pawn.RaceProps.IsMechanoid;

        private bool HasValidImplants(Pawn targetPawn, bool throwMessages)
        {
            bool hasImplants = targetPawn.health.hediffSet.hediffs
                .Any(hediff => IsImplantOrProsthetic(hediff) && !IsImplantImmune(hediff));

            if (!hasImplants && throwMessages)
            {
                Messages.Message("Force.MechuTek_NoImplants".Translate(targetPawn.LabelShort),
                                parent.pawn, MessageTypeDefOf.RejectInput);
            }

            return hasImplants;
        }

        private bool IsImplantImmune(Hediff hediff)
        {
            return false;
        }
    }
}