using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Nightsister
{
    public class CompAbilityEffect_SummonObject : CompAbilityEffect
    {
        private const float BaseValuePerPoint = 1176f;
        private const float IntellectWeight = 0.3f;
        private const float FPCostPerSilver = 0.5f;
        private const float MinFPCost = 5f;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (!target.HasThing || target.Thing == null)
                return;

            var pawn = parent.pawn;
            var forceUser = pawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null) return;

            float psychicFactor = pawn.GetStatValue(StatDefOf.PsychicSensitivity);
            float intellectFactor = pawn.skills.GetSkill(SkillDefOf.Intellectual).Level / 20f;
            float combinedStat = (psychicFactor * (1f - IntellectWeight)) + (intellectFactor * IntellectWeight);
            float maxSummonValue = combinedStat * BaseValuePerPoint;

            ThingDef copyDef = target.Thing.def;
            float itemValue = copyDef.BaseMarketValue;

            float fpCost = Mathf.Max(itemValue * FPCostPerSilver, MinFPCost);

            if (!forceUser.TrySpendFP(fpCost))
            {
                Messages.Message(
                    "Force.SummonObject.NotEnoughFP".Translate(copyDef.label, fpCost.ToString("F1")),
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            if (itemValue > maxSummonValue)
            {
                Messages.Message(
                    "Force.SummonObject.ValueTooHigh".Translate(copyDef.label, itemValue.ToString("F0"), maxSummonValue.ToString("F0")),
                    MessageTypeDefOf.RejectInput
                );
                forceUser.RecoverFP(fpCost);
                return;
            }

            Thing copy = copyDef.MadeFromStuff
                ? ThingMaker.MakeThing(copyDef, target.Thing.Stuff ?? GenStuff.DefaultStuffFor(copyDef))
                : ThingMaker.MakeThing(copyDef);

            if (copyDef.useHitPoints)
            {
                float durabilityFactor = 0.3f + (combinedStat * 0.7f);
                copy.HitPoints = (int)(copy.MaxHitPoints * durabilityFactor);
            }

            GenPlace.TryPlaceThing(copy, target.Thing.Position, target.Thing.Map, ThingPlaceMode.Near);

            // Visual effects
            FleckMaker.ThrowSmoke(target.Thing.Position.ToVector3(), target.Thing.Map, 1f);
            FleckMaker.ThrowLightningGlow(target.Thing.Position.ToVector3(), target.Thing.Map, 1.5f);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!target.HasThing)
            {
                if (throwMessages)
                {
                    Messages.Message("Force.SummonObject.MustTargetObject".Translate(), MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            // Prevent summoning certain types of objects
            if (target.Thing is Pawn || target.Thing is Building || target.Thing.def.IsCorpse)
            {
                if (throwMessages)
                {
                    Messages.Message("Force.SummonObject.CannotSummonLivingOrBuildings".Translate(), MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            // Check FP cost
            var forceUser = parent.pawn.GetComp<CompClass_ForceUser>();
            if (forceUser != null)
            {
                float fpCost = Mathf.Max(target.Thing.def.BaseMarketValue * FPCostPerSilver, MinFPCost);
                if (forceUser.currentFP < fpCost)
                {
                    if (throwMessages)
                    {
                        Messages.Message(
                            "Force.SummonObject.NotEnoughFPGeneral".Translate(fpCost.ToString("F1"), forceUser.currentFP.ToString("F1")),
                            MessageTypeDefOf.RejectInput
                        );
                    }
                    return false;
                }
            }

            return base.Valid(target, throwMessages);
        }

        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            yield return new PreCastAction
            {
                action = delegate (LocalTargetInfo target, LocalTargetInfo dest)
                {
                    if (target.HasThing)
                    {
                        FleckMaker.ThrowLightningGlow(target.Thing.DrawPos, target.Thing.Map, 1f);
                        FleckMaker.Static(target.Thing.Position, target.Thing.Map, FleckDefOf.PsycastAreaEffect);

                        // Show FP cost preview
                        if (parent.pawn.GetComp<CompClass_ForceUser>() is CompClass_ForceUser forceUser)
                        {
                            float fpCost = Mathf.Max(target.Thing.def.BaseMarketValue * FPCostPerSilver, MinFPCost);
                            MoteMaker.ThrowText(
                                target.Thing.DrawPos,
                                target.Thing.Map,
                                "Force.SummonObject.FPCostPreview".Translate(fpCost.ToString("F1")),
                                Color.yellow
                            );
                        }
                    }
                },
                ticksAwayFromCast = 15
            };
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            if (target.HasThing)
            {
                float fpCost = Mathf.Max(target.Thing.def.BaseMarketValue * FPCostPerSilver, MinFPCost);
                return "Force.SummonObject.FPCostLabel".Translate(fpCost.ToString("F1"));
            }
            return null;
        }
    }
}