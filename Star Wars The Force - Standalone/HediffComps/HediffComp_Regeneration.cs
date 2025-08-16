using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.HediffComps
{
    public class HediffComp_Regeneration : HediffComp
    {
        public HediffCompProperties_Regeneration Props => (HediffCompProperties_Regeneration)props;
        protected int ticksToHeal;
        protected Hediff nextTarget;

        public override void CompPostMake()
        {
            base.CompPostMake();
            ResetTicksToHeal();
        }

        public virtual void ResetTicksToHeal()
        {
            var healableHediffs = GetHealableHediffs();
            if (!healableHediffs.Any())
            {
                parent.pawn.health.RemoveHediff(parent);
                return;
            }

            var missingParts = healableHediffs.OfType<Hediff_MissingPart>().ToList();
            if (missingParts.Any())
            {
                nextTarget = missingParts
                    .Where(mp => !parent.pawn.health.hediffSet.PartIsMissing(mp.Part.parent))
                    .OrderByDescending(mp => GetPartDepth(mp.Part))
                    .FirstOrDefault();

                if (nextTarget == null)
                {
                    nextTarget = missingParts
                        .OrderByDescending(mp => GetPartDepth(mp.Part))
                        .FirstOrDefault();
                }
            }
            else
            {
                nextTarget = healableHediffs.RandomElement();
            }

            ticksToHeal = CalculateHealingTicks(nextTarget);
        }

        protected int GetPartDepth(BodyPartRecord part)
        {
            int depth = 0;
            while (part != null)
            {
                depth++;
                part = part.parent;
            }
            return depth;
        }

        protected List<Hediff> GetHealableHediffs()
        {
            return base.Pawn.health.hediffSet.hediffs
                .Where(hd =>
                {
                    if (hd.def.countsAsAddedPartOrImplant || !hd.def.isBad)
                        return false;

                    if (hd is Hediff_MissingPart missingPart)
                    {
                        BodyPartRecord currentPart = missingPart.Part;
                        int depthChecked = 0;
                        const int maxParentDepth = 3;

                        while (currentPart != null && depthChecked < maxParentDepth)
                        {
                            if (base.Pawn.health.hediffSet.hediffs
                                .Any(x => x.Part == currentPart && x.def.countsAsAddedPartOrImplant))
                            {
                                return false;
                            }
                            currentPart = currentPart.parent;
                            depthChecked++;
                        }
                    }
                    return true;
                })
                .ToList();
        }

        protected int CalculateHealingTicks(Hediff hediff)
        {
            const float baseHours = 3f;
            const float ticksPerHour = 2500f;
            float baseTicks = baseHours * ticksPerHour;

            float healingFactor = parent.pawn.HealthScale * parent.pawn.GetStatValue(StatDefOf.InjuryHealingFactor);

            if (hediff is Hediff_MissingPart missingPart)
            {
                float partMaxHealth = missingPart.Part.def.GetMaxHealth(parent.pawn);
                float normalizedHealth = Mathf.Clamp(partMaxHealth / 30f, 0.5f, 3f);
                return (int)(baseTicks * 4f * normalizedHealth / healingFactor);
            }

            if (hediff.IsPermanent() || hediff.def.chronic)
            {
                float severityFactor = Mathf.Lerp(0.5f, 4f, hediff.Severity / hediff.def.maxSeverity);
                return (int)(baseTicks * severityFactor / healingFactor);
            }

            return (int)(baseTicks / healingFactor);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            ticksToHeal--;
            if (ticksToHeal <= 0)
            {
                if (nextTarget != null && nextTarget.pawn != null)
                {
                    TryHealPermanentWound(nextTarget, parent.LabelCap);
                }
                ResetTicksToHeal();
            }
        }

        protected bool IsChildOfPart(BodyPartRecord part, BodyPartRecord potentialParent)
        {
            while (part != null)
            {
                if (part.parent == potentialParent)
                    return true;
                part = part.parent;
            }
            return false;
        }

        protected void TryHealPermanentWound(Hediff target, string cause)
        {
            if (target is Hediff_MissingPart missingPart)
            {
                BodyPartRecord partToHeal = missingPart.Part;
                while (partToHeal.parent != null &&
                       parent.pawn.health.hediffSet.PartIsMissing(partToHeal.parent))
                {
                    partToHeal = partToHeal.parent;
                }
                var partsToRemove = parent.pawn.health.hediffSet.GetMissingPartsCommonAncestors()
                    .Where(mp => mp.Part == partToHeal || IsChildOfPart(mp.Part, partToHeal))
                    .ToList();
                var missingHediffsBefore = parent.pawn.health.hediffSet.hediffs.OfType<Hediff_MissingPart>().ToList();
                foreach (var mp in partsToRemove)
                {
                    parent.pawn.health.RemoveHediff(mp);
                }

                FleckMaker.ThrowMetaIcon(parent.pawn.Position, parent.pawn.Map, FleckDefOf.HealingCross);
            }
            else
            {
                HealthUtility.Cure(target);
            }

            if (PawnUtility.ShouldSendNotificationAbout(target.pawn))
            {
                string messageKey = target is Hediff_MissingPart ?
                    "MessageRegrowingBodyPart" : "MessagePermanentWoundHealed";
                Messages.Message(messageKey.Translate(
                    cause,
                    target.pawn.LabelShort,
                    target.Label,
                    target.pawn.Named("PAWN")),
                    target.pawn,
                    MessageTypeDefOf.PositiveEvent);
            }
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref ticksToHeal, "ticksToHeal", 0);
            Scribe_References.Look(ref nextTarget, "nextTarget");
        }

        public override string CompTipStringExtra
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();

                if (!parent.def.description.NullOrEmpty())
                {
                    stringBuilder.AppendLine();
                }

                stringBuilder.AppendLine("Regenerative Healing Active");

                var healableHediffs = GetHealableHediffs();

                if (nextTarget != null)
                {
                    string targetInfo = nextTarget.Part != null
                        ? $"{nextTarget.LabelCap} ({nextTarget.Part.Label})"
                        : nextTarget.LabelCap;

                    stringBuilder.AppendLine($"Healing: {targetInfo}");
                    stringBuilder.AppendLine($"Ready in: {ticksToHeal.ToStringTicksToPeriod()}");
                }

                if (healableHediffs.Any())
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine("Remaining injuries:");
                    foreach (Hediff hd in healableHediffs)
                    {
                        string hdInfo = hd.Part != null
                            ? $"{hd.LabelCap} ({hd.Part.Label})"
                            : hd.LabelCap;

                        string estimate = hd == nextTarget ?
                            $"({ticksToHeal.ToStringTicksToPeriod()} left)" :
                            $"(~{CalculateHealingTicks(hd).ToStringTicksToPeriod()})";

                        stringBuilder.AppendLine($"- {hdInfo} {estimate}");
                    }

                    int totalTicks = healableHediffs.Sum(hd => hd == nextTarget ?
                        ticksToHeal : CalculateHealingTicks(hd));
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine($"Full recovery in: ~{totalTicks.ToStringTicksToPeriod()}");
                }
                else
                {
                    stringBuilder.AppendLine("All injuries healed!");
                }

                return stringBuilder.ToString().TrimEndNewlines();
            }
        }
    }

    public class HediffCompProperties_Regeneration : HediffCompProperties
    {
        public HediffCompProperties_Regeneration()
        {
            compClass = typeof(HediffComp_Regeneration);
        }
    }
}