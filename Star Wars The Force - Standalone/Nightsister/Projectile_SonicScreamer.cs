using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Nightsister
{
    internal class Projectile_SonicScreamer : Projectile
    {
        private static FloatRange BaseStunDurationRange = new FloatRange(1.5f, 3.5f);
        private static FloatRange DeafDurationRange = new FloatRange(6f, 24f);
        private static float ChanceToCauseBleeding = 0.3f;
        private const float MaxHearingFactor = 2.5f;
        private const float MinHearingFactor = 0.4f;

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            IntVec3 position = base.Position;
            Map map = base.Map;
            base.Impact(hitThing, blockedByShield);
            Find.BattleLog.Add(new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef));

            // Affect the direct hit target
            if (hitThing is Pawn hitPawn)
            {
                float hearingFactor = Mathf.Lerp(MinHearingFactor, MaxHearingFactor,
                    hitPawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing));

                // Apply scaled stun effect
                float baseStunDuration = BaseStunDurationRange.RandomInRange;
                float scaledStunDuration = baseStunDuration * hearingFactor;
                hitPawn.stances.stunner.StunFor((int)(scaledStunDuration * 60f), launcher, addBattleLog: true);

                // Apply deafness to ALL hearing sources
                float deafHours = DeafDurationRange.RandomInRange * hearingFactor;
                float deafSeverity = deafHours / 24f;
                foreach (BodyPartRecord earPart in hitPawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.HearingSource))
                {
                    Hediff hediff = HediffMaker.MakeHediff(ForceDefOf.Force_TemporaryHearingLoss, hitPawn, earPart);
                    hediff.Severity = deafSeverity;
                    hitPawn.health.AddHediff(hediff);
                }

                float bleedingChance = ChanceToCauseBleeding * hearingFactor;
                if (Rand.Chance(bleedingChance))
                {
                    foreach (BodyPartRecord earPart in hitPawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.HearingSource))
                    {
                        hitPawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt,
                            1f * hearingFactor,
                            0f, -1f, launcher, earPart));
                    }
                }
            }

            // Area effect for nearby pawns
            foreach (Pawn pawn in GenRadial.RadialDistinctThingsAround(position, map, 3.5f, true).OfType<Pawn>())
            {
                if (pawn != hitThing)
                {
                    float distanceFactor = Mathf.Clamp01(1f - (pawn.Position.DistanceTo(position) / 3.5f));
                    float hearingFactor = Mathf.Lerp(MinHearingFactor, MaxHearingFactor,
                        pawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing));

                    if (Rand.Chance(distanceFactor * 0.7f))
                    {
                        float nearbyBaseStunDuration = BaseStunDurationRange.RandomInRange * distanceFactor * 0.6f;
                        float nearbyScaledStunDuration = nearbyBaseStunDuration * hearingFactor;
                        pawn.stances.stunner.StunFor((int)(nearbyScaledStunDuration * 60f), launcher);
                    }

                    if (Rand.Chance(distanceFactor * 0.5f))
                    {
                        float nearbyDeafHours = DeafDurationRange.RandomInRange * distanceFactor * 0.5f * hearingFactor;
                        float nearbyDeafSeverity = nearbyDeafHours / 24f;
                        foreach (BodyPartRecord earPart in pawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.HearingSource))
                        {
                            Hediff hediff = HediffMaker.MakeHediff(ForceDefOf.Force_TemporaryHearingLoss, pawn, earPart);
                            hediff.Severity = nearbyDeafSeverity;
                            pawn.health.AddHediff(hediff);
                        }
                    }
                }
            }

            // Create sonic boom effect
            MoteMaker.MakeStaticMote(position, map, ThingDefOf.Mote_Stun, 1f);
        }
    }
}
