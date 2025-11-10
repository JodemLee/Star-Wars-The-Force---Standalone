using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
            CopyBasicProperties(pawn, newPawn);
            CopyGenesAndTraits(pawn, newPawn);
            CopyAppearanceAndStyle(pawn, newPawn);
            CopySkillsAndAbilities(pawn, newPawn);
            CopyHealthAndNeeds(pawn, newPawn);
            if (HARCompat.HARActive)
            {
                try
                {
                    HARCompat.CopyAlienData(pawn, newPawn);
                }
                catch
                {
                    Log.Error("HAR CHECK FAILED");
                }
            }

            newPawn.Notify_DuplicatedFrom(pawn);
            newPawn.Drawer.renderer.SetAllGraphicsDirty();
            newPawn.Notify_DisabledWorkTypesChanged();

            return newPawn;
        }

        public static void CopyBasicProperties(Pawn source, Pawn target)
        {
            target.Name = NameTriple.FromString(source.Name.ToString());
            target.gender = source.gender;
            target.story.Adulthood = source.story.Adulthood;
            target.story.Childhood = source.story.Childhood;

            if (ModsConfig.BiotechActive)
            {
                target.ageTracker.growthPoints = source.ageTracker.growthPoints;
                target.ageTracker.vatGrowTicks = source.ageTracker.vatGrowTicks;
            }
        }

        public static void CopyBasicPropertiesNoName(Pawn source, Pawn target)
        {

            target.story.Adulthood = source.story.Adulthood;
            target.story.Childhood = source.story.Childhood;

            if (ModsConfig.BiotechActive)
            {
                target.ageTracker.growthPoints = source.ageTracker.growthPoints;
                target.ageTracker.vatGrowTicks = source.ageTracker.vatGrowTicks;

            }
        }


        public static void CopyGenesAndTraits(Pawn source, Pawn target)
        {
            // Copy genes
            if (ModsConfig.BiotechActive)
            {
                target.genes.SetXenotype(source.genes.Xenotype);
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

        public static void CopyAppearanceAndStyle(Pawn source, Pawn target)
        {
            target.story.headType = source.story.headType;
            target.story.bodyType = source.story.bodyType;
            target.story.hairDef = source.story.hairDef;
            target.story.HairColor = source.story.HairColor;
            target.story.SkinColorBase = source.story.SkinColorBase;
            target.story.skinColorOverride = source.story.skinColorOverride;
            target.story.furDef = source.story.furDef;

            if (ModsConfig.IdeologyActive)
            {
                target.style.beardDef = source.style.beardDef;
                target.style.BodyTattoo = source.style.BodyTattoo;
                target.style.FaceTattoo = source.style.FaceTattoo;
            }
        }

        public static void CopySkillsAndAbilities(Pawn source, Pawn target)
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

        public static void CopyHealthAndNeeds(Pawn source, Pawn target)
        {
            // Copy health (hediffs)
            target.health.hediffSet.hediffs.Clear();
            foreach (var hediff in source.health.hediffSet.hediffs)
            {
                if (hediff.def.duplicationAllowed && (hediff.Part == null || target.health.hediffSet.HasBodyPart(hediff.Part)) && !hediff.def.countsAsAddedPartOrImplant)
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

        public static void CopyApparel(Pawn source, Pawn target)
        {

            if (source.apparel == null || source.apparel == null) return;

            // Remove any existing apparel from the copy first
            var copyApparelList = target.apparel.WornApparel.ToList();
            foreach (var apparel in copyApparelList)
            {
                target.apparel.Remove(apparel);
            }

            // Create a list of apparel to copy from original
            var apparelToCopy = source.apparel.WornApparel.ToList();
            var newApparelList = new List<Apparel>();

            // First create all the new apparel items
            foreach (Apparel originalApparel in apparelToCopy)
            {
                try
                {
                    if (originalApparel != null && originalApparel.def != null)
                    {
                        Apparel newApparel = (Apparel)ThingMaker.MakeThing(originalApparel.def, originalApparel.Stuff);
                        if (newApparel != null)
                        {
                            newApparelList.Add(newApparel);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error creating apparel copy for {originalApparel?.Label ?? "null"}: {ex}");
                }
            }

            // Then wear all the new apparel
            foreach (var newApparel in newApparelList)
            {
                try
                {
                    if (newApparel != null && target.apparel.CanWearWithoutDroppingAnything(newApparel.def))
                    {
                        target.apparel.Wear(newApparel);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error wearing apparel {newApparel?.Label ?? "null"}: {ex}");
                }
            }

        }

        public static void CopyEquipment(Pawn source, Pawn target)
        {
            if (source.equipment == null || target.equipment == null) return;

            try
            {
                // Remove any existing equipment from the target first
                var targetEquipmentList = target.equipment.AllEquipmentListForReading.ToList();
                foreach (var equipment in targetEquipmentList)
                {
                    target.equipment.Remove(equipment);
                }

                // Copy all equipment from source to target
                var equipmentToCopy = source.equipment.AllEquipmentListForReading.ToList();
                var newEquipmentList = new List<ThingWithComps>();

                // First create all the new equipment items
                foreach (ThingWithComps originalEquipment in equipmentToCopy)
                {
                    try
                    {
                        if (originalEquipment != null && originalEquipment.def != null)
                        {
                            ThingWithComps newEquipment = (ThingWithComps)ThingMaker.MakeThing(originalEquipment.def, originalEquipment.Stuff);
                            if (newEquipment != null)
                            {
                                // Copy hit points
                                newEquipment.HitPoints = Mathf.Clamp(originalEquipment.HitPoints, 1, newEquipment.MaxHitPoints);
                                newEquipmentList.Add(newEquipment);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error creating equipment copy for {originalEquipment?.Label ?? "null"}: {ex}");
                    }
                }

                // Then add all the new equipment
                foreach (var newEquipment in newEquipmentList)
                {
                    try
                    {
                        if (newEquipment != null)
                        {
                            target.equipment.AddEquipment(newEquipment);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error adding equipment {newEquipment?.Label ?? "null"}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in CopyEquipment: {ex}");
            }
        }
    }
}
