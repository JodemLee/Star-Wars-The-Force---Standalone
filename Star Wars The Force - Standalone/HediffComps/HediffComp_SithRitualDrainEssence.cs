using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.HediffComps
{
    public class HediffComp_SithRitualDrainEssence : HediffComp
    {
        private const float DrainAmount = 0.005f;
        private Pawn caster;

        public HediffCompProperties_SithRitualDrainEssence Props => props as HediffCompProperties_SithRitualDrainEssence;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref caster, "caster");
        }

        public void Initialize(Pawn casterPawn)
        {
            caster = casterPawn;
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);

            // Remove if caster is gone or victim is dead
            if (caster == null || caster.Destroyed || caster.Dead || Pawn.Dead || !Pawn.Spawned)
            {
                Pawn.health.RemoveHediff(parent);
                return;
            }

            bool allNeedsDrained = ApplyDrainEffect();
            if (allNeedsDrained)
            {
                Pawn.health.RemoveHediff(parent);
                return;
            }
        }

        private bool ApplyDrainEffect()
        {
            bool allNeedsDrained = true;
            bool anyNeedDrained = false;

            // Drain all needs from victim and transfer to caster
            if (Pawn.needs != null && caster.needs != null)
            {
                foreach (var victimNeed in Pawn.needs.AllNeeds)
                {
                    if (victimNeed == null || victimNeed.CurLevel <= 0f)
                        continue;

                    // If we found at least one need that still has value, not all are drained
                    if (victimNeed.CurLevel > 0f)
                    {
                        allNeedsDrained = false;
                    }

                    var casterNeed = caster.needs.TryGetNeed(victimNeed.def);
                    if (casterNeed != null)
                    {
                        // Drain from victim
                        float drainedAmount = Mathf.Min(victimNeed.CurLevel, DrainAmount);
                        victimNeed.CurLevel = Mathf.Max(0, victimNeed.CurLevel - drainedAmount);
                        casterNeed.CurLevel = Mathf.Min(casterNeed.MaxLevel, casterNeed.CurLevel + (drainedAmount / 2f));
                        anyNeedDrained = true;
                    }
                    else
                    {
                        victimNeed.CurLevel = Mathf.Max(0, victimNeed.CurLevel - DrainAmount);
                        anyNeedDrained = true;
                    }
                }
            }

            if (anyNeedDrained && caster.TryGetComp<CompClass_ForceUser>(out var forceUser))
            {
                forceUser.RecoverFP(DrainAmount * 10f);
                forceUser.Leveling.AddForceExperience(DrainAmount);
            }

            return allNeedsDrained;
        }

        public override string CompDebugString()
        {
            return $"Caster: {caster?.LabelShort ?? "None"}";
        }
    }

    public class HediffCompProperties_SithRitualDrainEssence : HediffCompProperties
    {
        public HediffCompProperties_SithRitualDrainEssence()
        {
            compClass = typeof(HediffComp_SithRitualDrainEssence);
        }
    }
}