using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Apprenticeship.Ritual
{
    internal class RitualOutcomeEffectWorker_Lecture : RitualOutcomeEffectWorker_FromQuality
    {
        public RitualOutcomeEffectWorker_Lecture()
        {
        }

        public RitualOutcomeEffectWorker_Lecture(RitualOutcomeEffectDef def)
            : base(def)
        {
        }

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            float quality = GetQuality(jobRitual, progress);
            RitualOutcomePossibility outcome = GetOutcome(quality, jobRitual);

            // Get our custom extension data
            var extension = def.GetModExtension<RitualOutcomeEffectDefExtension_Lecture>();
            if (extension == null)
            {
                Log.Error("RitualOutcomeEffectWorker_Lecture requires RitualOutcomeEffectDefExtension_Lecture mod extension");
                return;
            }

            foreach (var pair in totalPresence)
            {
                Pawn attendee = pair.Key;
                if (attendee == jobRitual.Organizer) continue;

                Hediff hediff = HediffMaker.MakeHediff(extension.hediffDef, attendee);
                float severity = extension.baseSeverity + (quality * extension.severityPerQuality);
                if (extension.maxSeverity > 0)
                {
                    severity = Mathf.Min(severity, extension.maxSeverity);
                }
                hediff.Severity = severity;
                attendee.health.AddHediff(hediff);

                var forceComp = attendee.TryGetComp<CompClass_ForceUser>();
                if (forceComp != null && forceComp.IsValidForceUser)
                {
                    float xpGain = extension.baseXP + (quality * extension.xpPerQuality);
                    if (extension.maxXP > 0)
                    {
                        xpGain = Mathf.Min(xpGain, extension.maxXP);
                    }
                    forceComp.Leveling.AddForceExperience(xpGain);
                }
            }

            var organizerComp = jobRitual.Organizer.TryGetComp<CompClass_ForceUser>();
            if (organizerComp != null && organizerComp.IsValidForceUser)
            {

                float teacherXPGain = extension.baseTeacherXP +
                                      (totalPresence.Count * extension.xpPerStudent) +
                                      (quality * extension.xpPerQualityTeacher);

                if (extension.maxTeacherXP > 0)
                {
                    teacherXPGain = Mathf.Min(teacherXPGain, extension.maxTeacherXP);
                }
                organizerComp.Leveling.AddForceExperience(teacherXPGain);
            }

            ApplyOutcomeInMemory(jobRitual, progress, totalPresence, outcome);
            SendOutcomeLetters(jobRitual, outcome, quality, progress, totalPresence);
        }

        private void ApplyOutcomeInMemory(LordJob_Ritual jobRitual, float progress, Dictionary<Pawn, int> totalPresence, RitualOutcomePossibility outcome)
        {
            foreach (var pair in totalPresence)
            {
                Pawn pawn = pair.Key;
                if (pawn != jobRitual.Organizer)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(outcome.memory);
                }
            }
        }

        private void SendOutcomeLetters(LordJob_Ritual jobRitual, RitualOutcomePossibility outcome, float quality, float progress, Dictionary<Pawn, int> totalPresence)
        {
            string letterText = OutcomeDesc(outcome, quality, progress, jobRitual, totalPresence.Count);
            Find.LetterStack.ReceiveLetter(
                "Force_OutcomeLetterLabel".Translate(outcome.label.Named("OUTCOMELABEL")),
                letterText,
                outcome.Positive ? LetterDefOf.RitualOutcomePositive : LetterDefOf.RitualOutcomeNegative,
                new LookTargets(jobRitual.Organizer)
            );
        }

        private string OutcomeDesc(RitualOutcomePossibility outcome, float quality, float progress, LordJob_Ritual jobRitual, int totalPresence)
        {
            TaggedString text = "Force_LectureOutcomeQualitySpecific".Translate(quality.ToStringPercent()) + ":\n";

            if (def.startingQuality > 0f)
            {
                text += "\n  - " + "StartingRitualQuality".Translate(def.startingQuality.ToStringPercent()) + ".";
            }

            foreach (RitualOutcomeComp comp in def.comps)
            {
                if (comp is RitualOutcomeComp_Quality && comp.Applies(jobRitual) && Mathf.Abs(comp.QualityOffset(jobRitual, DataForComp(comp))) >= float.Epsilon)
                {
                    text += "\n  - " + comp.GetDesc(jobRitual, DataForComp(comp)).CapitalizeFirst();
                }
            }

            if (progress < 1f)
            {
                text += "\n  - " + "RitualOutcomeProgress".Translate("Lecture".Translate()) + ": x" + Mathf.Lerp(RitualOutcomeEffectWorker_FromQuality.ProgressToQualityMapping.min, RitualOutcomeEffectWorker_FromQuality.ProgressToQualityMapping.max, progress).ToStringPercent();
            }

            var extension = def.GetModExtension<RitualOutcomeEffectDefExtension_Lecture>();
            if (extension != null)
            {
                text += "\n\n" + "Force_LectureOutcomeHediffApplied".Translate(
                    extension.hediffDef.LabelCap,
                    (extension.baseSeverity + (quality * extension.severityPerQuality)).ToStringPercent()
                );

                float studentXPGain = extension.baseXP + (quality * extension.xpPerQuality);
                if (extension.maxXP > 0)
                {
                    studentXPGain = Mathf.Min(studentXPGain, extension.maxXP);
                }
                text += "\n" + "Force_LectureOutcomeXPGainedStudents".Translate(studentXPGain.ToString("F1"), totalPresence - 1);

                // Teacher info
                var organizerComp = jobRitual.Organizer.TryGetComp<CompClass_ForceUser>();
                if (organizerComp != null && organizerComp.IsValidForceUser)
                {
                    float teacherXPGain = extension.baseTeacherXP +
                                        ((totalPresence - 1) * extension.xpPerStudent) +
                                        (quality * extension.xpPerQualityTeacher);
                    if (extension.maxTeacherXP > 0)
                    {
                        teacherXPGain = Mathf.Min(teacherXPGain, extension.maxTeacherXP);
                    }
                    text += "\n" + "Force_LectureOutcomeXPGainedTeacher".Translate(teacherXPGain.ToString("F1"));
                }
            }

            return text;
        }
    }

    public class RitualOutcomeEffectDefExtension_Lecture : DefModExtension
    {
        // Student fields
        public HediffDef hediffDef;
        public float baseSeverity = 0.1f;
        public float severityPerQuality = 0.5f;
        public float maxSeverity = 1f;

        // Student XP fields
        public float baseXP = 10f;
        public float xpPerQuality = 20f;
        public float maxXP = 100f;

        // Teacher XP fields
        public float baseTeacherXP = 15f;
        public float xpPerStudent = 5f;
        public float xpPerQualityTeacher = 25f;
        public float maxTeacherXP = 150f;
    }
}