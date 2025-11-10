using RimWorld;
using System.Collections.Generic;
using System.Linq;
using TheForce_Standalone;
using Verse;

namespace TheForce_Standalone.Generic
{
    public class CompUseEffect_GainRandomAbility : CompUseEffect
    {
        public  CompProperties_UseEffect_GainRandomAbility Props => (CompProperties_UseEffect_GainRandomAbility)props;

        private AbilityDef selectedAbility;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (selectedAbility == null)
            {
                selectedAbility = GetRandomAbility();
            }
        }


        public override void DoEffect(Pawn user)
        {
            base.DoEffect(user);

            var forceUserComp = user.GetComp<CompClass_ForceUser>();
            if (forceUserComp == null || !forceUserComp.IsValidForceUser)
            {
                Messages.Message("Force.MustBeForceUserToUse".Translate(), user, MessageTypeDefOf.RejectInput);
                return;
            }

            if (selectedAbility == null)
            {
                selectedAbility = GetRandomAbility();
            }

            // Grant the selected ability
            if (!forceUserComp.unlockedAbiliities.Contains(selectedAbility.defName))
            {
                forceUserComp.unlockedAbiliities.Add(selectedAbility.defName);
                forceUserComp.Abilities.UpdatePawnAbilities();

                if (PawnUtility.ShouldSendNotificationAbout(user))
                {
                    Messages.Message("AbilityNeurotrainerUsed".Translate(user.Named("USER"), selectedAbility.LabelCap),
                                    user, MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        private AbilityDef GetRandomAbility()
        {
            if (Props.abilityCategories == null || Props.abilityCategories.Count == 0)
            {
                Log.Error("CompUseEffect_GainRandomAbility: No ability categories defined!");
                return null;
            }

            // Get all eligible abilities from the specified categories
            var eligibleAbilities = new List<AbilityDef>();
            foreach (var category in Props.abilityCategories)
            {
                var abilitiesInCategory = DefDatabase<AbilityDef>.AllDefs
                    .Where(ability => ability.category == category)
                    .Where(ability =>
                    {
                        var forceExt = ability.GetModExtension<ForceAbilityDefExtension>();
                        return forceExt != null;
                    });

                eligibleAbilities.AddRange(abilitiesInCategory);
            }

            // Select one random ability
            return eligibleAbilities.RandomElement();
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            var forceUserComp = p.GetComp<CompClass_ForceUser>();
            if (forceUserComp == null || !forceUserComp.IsValidForceUser)
            {
                return "Force.MustBeForceUserToUse".Translate();
            }

            if (forceUserComp.unlockedAbiliities.Contains(selectedAbility.defName))
            {
                return "Force.AlreadyKnowAbility".Translate();
            }

            if (Props.abilityCategories == null || Props.abilityCategories.Count == 0)
            {
                return "Force.NoAbilityCategoriesDefined".Translate();
            }

            bool hasEligibleAbilities = false;
            foreach (var category in Props.abilityCategories)
            {
                var abilitiesInCategory = DefDatabase<AbilityDef>.AllDefs
                    .Where(ability => ability.category == category)
                    .Where(ability =>
                    {
                        var forceExt = ability.GetModExtension<ForceAbilityDefExtension>();
                        return forceExt != null && forceUserComp.forceLevel >= forceExt.RequiredLevel;
                    })
                    .Where(ability => !forceUserComp.unlockedAbiliities.Contains(ability.defName));

                if (abilitiesInCategory.Any())
                {
                    hasEligibleAbilities = true;
                    break;
                }
            }

            if (!hasEligibleAbilities)
            {
                return "Force.AlreadyHasAllAbilitiesFromCategories".Translate();
            }

            return base.CanBeUsedBy(p);
        }

        public override TaggedString ConfirmMessage(Pawn p)
        {
            var forceUserComp = p.GetComp<CompClass_ForceUser>();
            if (forceUserComp == null) return null;

            if (selectedAbility == null)
            {
                selectedAbility = GetRandomAbility();
            }

            if (selectedAbility != null)
            {
                return "Force.GainRandomAbilityConfirm".Translate(selectedAbility.LabelCap);
            }

            return null;
        }

        public override bool AllowStackWith(Thing other)
        {
            if (!base.AllowStackWith(other))
            {
                return false;
            }

            CompUseEffect_GainRandomAbility otherComp = other.TryGetComp<CompUseEffect_GainRandomAbility>();
            if (otherComp == null || !Props.abilityCategories.SequenceEqual(otherComp.Props.abilityCategories))
            {
                return false;
            }

            return true;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref selectedAbility, "selectedAbility");
        }

        public override string CompInspectStringExtra()
        {
            if (selectedAbility != null)
            {
                return "Force.ContainsAbility".Translate(selectedAbility.LabelCap);
            }
            return null;
        }


        public override string CompTipStringExtra()
        {
            if (selectedAbility != null)
            {
                return "Force.StoredAbilityTooltip".Translate(selectedAbility.LabelCap, selectedAbility.description);
            }
            return null;
        }

        public override string TransformLabel(string label)
        {
            if (selectedAbility != null)
            {
                return label + " (" + selectedAbility.LabelCap + ")";
            }
            return label;
        }
    }

    public class CompProperties_UseEffect_GainRandomAbility : CompProperties_UseEffect
    {
        public List<AbilityCategoryDef> abilityCategories;

        public CompProperties_UseEffect_GainRandomAbility()
        {
            compClass = typeof(CompUseEffect_GainRandomAbility);
        }
    }
}

