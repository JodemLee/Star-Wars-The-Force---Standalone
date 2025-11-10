using RimWorld;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.HediffComps
{
    public class HediffComp_LinkWithEffect : HediffComp_Link
    {
        private Hediff addedHediff;
        public new HediffCompProperties_LinkWithEffect Props => (HediffCompProperties_LinkWithEffect)props;
        public new bool drawConnection = true;
        private MoteDualAttached mote;

        public override bool CompShouldRemove
        {
            get
            {
                if (parent?.pawn == null || other == null)
                    return true;

                if (!parent.pawn.SpawnedOrAnyParentSpawned ||
                    !other.SpawnedOrAnyParentSpawned ||
                    (Props?.maxDistance > 0f && !parent.pawn.PositionHeld.InHorDistOf(other.PositionHeld, Props.maxDistance)))
                {
                    RemoveAddedHediff();
                    return true;
                }

                return false;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            try
            {
                if (parent?.pawn == null || other == null)
                {
                    RemoveAddedHediff();
                    return;
                }

                base.CompPostTick(ref severityAdjustment);

                if (other is Pawn otherPawn && otherPawn != null && otherPawn.health != null)
                {
                    if (ShouldApplyHediff())
                    {
                        ApplyHediffIfNeeded(otherPawn);
                    }
                }

                if (Props?.customMote != null &&
                    other.MapHeld != null &&
                    parent.pawn.MapHeld != null &&
                    other.MapHeld == parent.pawn.MapHeld)
                {
                    ThingDef moteDef = Props.customMote;
                    if (mote == null)
                    {
                        mote = MoteMaker.MakeInteractionOverlay(moteDef, parent.pawn, other);
                    }
                    mote?.Maintain();
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Exception in HediffComp_LinkWithEffect.CompPostTick: {ex}");
                RemoveAddedHediff();
            }
        }

        private bool ShouldApplyHediff()
        {
            return other != null &&
                   parent != null &&
                   parent.pawn != null &&
                   !parent.pawn.Dead &&
                   !parent.pawn.Destroyed &&
                   Props?.hediffToApply != null;
        }

        private void ApplyHediffIfNeeded(Pawn targetPawn)
        {
            // Apply to the CASTER (other pawn), not the ghost (parent.pawn)
            if (other == null || targetPawn.health == null || Props?.hediffToApply == null)
                return;

            try
            {
                var existingHediff = targetPawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffToApply);

                if (existingHediff == null)
                {
                    addedHediff = targetPawn.health.AddHediff(Props.hediffToApply);
                    if (addedHediff != null)
                    {
                        addedHediff.Severity = Props.severityOffset;
                    }
                }
                else
                {
                    existingHediff.Severity += Props.severityOffset;
                    addedHediff = existingHediff;
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Failed to apply hediff to caster {other}: {ex}");
                RemoveAddedHediff();
            }
        }

        private void RemoveAddedHediff()
        {
            try
            {
                // When link breaks, we should DECREASE severity, not remove entirely
                if (addedHediff != null && addedHediff.pawn != null && addedHediff.pawn.health != null)
                {
                    addedHediff.Severity = Mathf.Max(0, addedHediff.Severity - Props.severityOffset);

                    // Only remove if severity reaches 0
                    if (addedHediff.Severity <= 0)
                    {
                        addedHediff.pawn.health.RemoveHediff(addedHediff);
                    }
                }
                addedHediff = null;
            }
            catch (System.Exception ex)
            {
                Log.Error($"Failed to remove added hediff: {ex}");
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref addedHediff, "addedHediff");
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            RemoveAddedHediff();
        }
    }

    public class HediffCompProperties_LinkWithEffect : HediffCompProperties_Link
    {
        public HediffDef hediffToApply;
        public float severityFactor = 1f;
        public float severityOffset = 0f;

        public HediffCompProperties_LinkWithEffect()
        {
            compClass = typeof(HediffComp_LinkWithEffect);
        }
    }
}