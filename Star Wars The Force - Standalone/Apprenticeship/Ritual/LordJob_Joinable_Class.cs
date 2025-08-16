using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheForce_Standalone.Apprenticeship.Ritual
{
    internal class LordJob_Joinable_Class : LordJob_Ritual
    {
        protected override int MinTicksToFinish => base.DurationTicks / 2;
        public override bool AllowStartNewGatherings => false;
        public override bool OrganizerIsStartingPawn => true;

        public LordJob_Joinable_Class() { }

        public LordJob_Joinable_Class(TargetInfo spot, Pawn organizer, Precept_Ritual ritual, List<RitualStage> stages, RitualRoleAssignments assignments)
            : base(spot, ritual, null, stages, assignments, organizer)
        {
            selectedTarget = new TargetInfo(organizer);
        }

        protected override LordToil_Ritual MakeToil(RitualStage stage)
        {
            if (stage == null) return new LordToil_Speech(spot, ritual, this, organizer);
            return new LordToil_Ritual(spot, this, stage, organizer);
        }

        public override string GetReport(Pawn pawn)
        {
            return ((pawn != organizer)
                ? "Force.Training_ListeningReport".Translate(organizer.Named("ORGANIZER"))
                : "Force.Training_GivingReport".Translate()) + base.TimeLeftPostfix;
        }

        public override void ApplyOutcome(float progress, bool showFinishedMessage = true, bool showFailedMessage = true, bool cancelled = false)
        {
            if (ticksPassed < MinTicksToFinish || cancelled)
            {
                Find.LetterStack.ReceiveLetter(
                    "Force.Training_CancelledTitle".Translate(),
                    "Force.Training_CancelledText".Translate(organizer.LabelShort).CapitalizeFirst(),
                    LetterDefOf.NegativeEvent,
                    organizer);
                ritual.outcomeEffect?.ResetCompDatas();
            }
            else
            {
                base.ApplyOutcome(progress, showFinishedMessage: false);
            }
        }
    }

    public class JobGiver_GiveJediTrainingSpeech : JobGiver_GiveSpeechFacingTarget
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Job job = base.TryGiveJob(pawn);
            if (job != null) return job;

            LordJob_Joinable_Class lordJob = pawn.GetLord()?.LordJob as LordJob_Joinable_Class;
            if (lordJob == null) return null;

            IntVec3 position = pawn.Position;
            if (!pawn.CanReserve(position))
            {
                CellFinder.TryRandomClosewalkCellNear(position, pawn.Map, 2, out position,
                    (IntVec3 c) => pawn.CanReserveAndReach(c, PathEndMode.OnCell, pawn.NormalMaxDanger()));
            }

            IntVec3 faceCell = lordJob.selectedTarget.Cell;
            if (lordJob.selectedTarget.IsValid) faceCell = lordJob.selectedTarget.Cell;

            job = JobMaker.MakeJob(JobDefOf.GiveSpeech, position, faceCell);
            job.showSpeechBubbles = showSpeechBubbles;

            if (lordJob.lord.CurLordToil is LordToil_Ritual lordToilRitual)
            {
                var role = lordJob.RoleFor(pawn);
                if (role != null) job.interaction = lordToilRitual.stage.BehaviorForRole(role.id)?.speakerInteraction;
            }

            job.speechSoundMale = soundDefMale ?? SoundDefOf.Speech_Leader_Male;
            job.speechSoundFemale = soundDefFemale ?? SoundDefOf.Speech_Leader_Female;
            job.speechFaceSpectatorsIfPossible = faceSpectatorsIfPossible;

            return job;
        }

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            return (JobGiver_GiveJediTrainingSpeech)base.DeepCopy(resolve);
        }
    }
}