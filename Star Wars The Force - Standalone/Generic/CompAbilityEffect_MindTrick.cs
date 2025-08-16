using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TheForce_Standalone.Generic
{
    public class CompAbilityEffect_MindTrick : CompAbilityEffect_WithDest
    {
        public new CompProperties_MindTrick Props => (CompProperties_MindTrick)props;

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            base.AICanTargetNow(target);
            bool foundTarget = false;
            foreach (Pawn pawn in parent.GetAffectedTargets(target).Select(v => (Pawn)v ?? null))
            {
                if (pawn.Faction.AllyOrNeutralTo(parent.pawn.Faction))
                    return false;

                bool hasMindTrickJob = false;
                foreach (var mindJob in Props.availableJobs)
                {
                    if (pawn.CurJob?.def == mindJob.jobDef)
                    {
                        hasMindTrickJob = true;
                        break;
                    }
                }

                if (!hasMindTrickJob)
                    foundTarget = true;
            }
            return foundTarget;
        }

        public void AssignJob(MindControlJob mindControlJob, Pawn targetPawn)
        {
            if (targetPawn == null || mindControlJob?.jobDef == null)
                return;

            LocalTargetInfo destination = GetDestinationForJob(mindControlJob, targetPawn);
            if (!destination.IsValid)
            {
                Messages.Message("Force.MindTrick_NoValidDestination".Translate(),
                              parent.pawn,
                              MessageTypeDefOf.RejectInput);
                return;
            }

            Job job = JobMaker.MakeJob(mindControlJob.jobDef, destination);
            float durationMultiplier = Props.durationMultiplier != null ?
                targetPawn.GetStatValue(Props.durationMultiplier) : 1f;

            job.expiryInterval = (parent.def.GetStatValueAbstract(StatDefOf.Ability_Duration, parent.pawn) * durationMultiplier).SecondsToTicks();
            job.mote = MoteMaker.MakeThoughtBubble(targetPawn, parent.def.iconPath, maintain: true);

            RestUtility.WakeUp(targetPawn);
            targetPawn.jobs.StopAll();
            targetPawn.jobs.StartJob(job, JobCondition.InterruptForced);
        }

        private LocalTargetInfo GetDestinationForJob(MindControlJob job, Pawn targetPawn)
        {
            if (!job.useCustomDestination)
            {
                return GetDestination(new LocalTargetInfo(targetPawn.Position));
            }

            var originalDest = Props.destination;
            var originalRange = Props.range;
            var originalRandomRange = Props.randomRange;

            Props.destination = job.destination;
            Props.range = job.range;
            Props.randomRange = job.randomRange;

            var result = GetDestination(new LocalTargetInfo(targetPawn.Position));

            Props.destination = originalDest;
            Props.range = originalRange;
            Props.randomRange = originalRandomRange;

            return result;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (TryResist(target.Pawn))
            {
                MoteMaker.ThrowText(target.Pawn.DrawPos, target.Pawn.Map, "Force.MindTrick_Resisted".Translate());
                return;
            }

            if (target.Thing is Pawn pawn)
            {
                if (Find.Selector.IsSelected(parent.pawn))
                {
                    Find.WindowStack.Add(new Window_JobSelector(pawn, this));
                }
                else if (Props.availableJobs.Count > 0)
                {
                    MindControlJob randomJob = Props.availableJobs.RandomElement();
                    AssignJob(randomJob, pawn);
                }
            }
        }

        protected virtual bool TryResist(Pawn pawn)
        {
            if (pawn.GetStatValue(StatDefOf.PsychicSensitivity) <= 0f)
            {
                Messages.Message("Force.MindTrick_ZeroSensitivityResist".Translate(pawn.LabelShort),
                              pawn,
                              MessageTypeDefOf.NegativeEvent);
                return true;
            }
            float casterForcePower = parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity);
            float targetSensitivity = pawn.GetStatValue(StatDefOf.PsychicSensitivity);
            float resistChance = Mathf.Clamp01(0.3f * (targetSensitivity - casterForcePower));

            if (Rand.Value < resistChance)
            {
                Messages.Message("Force.MindTrick_Resisted".Translate(pawn.LabelShort),
                              pawn,
                              MessageTypeDefOf.NegativeEvent);
                return true;
            }

            return false;
        }
    }

    public class CompProperties_MindTrick : CompProperties_EffectWithDest
    {
        public List<MindControlJob> availableJobs = new List<MindControlJob>();
        public StatDef durationMultiplier;

        public CompProperties_MindTrick()
        {
            compClass = typeof(CompAbilityEffect_MindTrick);
        }
    }

    public class Window_JobSelector : Window
    {
        private readonly Pawn targetPawn;
        private readonly CompAbilityEffect_MindTrick compAbilityEffect;
        private Vector2 scrollPosition;
        private float totalJobHeight;

        public Window_JobSelector(Pawn targetPawn, CompAbilityEffect_MindTrick compAbilityEffect)
        {
            this.targetPawn = targetPawn;
            this.compAbilityEffect = compAbilityEffect;
            closeOnAccept = false;
            closeOnCancel = true;
            doCloseButton = true;
            doWindowBackground = true;
            absorbInputAroundWindow = true;
            forcePause = true;

            float height = Mathf.Min(600f, 100f + (compAbilityEffect.Props.availableJobs.Count * 45f));
            windowRect = new Rect(
                (UI.screenWidth - 500f) / 2f,
                (UI.screenHeight - height) / 2f,
                500f,
                height
            );
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 35f);
            Widgets.Label(titleRect, "Force.MindTrick_WindowTitle".Translate(targetPawn.LabelShortCap));
            Text.Font = GameFont.Small;

            Rect contentRect = new Rect(inRect.x, titleRect.yMax + 10f, inRect.width, inRect.height - titleRect.height - 10f - CloseButSize.y);
            Widgets.DrawBoxSolid(contentRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            totalJobHeight = 0f;
            foreach (var job in compAbilityEffect.Props.availableJobs)
            {
                if (job?.jobDef != null)
                {
                    totalJobHeight += 30f;
                    if (!job.description.NullOrEmpty())
                    {
                        totalJobHeight += Text.CalcHeight(job.description, contentRect.width - 30f);
                    }
                    totalJobHeight += 10f;
                }
            }

            Rect viewRect = new Rect(0f, 0f, contentRect.width - 16f, totalJobHeight);
            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);

            float currentY = 0f;
            foreach (MindControlJob job in compAbilityEffect.Props.availableJobs)
            {
                if (job?.jobDef == null) continue;

                Rect jobRect = new Rect(0f, currentY, viewRect.width, 30f);
                string label = job.labelOverride ?? job.jobDef.LabelCap;
                if (Widgets.ButtonText(jobRect, label))
                {
                    compAbilityEffect.AssignJob(job, targetPawn);
                    Close();
                }
                currentY += 30f;

                if (!job.description.NullOrEmpty())
                {
                    float descHeight = Text.CalcHeight(job.description, viewRect.width - 30f);
                    Rect descRect = new Rect(15f, currentY, viewRect.width - 30f, descHeight);
                    Widgets.Label(descRect, job.description);
                    currentY += descHeight;
                }

                currentY += 10f;
            }

            Widgets.EndScrollView();
        }
    }

    public class MindControlJob
    {
        public JobDef jobDef;
        public bool useCustomDestination = false;
        public AbilityEffectDestination destination = AbilityEffectDestination.Selected;
        public float range = -1f;
        public FloatRange randomRange = FloatRange.Zero;
        public string labelOverride;
        public string description;

        public bool IsValid => jobDef != null;
    }
}