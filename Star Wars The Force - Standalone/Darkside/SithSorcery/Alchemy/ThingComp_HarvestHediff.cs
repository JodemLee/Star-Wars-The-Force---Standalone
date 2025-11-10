using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Darkside.SithSorcery.Alchemy
{
    internal class ThingComp_HarvestHediff : CompUseEffect
    {
        private HediffData storedHediff;
        private bool hasExtractedHediff = false;

        public override void DoEffect(Pawn user)
        {
            if (!hasExtractedHediff)
            {
                // Extraction mode - harvest hediff from the user
                TryExtractHediff(user);
            }
            else
            {
                // Injection mode - inject stored hediff into the user
                TryInjectHediff(user);
            }
        }

        private void TryExtractHediff(Pawn target)
        {
            // Get all extractable hediffs from the target
            var extractableHediffs = target.health.hediffSet.hediffs
                .Where(h => h.Visible && h.def != null && !h.def.countsAsAddedPartOrImplant)
                .ToList();

            if (extractableHediffs.Count == 0)
            {
                Messages.Message("Force.NoExtractableHediffs".Translate(), target, MessageTypeDefOf.RejectInput);
                return;
            }

            // If only one hediff, extract it automatically
            if (extractableHediffs.Count == 1)
            {
                ExtractSpecificHediff(target, extractableHediffs[0]);
            }
            else
            {
                // If multiple hediffs, show float menu options
                Find.WindowStack.Add(new FloatMenu(GetHediffSelectionOptions(target, extractableHediffs).ToList()));
            }
        }

        private IEnumerable<FloatMenuOption> GetHediffSelectionOptions(Pawn target, List<Hediff> hediffs)
        {
            foreach (var hediff in hediffs)
            {
                string durationInfo = "";
                var disappearsComp = hediff.TryGetComp<HediffComp_Disappears>();
                if (disappearsComp != null && disappearsComp.ticksToDisappear > 0)
                {
                    durationInfo = $" ({disappearsComp.ticksToDisappear.ToStringTicksToPeriod()})";
                }

                yield return new FloatMenuOption(
                    $"Force.ExtractHediff".Translate(hediff.def.LabelCap, hediff.Severity.ToStringPercent()) + durationInfo,
                    () => ExtractSpecificHediff(target, hediff)
                );
            }
        }

        private void ExtractSpecificHediff(Pawn target, Hediff hediff)
        {
            // Store hediff data in THIS item
            storedHediff = new HediffData
            {
                hediffDef = hediff.def,
                severity = hediff.Severity,
                part = hediff.Part
            };

            // Store disappears comp data if it exists
            var disappearsComp = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappearsComp != null)
            {
                storedHediff.disappearsTicks = disappearsComp.ticksToDisappear;
                storedHediff.disappearsAfterTicks = disappearsComp.disappearsAfterTicks;
                storedHediff.seed = disappearsComp.seed;
            }

            // Remove the hediff from the target
            target.health.RemoveHediff(hediff);
            hasExtractedHediff = true;

            Messages.Message("Force.HediffExtracted".Translate(hediff.def.label), target, MessageTypeDefOf.PositiveEvent);
        }

        private bool TryInjectHediff(Pawn target)
        {
            if (storedHediff == null || storedHediff.hediffDef == null)
            {
                Log.Error("Tried to inject null stored hediff");
                return false;
            }

            // Store the hediff info before clearing it
            var hediffDefLabel = storedHediff.hediffDef.label;

            // Check if target already has this hediff
            if (target.health.hediffSet.HasHediff(storedHediff.hediffDef))
            {
                Messages.Message("Force.AlreadyHasHediff".Translate(hediffDefLabel), target, MessageTypeDefOf.RejectInput);
                return false;
            }

            // Add the hediff to the target
            var newHediff = HediffMaker.MakeHediff(storedHediff.hediffDef, target, storedHediff.part);
            newHediff.Severity = storedHediff.severity;

            // Restore disappears comp data if it was stored
            if (storedHediff.disappearsTicks.HasValue)
            {
                var disappearsComp = newHediff.TryGetComp<HediffComp_Disappears>();
                if (disappearsComp != null)
                {
                    disappearsComp.ticksToDisappear = storedHediff.disappearsTicks.Value;
                    if (storedHediff.disappearsAfterTicks.HasValue)
                    {
                        disappearsComp.disappearsAfterTicks = storedHediff.disappearsAfterTicks.Value;
                    }
                    if (storedHediff.seed.HasValue)
                    {
                        disappearsComp.seed = storedHediff.seed.Value;
                    }
                }
            }

            target.health.AddHediff(newHediff);

            // Clear the stored hediff so item can be used again
            storedHediff = null;
            hasExtractedHediff = false;

            Messages.Message("Force.HediffInjected".Translate(hediffDefLabel), target, MessageTypeDefOf.PositiveEvent);
            return true;
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            if (p == null || p.health == null || p.health.hediffSet == null)
                return false;

            if (!hasExtractedHediff)
            {
                // Extraction mode - check if pawn has any extractable hediffs
                var hasExtractableHediffs = p.health.hediffSet.hediffs
                    .Any(h => h.Visible && h.def != null && !h.def.countsAsAddedPartOrImplant);

                if (!hasExtractableHediffs)
                    return "Force.NoExtractableHediffs".Translate();

                return true;
            }
            else
            {
                // Injection mode - check if pawn already has the stored hediff
                if (storedHediff?.hediffDef != null && p.health.hediffSet.HasHediff(storedHediff.hediffDef))
                    return "Force.AlreadyHasHediff".Translate(storedHediff.hediffDef.label);

                return true;
            }
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (selPawn == null || !selPawn.CanReach(parent, Verse.AI.PathEndMode.Touch, Danger.Some))
                yield break;

            string label;
            AcceptanceReport report = CanBeUsedBy(selPawn);

            if (!hasExtractedHediff)
            {
                label = "Force.ExtractHediff".Translate();
            }
            else
            {
                label = storedHediff?.hediffDef != null
                    ? "Force.InjectHediff".Translate(storedHediff.hediffDef.label)
                    : "Force.InjectHediff".Translate("unknown");
            }

            if (!report.Accepted)
            {
                yield return new FloatMenuOption(label + " (" + report.Reason + ")", null);
            }
            else
            {
                yield return new FloatMenuOption(label, () =>
                {
                    var compUsable = parent.TryGetComp<CompUsable>();
                    if (compUsable != null)
                    {
                        compUsable.TryStartUseJob(selPawn, parent);
                    }
                });
            }
        }

        public override string CompInspectStringExtra()
        {
            if (hasExtractedHediff && storedHediff?.hediffDef != null)
            {
                string durationInfo = "";
                if (storedHediff.disappearsTicks.HasValue && storedHediff.disappearsTicks.Value > 0)
                {
                    durationInfo = $" ({storedHediff.disappearsTicks.Value.ToStringTicksToPeriod()})";
                }
                return "Force.ContainsExtractedHediff".Translate(storedHediff.hediffDef.label, storedHediff.severity.ToString("F2")) + durationInfo;
            }
            return "Force.ReadyToExtractHediff".Translate();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref storedHediff, "storedHediff");
            Scribe_Values.Look(ref hasExtractedHediff, "hasExtractedHediff", false);
        }

        private class HediffData : IExposable
        {
            public HediffDef hediffDef;
            public float severity;
            public BodyPartRecord part;
            public int? disappearsTicks;
            public int? disappearsAfterTicks;
            public int? seed;

            public void ExposeData()
            {
                Scribe_Defs.Look(ref hediffDef, "hediffDef");
                Scribe_Values.Look(ref severity, "severity");
                Scribe_BodyParts.Look(ref part, "part");
                Scribe_Values.Look(ref disappearsTicks, "disappearsTicks");
                Scribe_Values.Look(ref disappearsAfterTicks, "disappearsAfterTicks");
                Scribe_Values.Look(ref seed, "seed");
            }
        }
    }

    public class CompProperties_UseEffect_ExtractInjectHediff : CompProperties_UseEffect
    {
        public CompProperties_UseEffect_ExtractInjectHediff()
        {
            compClass = typeof(ThingComp_HarvestHediff);
        }
    }
}