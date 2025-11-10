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

            var initiatorComp = initiator.GetComp<CompClass_ForceUser>();
            var recipientComp = recipient.GetComp<CompClass_ForceUser>();

            if (initiatorComp == null || recipientComp == null)
                return 0f;

            int levelDifference = initiatorComp.forceLevel - recipientComp.forceLevel;

            return Mathf.Clamp(0.1f * levelDifference, 0.05f, 0.8f);
        }

        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks,
            out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            base.Interacted(initiator, recipient, extraSentencePacks, out letterText, out letterLabel, out letterDef, out lookTargets);

            var initiatorComp = initiator.GetComp<CompClass_ForceUser>();
            var recipientComp = recipient.GetComp<CompClass_ForceUser>();

            if (initiatorComp == null || recipientComp == null)
            {
                letterLabel = "Force.Teaching_FailedTitle".Translate();
                letterText = "Force.Teaching_FailedText".Translate(initiator.LabelShort, recipient.LabelShort);
                letterDef = LetterDefOf.NegativeEvent;
                lookTargets = new LookTargets(initiator, recipient);
                return;
            }

            int levelDifference = initiatorComp.forceLevel - recipientComp.forceLevel;
            int xpGain = BaseXP * levelDifference;
            recipientComp.Leveling.AddForceExperience(xpGain);

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
            var masterForceUser = master.GetComp<CompClass_ForceUser>();
            var apprenticeForceUser = apprentice.GetComp<CompClass_ForceUser>();

            if (masterForceUser == null || apprenticeForceUser == null)
                return null;

            var masterUnlockedAbilities = masterForceUser.unlockedAbiliities
                .Select(defName => DefDatabase<AbilityDef>.GetNamedSilentFail(defName))
                .Where(def => def != null && def.comps.Any(c => c.compClass == typeof(CompAbilityEffect_ForcePower)))
                .ToList();

            if (masterUnlockedAbilities.Count == 0)
                return null;

            var teachableAbilities = masterUnlockedAbilities
                .Where(masterAbility => !apprenticeForceUser.unlockedAbiliities.Contains(masterAbility.defName))
                .ToList();

            if (teachableAbilities.Count == 0)
                return null;

            var abilityToLearn = teachableAbilities.RandomElement();
            apprenticeForceUser.unlockedAbiliities.Add(abilityToLearn.defName);
            apprenticeForceUser.Abilities.UpdatePawnAbilities();
            apprenticeForceUser.Abilities.OnAbilityUnlocked(abilityToLearn.defName);

            Messages.Message("Force.Teaching_AbilityLearned".Translate(
                apprentice.LabelShort,
                abilityToLearn.LabelCap
            ), apprentice, MessageTypeDefOf.PositiveEvent);

            return abilityToLearn;
        }

        private bool IsValidMasterApprenticePair(Pawn initiator, Pawn recipient)
        {
            var initiatorComp = initiator.GetComp<CompClass_ForceUser>();
            var recipientComp = recipient.GetComp<CompClass_ForceUser>();

            if (initiatorComp?.Apprenticeship == null || recipientComp?.Apprenticeship == null)
                return false;

            // Check if initiator is master of recipient
            return initiatorComp.Apprenticeship.apprentices.Contains(recipient) &&
                   recipientComp.Apprenticeship.master == initiator;
        }
    }
}