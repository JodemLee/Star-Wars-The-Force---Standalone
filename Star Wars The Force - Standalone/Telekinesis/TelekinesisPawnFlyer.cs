using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Telekinesis
{
    internal class TelekinesisPawnFlyer : PawnFlyer
    {

        private static readonly AccessTools.FieldRef<PawnFlyer, IntVec3> DestCellField
            = AccessTools.FieldRefAccess<IntVec3>(typeof(PawnFlyer), "destCell");
        private static readonly AccessTools.FieldRef<PawnFlyer, Vector3> EffectivePosField
            = AccessTools.FieldRefAccess<Vector3>(typeof(PawnFlyer), "effectivePos");
        private static readonly AccessTools.FieldRef<PawnFlyer, Vector3> GroundPosField
            = AccessTools.FieldRefAccess<Vector3>(typeof(PawnFlyer), "groundPos");
        private static readonly AccessTools.FieldRef<PawnFlyer, float> EffectiveHeightField
            = AccessTools.FieldRefAccess<float>(typeof(PawnFlyer), "effectiveHeight");

        public Ability ability;
        public bool selectOnSpawn = false;

        public ref Vector3 EffectivePos => ref EffectivePosField(this);
        public ref Vector3 GroundPos => ref GroundPosField(this);
        public ref float EffectiveHeight => ref EffectiveHeightField(this);
        public bool hitWallDuringFlight;
        public IntVec3 impactPosition;
        private float flightDistance;
        private IntVec3 launchPosition;
        public IntVec3 casterPos;
        public IntVec3 targetPos;
        private Vector3 groundPos;
        private int positionLastComputedTick = -1;
        private Vector3 effectivePos;
        private float effectiveHeight;
        public IntVec3 originalDestination;
        public ref IntVec3 DestinationCell => ref DestCellField(this);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            if (!respawningAfterLoad && FlyingPawn != null)
            {
                launchPosition = FlyingPawn.Position;
                flightDistance = launchPosition.DistanceTo(DestinationCell);
            }
            base.SpawnSetup(map, respawningAfterLoad);
        }

        private void RecomputePosition()
        {
            if (positionLastComputedTick != ticksFlying)
            {
                positionLastComputedTick = ticksFlying;
                float t = (float)ticksFlying / (float)ticksFlightTime;
                float t2 = def.pawnFlyer.Worker.AdjustedProgress(t);
                effectiveHeight = def.pawnFlyer.Worker.GetHeight(t2);
                groundPos = Vector3.Lerp(startVec, DestinationPos, t2);
                Vector3 vector = Altitudes.AltIncVect * effectiveHeight;
                Vector3 vector2 = Vector3.forward * (def.pawnFlyer.heightFactor * effectiveHeight);
                effectivePos = groundPos + vector + vector2;
                base.Position = groundPos.ToIntVec3();
            }
        }

        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            RecomputePosition();

            if (FlyingPawn != null && !FlyingPawn.Destroyed)
            {
                FlyingPawn.Drawer?.renderer?.DynamicDrawPhaseAt(phase, drawLoc, Rotation, true);
                FlyingPawn.DynamicDrawPhaseAt(phase, effectivePos);
            }
            else if (FlyingThing != null && !FlyingThing.Destroyed)
            {
                FlyingThing.DynamicDrawPhaseAt(phase, effectivePos);
            }
        }

        protected override void RespawnPawn()
        {
            Pawn pawn = this.FlyingPawn;
            if (pawn == null) return;

            if (hitWallDuringFlight)
            {
                ApplyWallImpactEffects(pawn);
            }

            base.RespawnPawn();
            ability?.CanApplyOn(new LocalTargetInfo(pawn));
        }

        private void ApplyWallImpactEffects(Pawn pawn)
        {
            // Null check the pawn and critical components
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null || pawn.Destroyed)
            {
                Log.Warning("[The Force] Attempted to apply wall impact effects to null or invalid pawn");
                return;
            }

            try
            {
                // Calculate damage with fallback values if positions are invalid
                float baseDamage = 10f;
                if (launchPosition.IsValid && impactPosition.IsValid)
                {
                    baseDamage = TelekinesisUtility.CalculateDistanceBasedDamage(10f, launchPosition, impactPosition);
                }

                float massFactor = TelekinesisUtility.CalculateKineticDamage(1f, pawn);
                float totalDamage = Mathf.Clamp(baseDamage * massFactor, 1f, 25f); // Clamp to reasonable values

                // Get random body part or fallback to whole body
                BodyPartRecord hitPart = pawn.health.hediffSet.GetRandomNotMissingPart(
                    DamageDefOf.Blunt,
                    BodyPartHeight.Undefined,
                    BodyPartDepth.Outside
                ) ?? pawn.RaceProps.body.corePart;

                DamageInfo damageInfo = new DamageInfo(
                    DamageDefOf.Blunt,
                    totalDamage,
                    armorPenetration: 5f,
                    instigator: ability?.pawn,
                    hitPart: hitPart
                );

                pawn.TakeDamage(damageInfo);

                // Apply stagger if damage is significant and pawn can be staggered
                if (totalDamage > 15f && pawn.stances != null)
                {
                    int staggerTicks = Mathf.RoundToInt(totalDamage * 1.5f);
                    pawn.stances.stagger.StaggerFor(staggerTicks);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[The Force] Error in ApplyWallImpactEffects: {ex}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref hitWallDuringFlight, "hitWallDuringFlight");
            Scribe_Values.Look(ref impactPosition, "impactPosition");
            Scribe_Values.Look(ref flightDistance, "flightDistance");
            Scribe_Values.Look(ref launchPosition, "launchPosition");
            Scribe_Values.Look(ref casterPos, "casterPos");
            Scribe_Values.Look(ref targetPos, "targetPos");
        }
    }
}