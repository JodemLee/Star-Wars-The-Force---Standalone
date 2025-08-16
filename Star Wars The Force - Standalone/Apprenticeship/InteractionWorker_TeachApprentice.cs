using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Apprenticeship
{
    internal class InteractionWorker_TeachApprentice : InteractionWorker
    {
        private const int BaseXP = 25;
        private const float AbilityLearnChance = 0.05f;

        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            if (!IsValidMasterApprenticePair(initiator, recipient))
                return 0f;

            int levelDifference = initiator.GetComp<CompClass_ForceUser>().forceLevel -
                                recipient.GetComp<CompClass_ForceUser>().forceLevel;

            return Mathf.Clamp(0.1f * levelDifference, 0.05f, 0.8f);
        }

        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks,
            out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            base.Interacted(initiator, recipient, extraSentencePacks, out letterText, out letterLabel, out letterDef, out lookTargets);

            int levelDifference = initiator.GetComp<CompClass_ForceUser>().forceLevel -
                                recipient.GetComp<CompClass_ForceUser>().forceLevel;

            int xpGain = BaseXP * levelDifference;
            recipient.GetComp<CompClass_ForceUser>().Leveling.AddForceExperience(xpGain);

            bool learnedAbility = false;
            AbilityDef learnedAbilityDef = null;

            if (Rand.Chance(AbilityLearnChance))
            {
                learnedAbilityDef = TryTeachRandomAbility(initiator, recipient);
                learnedAbility = learnedAbilityDef != null;
            }

            if (learnedAbility)
            {
                letterLabel = "Force.Teaching_BreakthroughTitle".Translate();
                letterText = "Force.Teaching_BreakthroughText".Translate(
                    initiator.LabelShort,
                    recipient.LabelShort,
                    xpGain,
                    learnedAbilityDef.label
                );
                letterDef = LetterDefOf.PositiveEvent;
            }
            else
            {
                letterLabel = "Force.Teaching_SessionTitle".Translate();
                letterText = "Force.Teaching_SessionText".Translate(
                    initiator.LabelShort,
                    recipient.LabelShort,
                    xpGain
                );
                letterDef = LetterDefOf.PositiveEvent;
            }

            lookTargets = new LookTargets(initiator, recipient);
        }

        private AbilityDef TryTeachRandomAbility(Pawn master, Pawn apprentice)
        {
            var masterAbilities = master.abilities?.abilities;
            var apprenticeAbilities = apprentice.abilities?.abilities;

            if (masterAbilities == null || apprenticeAbilities == null)
                return null;

            var teachableAbilities = masterAbilities
                .Where(a => a.def.comps.Any(c => c.compClass == typeof(CompAbilityEffect_ForcePower)))
                .Where(a => !apprenticeAbilities.Any(appAbility => appAbility.def == a.def))
                .ToList();

            if (teachableAbilities.Count == 0)
                return null;

            var abilityToLearn = teachableAbilities.RandomElement().def;
            apprentice.abilities.GainAbility(abilityToLearn);

            Messages.Message("Force.Teaching_AbilityLearned".Translate(
                apprentice.LabelShort,
                abilityToLearn.LabelCap
            ), apprentice, MessageTypeDefOf.PositiveEvent);

            return abilityToLearn;
        }

        private bool IsValidMasterApprenticePair(Pawn initiator, Pawn recipient)
        {
            var masterHediff = initiator.health?.hediffSet?.GetFirstHediffOfDef(ForceDefOf.Force_Master) as Hediff_Master;
            if (masterHediff == null) return false;

            var apprenticeHediff = recipient.health?.hediffSet?.GetFirstHediffOfDef(ForceDefOf.Force_Apprentice) as Hediff_Apprentice;

            return apprenticeHediff != null
                && masterHediff.apprentices.Contains(recipient)
                && apprenticeHediff.master == initiator;
        }
    }
}