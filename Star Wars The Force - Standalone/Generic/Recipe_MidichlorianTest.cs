using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Generic
{
    public class Recipe_MidichlorianTest : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!base.AvailableOnNow(thing, part))
            {
                return false;
            }

            if (!(thing is Pawn pawn))
            {
                return false;
            }

            if (pawn.story?.traits?.HasTrait(DefDatabase<TraitDef>.GetNamed("Force_NeutralSensitivity")) ?? false)
            {
                return false;
            }
            return true;
        }

        public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord part = null)
        {
            var baseReport = base.AvailableReport(thing, part);
            if (!baseReport.Accepted)
            {
                return baseReport;
            }

            if (!(thing is Pawn pawn))
            {
                return "Force.InvalidTargetForSurgery".Translate();
            }

            if (pawn.story?.traits?.HasTrait(DefDatabase<TraitDef>.GetNamed("Force_NeutralSensitivity")) ?? false)
            {
                return "Force.AlreadyTested".Translate();
            }

            return true;
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null)
            {
                if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
            }

            OnSurgerySuccess(pawn, part, billDoer, ingredients, bill);
        }

        protected override void OnSurgerySuccess(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            base.OnSurgerySuccess(pawn, part, billDoer, ingredients, bill);
            int midichlorianCount = Rand.Range(0, 20001);
            string message = "Force.MidichlorianTestResults".Translate(pawn.LabelShortCap, midichlorianCount);

            if (midichlorianCount >= 5000)
            {
                int degree;
                if (midichlorianCount >= 15000)
                {
                    degree = 2;
                    message += "\n\n" + "Force.ExceptionalForceSensitivity".Translate();
                }
                else if (midichlorianCount >= 10000)
                {
                    degree = 1;
                    message += "\n\n" + "Force.StrongForceSensitivity".Translate();
                }
                else
                {
                    degree = -1;
                    message += "\n\n" + "Force.SomeForceSensitivity".Translate();
                }

                TraitDef forceTraitDef = DefDatabase<TraitDef>.GetNamed("Force_NeutralSensitivity");
                if (forceTraitDef != null)
                {
                    try
                    {
                        Trait newTrait = new Trait(forceTraitDef, degree, true);
                        pawn.story.traits.GainTrait(newTrait);
                        string degreeLabel = forceTraitDef.degreeDatas.FirstOrDefault(d => d.degree == degree)?.label ?? "Force.ForceSensitivity".Translate();
                        message += "\n\n" + "Force.GainedTrait".Translate(degreeLabel);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[MidichlorianTest] Error adding trait: {ex}");
                        message += "\n\n" + "Force.ErrorDeterminingSensitivity".Translate();
                    }
                }
                else
                {
                    message += "\n\n" + "Force.CouldNotDetermineSensitivity".Translate();
                }
            }
            else if (midichlorianCount < 5000)
            {
                message += "\n\n" + "Force.MidichlorianCountTooLow".Translate();
            }
            else
            {
                message += "\n\n" + "Force.NoSensitivityChange".Translate();
            }

            Messages.Message(message, pawn, MessageTypeDefOf.PositiveEvent);
        }
    }
}