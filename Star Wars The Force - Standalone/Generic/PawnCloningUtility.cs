using RimWorld;
using System;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Generic
{
    public static class PawnCloningUtility
    {
        public static Pawn Duplicate(Pawn pawn)
        {
            if (pawn == null)
                throw new ArgumentNullException(nameof(pawn), "Pawn cannot be null.");

            // Create a new pawn with the same biological and chronological age
            float biologicalAge = pawn.ageTracker.AgeBiologicalYearsFloat;
            float chronologicalAge = Math.Min(pawn.ageTracker.AgeChronologicalYearsFloat, biologicalAge);

            PawnGenerationRequest request = new PawnGenerationRequest(
                pawn.kindDef, pawn.Faction, PawnGenerationContext.NonPlayer, -1,
                forceGenerateNewPawn: true, allowDead: false, allowDowned: false,
                canGeneratePawnRelations: false, mustBeCapableOfViolence: false,
                0f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true,
                allowPregnant: false, allowFood: true, allowAddictions: true,
                inhabitant: false, certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false,
                0f, 0f, null, 0f, null, null, null, pawn.story.traits.allTraits.Select(t => t.def).ToList(), null,
                fixedGender: pawn.gender, fixedIdeo: pawn.Ideo,
                fixedBiologicalAge: biologicalAge, fixedChronologicalAge: chronologicalAge,
                fixedLastName: null, fixedBirthName: null, fixedTitle: null,
                forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: true,
                forceDead: false, forcedXenogenes: null, forcedEndogenes: null,
                forcedXenotype: pawn.genes.Xenotype, forcedCustomXenotype: pawn.genes.CustomXenotype,
                allowedXenotypes: null, forceBaselinerChance: 0f,
                developmentalStages: DevelopmentalStage.Adult, pawnKindDefGetter: null,
                excludeBiologicalAgeRange: null, biologicalAgeRange: null,
                forceRecruitable: false, dontGiveWeapon: false, onlyUseForcedBackstories: false,
                maximumAgeTraits: -1, minimumAgeTraits: 0, forceNoGear: true);

            Pawn newPawn = PawnGenerator.GeneratePawn(request);

            // Copy essential properties
            CopyBasicProperties(pawn, newPawn);
            CopyGenesAndTraits(pawn, newPawn);
            CopyAppearanceAndStyle(pawn, newPawn);
            CopySkillsAndAbilities(pawn, newPawn);
            CopyHealthAndNeeds(pawn, newPawn);

            // Notify the new pawn of duplication
            newPawn.Notify_DuplicatedFrom(pawn);
            newPawn.Drawer.renderer.SetAllGraphicsDirty();
            newPawn.Notify_DisabledWorkTypesChanged();

            return newPawn;
        }

        private static void CopyBasicProperties(Pawn source, Pawn target)
        {
            target.Name = NameTriple.FromString(source.Name.ToString());
            target.gender = source.gender;
            target.story.Adulthood = source.story.Adulthood;
            target.story.Childhood = source.story.Childhood;

            if (ModsConfig.BiotechActive)
            {
                target.ageTracker.growthPoints = source.ageTracker.growthPoints;
                target.ageTracker.vatGrowTicks = source.ageTracker.vatGrowTicks;
                target.genes.xenotypeName = source.genes.xenotypeName;
                target.genes.iconDef = source.genes.iconDef;
            }
        }

        private static void CopyGenesAndTraits(Pawn source, Pawn target)
        {
            // Copy genes
            if (ModsConfig.BiotechActive)
            {
                target.genes.Xenogenes.Clear();
                foreach (var gene in source.genes.Xenogenes)
                {
                    target.genes.AddGene(gene.def, xenogene: true);
                }

                target.genes.Endogenes.Clear();
                foreach (var gene in source.genes.Endogenes)
                {
                    target.genes.AddGene(gene.def, xenogene: false);
                }
            }

            target.story.traits.allTraits.Clear();
            foreach (var trait in source.story.traits.allTraits)
            {
                if (trait.sourceGene == null)
                    target.story.traits.GainTrait(new Trait(trait.def, trait.Degree, trait.ScenForced));
            }
        }

        private static void CopyAppearanceAndStyle(Pawn source, Pawn target)
        {
            target.story.headType = source.story.headType;
            target.story.bodyType = source.story.bodyType;
            target.story.hairDef = source.story.hairDef;
            target.story.HairColor = source.story.HairColor;
            target.story.SkinColorBase = source.story.SkinColorBase;
            target.story.skinColorOverride = source.story.skinColorOverride;
            target.story.furDef = source.story.furDef;

            target.style.beardDef = source.style.beardDef;
            if (ModsConfig.IdeologyActive)
            {
                target.style.BodyTattoo = source.style.BodyTattoo;
                target.style.FaceTattoo = source.style.FaceTattoo;
            }
        }

        private static void CopySkillsAndAbilities(Pawn source, Pawn target)
        {
            // Copy skills
            target.skills.skills.Clear();
            foreach (var skill in source.skills.skills)
            {
                var newSkill = new SkillRecord(target, skill.def)
                {
                    levelInt = skill.levelInt,
                    passion = skill.passion,
                    xpSinceLastLevel = skill.xpSinceLastLevel,
                    xpSinceMidnight = skill.xpSinceMidnight
                };
                target.skills.skills.Add(newSkill);
            }

            // Copy abilities
            target.abilities.abilities.Clear();
            foreach (var ability in source.abilities.abilities)
            {
                target.abilities.GainAbility(ability.def);
            }
        }

        private static void CopyHealthAndNeeds(Pawn source, Pawn target)
        {
            // Copy health (hediffs)
            target.health.hediffSet.hediffs.Clear();
            foreach (var hediff in source.health.hediffSet.hediffs)
            {
                if (hediff.def.duplicationAllowed && (hediff.Part == null || target.health.hediffSet.HasBodyPart(hediff.Part)))
                {
                    var newHediff = HediffMaker.MakeHediff(hediff.def, target, hediff.Part);
                    newHediff.CopyFrom(hediff);
                    target.health.hediffSet.AddDirect(newHediff);
                }
            }

            // Copy needs
            target.needs.AllNeeds.Clear();
            foreach (var need in source.needs.AllNeeds)
            {
                var newNeed = (Need)Activator.CreateInstance(need.def.needClass, target);
                newNeed.def = need.def;
                newNeed.SetInitialLevel();
                newNeed.CurLevel = need.CurLevel;
                target.needs.AllNeeds.Add(newNeed);
            }
        }
    }
}
