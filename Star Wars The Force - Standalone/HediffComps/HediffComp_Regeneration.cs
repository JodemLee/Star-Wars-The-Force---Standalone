using RimWorld;
using System;
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
            var pawn = base.Pawn;
            var hediffs = pawn.health.hediffSet.hediffs;
            var healableHediffs = new List<Hediff>();
            var exclude = Array.Empty<HediffDef>();

            // Replicate the exact priority logic from TryGetWorstHealthCondition
            var lifeThreatening = FindLifeThreateningHediff(pawn, exclude);
            if (lifeThreatening != null && IsHealableHediff(lifeThreatening))
                healableHediffs.Add(lifeThreatening);

            if (HealthUtility.TicksUntilDeathDueToBloodLoss(pawn) < 2500)
            {
                var bleeding = FindMostBleedingHediff(pawn, exclude);
                if (bleeding != null && IsHealableHediff(bleeding) && !healableHediffs.Contains(bleeding))
                    healableHediffs.Add(bleeding);
            }

            if (pawn.health.hediffSet.GetBrain() != null)
            {
                var brainInjury = FindPermanentInjury(pawn, Gen.YieldSingle(pawn.health.hediffSet.GetBrain()), exclude);
                if (brainInjury != null && IsHealableHediff(brainInjury) && !healableHediffs.Contains(brainInjury))
                    healableHediffs.Add(brainInjury);
            }

            float handCoverage = ThingDefOf.Human.race.body.GetPartsWithDef(BodyPartDefOf.Hand).First().coverageAbsWithChildren;
            var missingPart = FindBiggestMissingBodyPart(pawn, handCoverage);
            if (missingPart != null)
            {
                var missingHediff = hediffs.FirstOrDefault(h => h is Hediff_MissingPart mp && mp.Part == missingPart);
                if (missingHediff != null && IsHealableHediff(missingHediff) && !healableHediffs.Contains(missingHediff))
                    healableHediffs.Add(missingHediff);
            }

            var eyeInjury = FindPermanentInjury(pawn,
                from x in pawn.health.hediffSet.GetNotMissingParts()
                where x.def == BodyPartDefOf.Eye
                select x, exclude);
            if (eyeInjury != null && IsHealableHediff(eyeInjury) && !healableHediffs.Contains(eyeInjury))
                healableHediffs.Add(eyeInjury);

            var immunizable = FindImmunizableHediffWhichCanKill(pawn, exclude);
            if (immunizable != null && IsHealableHediff(immunizable) && !healableHediffs.Contains(immunizable))
                healableHediffs.Add(immunizable);

            var miscBadLethal = FindNonInjuryMiscBadHediff(pawn, onlyIfCanKill: true, checkDeprioritized: false, exclude);
            if (miscBadLethal != null && IsHealableHediff(miscBadLethal) && !healableHediffs.Contains(miscBadLethal))
                healableHediffs.Add(miscBadLethal);

            var miscBad = FindNonInjuryMiscBadHediff(pawn, onlyIfCanKill: false, checkDeprioritized: false, exclude);
            if (miscBad != null && IsHealableHediff(miscBad) && !healableHediffs.Contains(miscBad))
                healableHediffs.Add(miscBad);

            if (pawn.health.hediffSet.GetBrain() != null)
            {
                var brainInjuryNonPermanent = FindInjury(pawn, Gen.YieldSingle(pawn.health.hediffSet.GetBrain()), exclude);
                if (brainInjuryNonPermanent != null && IsHealableHediff(brainInjuryNonPermanent) && !healableHediffs.Contains(brainInjuryNonPermanent))
                    healableHediffs.Add(brainInjuryNonPermanent);
            }

            var generalMissingPart = FindBiggestMissingBodyPart(pawn);
            if (generalMissingPart != null)
            {
                var missingHediffGeneral = hediffs.FirstOrDefault(h => h is Hediff_MissingPart mp && mp.Part == generalMissingPart);
                if (missingHediffGeneral != null && IsHealableHediff(missingHediffGeneral) && !healableHediffs.Contains(missingHediffGeneral))
                    healableHediffs.Add(missingHediffGeneral);
            }

            var addiction = FindAddiction(pawn, exclude);
            if (addiction != null && IsHealableHediff(addiction) && !healableHediffs.Contains(addiction))
                healableHediffs.Add(addiction);

            var permanentInjury = FindPermanentInjury(pawn, null, exclude);
            if (permanentInjury != null && IsHealableHediff(permanentInjury) && !healableHediffs.Contains(permanentInjury))
                healableHediffs.Add(permanentInjury);

            var injury = FindInjury(pawn, null, exclude);
            if (injury != null && IsHealableHediff(injury) && !healableHediffs.Contains(injury))
                healableHediffs.Add(injury);

            var miscBadDeprioritized = FindNonInjuryMiscBadHediff(pawn, onlyIfCanKill: false, checkDeprioritized: true, exclude);
            if (miscBadDeprioritized != null && IsHealableHediff(miscBadDeprioritized) && !healableHediffs.Contains(miscBadDeprioritized))
                healableHediffs.Add(miscBadDeprioritized);

            return healableHediffs;
        }

        private bool IsHealableHediff(Hediff hd)
        {
            if (hd.def.countsAsAddedPartOrImplant || !hd.def.isBad || !hd.def.everCurableByItem || !hd.Visible)
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
        }

        private Hediff FindLifeThreateningHediff(Pawn pawn, params HediffDef[] exclude)
        {
            Hediff hediff = null;
            float num = -1f;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;

            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!hediffs[i].Visible || !hediffs[i].def.everCurableByItem || hediffs[i].FullyImmune() || (exclude != null && exclude.Contains(hediffs[i].def)))
                    continue;

                bool flag = hediffs[i].IsLethal && hediffs[i].Severity / hediffs[i].def.lethalSeverity >= 0.8f;
                if (hediffs[i].IsCurrentlyLifeThreatening || flag)
                {
                    float num2 = ((hediffs[i].Part != null) ? hediffs[i].Part.coverageAbsWithChildren : 999f);
                    if (hediff == null || num2 > num)
                    {
                        hediff = hediffs[i];
                        num = num2;
                    }
                }
            }
            return hediff;
        }

        private Hediff FindMostBleedingHediff(Pawn pawn, params HediffDef[] exclude)
        {
            float num = 0f;
            Hediff hediff = null;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;

            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i].Visible && hediffs[i].def.everCurableByItem && (exclude == null || !exclude.Contains(hediffs[i].def)))
                {
                    float bleedRate = hediffs[i].BleedRate;
                    if (bleedRate > 0f && (bleedRate > num || hediff == null))
                    {
                        num = bleedRate;
                        hediff = hediffs[i];
                    }
                }
            }
            return hediff;
        }

        private Hediff FindImmunizableHediffWhichCanKill(Pawn pawn, params HediffDef[] exclude)
        {
            Hediff hediff = null;
            float num = -1f;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;

            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i].Visible && hediffs[i].def.everCurableByItem && hediffs[i].TryGetComp<HediffComp_Immunizable>() != null &&
                    !hediffs[i].FullyImmune() && (exclude == null || !exclude.Contains(hediffs[i].def)) && hediffs[i].CanEverKill())
                {
                    float severity = hediffs[i].Severity;
                    if (hediff == null || severity > num)
                    {
                        hediff = hediffs[i];
                        num = severity;
                    }
                }
            }
            return hediff;
        }

        private Hediff FindNonInjuryMiscBadHediff(Pawn pawn, bool onlyIfCanKill, bool checkDeprioritized, params HediffDef[] exclude)
        {
            Hediff hediff = null;
            float num = -1f;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;

            for (int i = 0; i < hediffs.Count; i++)
            {
                if ((checkDeprioritized || !hediffs[i].def.deprioritizeHealing) && hediffs[i].Visible && hediffs[i].def.isBad &&
                    hediffs[i].def.everCurableByItem && !(hediffs[i] is Hediff_Injury) && !(hediffs[i] is Hediff_MissingPart) &&
                    !(hediffs[i] is Hediff_Addiction) && !(hediffs[i] is Hediff_AddedPart) &&
                    (!onlyIfCanKill || hediffs[i].CanEverKill()) && (exclude == null || !exclude.Contains(hediffs[i].def)))
                {
                    float num2 = ((hediffs[i].Part != null) ? hediffs[i].Part.coverageAbsWithChildren : 999f);
                    if (hediff == null || num2 > num)
                    {
                        hediff = hediffs[i];
                        num = num2;
                    }
                }
            }
            return hediff;
        }

        private BodyPartRecord FindBiggestMissingBodyPart(Pawn pawn, float minCoverage = 0f)
        {
            BodyPartRecord bodyPartRecord = null;
            foreach (Hediff_MissingPart missingPartsCommonAncestor in pawn.health.hediffSet.GetMissingPartsCommonAncestors())
            {
                if (!(missingPartsCommonAncestor.Part.coverageAbsWithChildren < minCoverage) &&
                    !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(missingPartsCommonAncestor.Part) &&
                    (bodyPartRecord == null || missingPartsCommonAncestor.Part.coverageAbsWithChildren > bodyPartRecord.coverageAbsWithChildren))
                {
                    bodyPartRecord = missingPartsCommonAncestor.Part;
                }
            }
            return bodyPartRecord;
        }

        private Hediff_Addiction FindAddiction(Pawn pawn, params HediffDef[] exclude)
        {
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is Hediff_Addiction { Visible: not false } hediff_Addiction &&
                    hediff_Addiction.def.everCurableByItem && (exclude == null || !exclude.Contains(hediffs[i].def)))
                {
                    return hediff_Addiction;
                }
            }
            return null;
        }

        private Hediff_Injury FindPermanentInjury(Pawn pawn, IEnumerable<BodyPartRecord> allowedBodyParts = null, params HediffDef[] exclude)
        {
            Hediff_Injury hediff_Injury = null;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;

            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is Hediff_Injury { Visible: not false } hediff_Injury2 && hediff_Injury2.IsPermanent() &&
                    hediff_Injury2.def.everCurableByItem && (allowedBodyParts == null || allowedBodyParts.Contains(hediff_Injury2.Part)) &&
                    (exclude == null || !exclude.Contains(hediffs[i].def)) && (hediff_Injury == null || hediff_Injury2.Severity > hediff_Injury.Severity))
                {
                    hediff_Injury = hediff_Injury2;
                }
            }
            return hediff_Injury;
        }

        private Hediff_Injury FindInjury(Pawn pawn, IEnumerable<BodyPartRecord> allowedBodyParts = null, params HediffDef[] exclude)
        {
            Hediff_Injury hediff_Injury = null;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;

            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is Hediff_Injury { Visible: not false } hediff_Injury2 && hediff_Injury2.def.everCurableByItem &&
                    (allowedBodyParts == null || allowedBodyParts.Contains(hediff_Injury2.Part)) &&
                    (exclude == null || !exclude.Contains(hediffs[i].def)) && (hediff_Injury == null || hediff_Injury2.Severity > hediff_Injury.Severity))
                {
                    hediff_Injury = hediff_Injury2;
                }
            }
            return hediff_Injury;
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
                    "Force.MessageRegrowingBodyPart" : "Force.MessagePermanentWoundHealed";
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