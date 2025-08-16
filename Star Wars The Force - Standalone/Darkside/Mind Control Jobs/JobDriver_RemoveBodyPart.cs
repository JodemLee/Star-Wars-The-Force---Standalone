using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TheForce_Standalone.Darkside.Mind_Control_Jobs
{
    public abstract class JobDriver_RemoveBodyPart : JobDriver
    {
        protected abstract BodyPartDef TargetBodyPart { get; }
        protected abstract DamageDef DamageType { get; }
        protected virtual SoundDef SoundEffect => SoundDefOf.Execute_Cut;
        protected virtual HistoryEventDef HistoryEvent => null;
        protected virtual int InitialWaitTicks => 35;
        protected virtual int FinalWaitTicks => 120;
        protected virtual int BloodSplatterCount => 3;
        protected virtual bool ShouldCreateHistoryEvent => true;

        protected virtual void PreRemovalAction(Pawn pawn, BodyPartRecord part) { }
        protected virtual void PostRemovalAction(Pawn pawn, BodyPartRecord part) { }

        protected void RemoveBodyPart(Pawn pawn)
        {
            var parts = pawn.health.hediffSet.GetNotMissingParts()
                .Where(p => p.def == TargetBodyPart)
                .ToList();

            if (parts.Count == 0) return;

            var partToRemove = parts.First();

            PreRemovalAction(pawn, partToRemove);

            pawn.TakeDamage(new DamageInfo(
                DamageType,
                99999f,
                999f,
                -1f,
                null,
                partToRemove));

            PostRemovalAction(pawn, partToRemove);

            if (ShouldCreateHistoryEvent && HistoryEvent != null)
            {
                try
                {
                    Find.HistoryEventsManager?.RecordEvent(
                        new HistoryEvent(HistoryEvent, pawn.Named(HistoryEventArgsNames.Doer)));
                }
                catch (Exception ex)
                {
                    Log.Warning($"Failed to record history event: {ex}");
                }
            }

            if (SoundEffect != null)
            {
                SoundEffect.PlayOneShot(pawn);
            }

            CreateBloodEffects(pawn);
        }

        protected void CreateBloodEffects(Pawn pawn)
        {
            if (pawn.RaceProps?.BloodDef == null || !pawn.PositionHeld.IsValid || pawn.MapHeld == null)
                return;

            CellRect cellRect = new CellRect(pawn.PositionHeld.x - 1, pawn.PositionHeld.z - 1, 3, 3);
            for (int i = 0; i < BloodSplatterCount; i++)
            {
                IntVec3 randomCell = cellRect.RandomCell;
                if (randomCell.InBounds(pawn.MapHeld) && GenSight.LineOfSight(randomCell, pawn.PositionHeld, pawn.MapHeld))
                {
                    FilthMaker.TryMakeFilth(randomCell, pawn.MapHeld, pawn.RaceProps.BloodDef, pawn.LabelIndefinite());
                }
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_General.Wait(InitialWaitTicks);

            Toil removalToil = ToilMaker.MakeToil("RemoveBodyPart");
            removalToil.initAction = () => RemoveBodyPart(pawn);
            removalToil.defaultCompleteMode = ToilCompleteMode.Instant;

            yield return Toils_General.Wait(FinalWaitTicks);
            yield return removalToil;
        }
    }
}
