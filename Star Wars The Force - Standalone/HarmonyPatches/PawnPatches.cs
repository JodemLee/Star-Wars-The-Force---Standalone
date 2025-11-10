using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheForce_Standalone.Apprenticeship;
using TheForce_Standalone.Darkside.SithSorcery;
using TheForce_Standalone.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TheForce_Standalone.HarmonyPatches
{
    internal class PawnPatches
    {
        [HarmonyPatch(typeof(PawnGenerator), "GenerateTraits")]
        public static class PawnGenerator_GenerateTraits_Patch
        {
            public static void Postfix(Pawn pawn, PawnGenerationRequest request)
            {
                try
                {
                    if (pawn.story == null || request.AllowedDevelopmentalStages.Newborn())
                        return;

                    var extension = pawn.kindDef.GetModExtension<ModExtension_TraitChances>();
                    if (extension?.traitChances == null)
                        return;

                    foreach (var traitChance in extension.traitChances)
                    {
                        if (traitChance.def == null || pawn.story.traits.HasTrait(traitChance.def))
                            continue;

                        if (request.KindDef.disallowedTraits?.Contains(traitChance.def) ?? false)
                            continue;

                        if (request.ProhibitedTraits?.Contains(traitChance.def) ?? false)
                            continue;

                        if (Rand.Value <= traitChance.chance)
                        {
                            int degree = traitChance.degree ?? 0;
                            pawn.story.traits.GainTrait(new Trait(traitChance.def, degree));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in PawnGenerator.GenerateTraits postfix: {ex}");
                }
            }
        }
    }

    [HarmonyPatch(typeof(PawnBioAndNameGenerator), "FillBackstorySlotShuffled")]
    public static class PawnBioAndNameGenerator_FillBackstorySlotShuffled_Patch
    {
        public static void Postfix(Pawn pawn, BackstorySlot slot)
        {
            try
            {
                if (slot == BackstorySlot.Adulthood)
                {
                    var childhoodExtension = pawn.story.Childhood?.GetModExtension<BackstoryModExtension>();
                    if (childhoodExtension?.availableBackstories != null && childhoodExtension.availableBackstories.Any())
                    {
                        pawn.story.Adulthood = childhoodExtension.availableBackstories.RandomElement();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in PawnBioAndNameGenerator.FillBackstorySlotShuffled postfix: {ex}");
            }
        }
    }


    [HarmonyPatch(typeof(Ability), "PreActivate")]
    public static class PatchAbility_DisplayIconInThoughtBubbleForEnemies
    {
        public static void Postfix(Ability __instance)
        {
            if (__instance.pawn.IsPlayerControlled || !__instance.pawn.IsPlayerControlled)
            {
                if (__instance.def.iconPath != null)
                {
                    MoteMaker.MakeThoughtBubble(__instance.pawn, __instance.def.iconPath, maintain: false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_AbilityTracker), "AICastableAbilities")]
    public static class PatchPawn_AbilityTracker_RandomizeOrder
    {
        public static void Postfix(ref List<Ability> __result, Pawn_AbilityTracker __instance, LocalTargetInfo target)
        {
            __result = __result.AsEnumerable().InRandomOrder().ToList();
        }
    }

    [HarmonyPatch(typeof(PawnRenderNode_AnimalPart), nameof(PawnRenderNode_AnimalPart.GraphicFor))]
    public static class Patch_ApplyWraithAppearance
    {
        static void Postfix(Pawn pawn, ref Graphic __result)
        {
            var comp = pawn?.TryGetComp<Comp_RandomAnimalAppearance>();
            if (comp?.CachedAnimalGraphic != null)
            {
                __result = comp.CachedAnimalGraphic;
            }
        }
    }


    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ChangeKind))]
    public static class Patch_ForcePawnKind
    {
        static bool Prefix(Pawn __instance, ref PawnKindDef newKindDef)
        {
            var forceUser = __instance.kindDef.GetModExtension<ModExtension_ForceUser>();

            if (forceUser == null)
                return true;

            return false;
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateNewPawnInternal")]
    public static class PawnGen_Patch
    {
        private static List<ThingDef> tmpGeneratedTechHediffsList = new List<ThingDef>();
        private static List<Thing> emptyIngredientsList = new List<Thing>();

        [HarmonyPostfix]
        public static void Postfix(Pawn __result)
        {
            if (__result == null || __result.kindDef == null)
                return;

            var extension = __result.kindDef.GetModExtension<ModExtension_TechHediffExtension>();
            if (extension == null || !extension.enableTechHediffs)
                return;


            if (__result.kindDef.techHediffsTags == null ||
                __result.kindDef.techHediffsChance <= 0f ||
                __result.kindDef.techHediffsMaxAmount <= 0)
                return;

            GenerateRandomTechHediffs(__result);
        }

        private static void GenerateRandomTechHediffs(Pawn pawn)
        {
            float remainingMoney = pawn.kindDef.techHediffsMoney.RandomInRange;
            int maxAttempts = pawn.kindDef.techHediffsMaxAmount;

            tmpGeneratedTechHediffsList.Clear();

            for (int i = 0; i < maxAttempts; i++)
            {
                if (Rand.Value > pawn.kindDef.techHediffsChance)
                    continue;

                var validHediffs = DefDatabase<ThingDef>.AllDefs
                    .Where(x => x.isTechHediff)
                    .Where(x => !tmpGeneratedTechHediffsList.Contains(x))
                    .Where(x => x.BaseMarketValue <= remainingMoney)
                    .Where(x => x.techHediffsTags != null)
                    .Where(x => pawn.kindDef.techHediffsTags.Any(tag => x.techHediffsTags.Contains(tag)))
                    .Where(x => !pawn.WorkTagIsDisabled(WorkTags.Violent) || !x.violentTechHediff)
                    .Where(x => pawn.kindDef.techHediffsDisallowTags == null ||
                               !pawn.kindDef.techHediffsDisallowTags.Any(tag => x.techHediffsTags.Contains(tag)))
                    .ToList();

                if (!validHediffs.Any())
                    continue;

                ThingDef chosenHediff = validHediffs.RandomElementByWeight(x => x.BaseMarketValue);
                remainingMoney -= chosenHediff.BaseMarketValue;

                InstallPart(pawn, chosenHediff);
                tmpGeneratedTechHediffsList.Add(chosenHediff);
            }
        }

        private static void InstallPart(Pawn pawn, ThingDef partDef)
        {
            IEnumerable<RecipeDef> source = DefDatabase<RecipeDef>.AllDefs.Where((RecipeDef x) =>
                x.IsIngredient(partDef) &&
                pawn.def.AllRecipes.Contains(x));

            if (source.Any())
            {
                RecipeDef recipeDef = source.RandomElement();
                if (!recipeDef.targetsBodyPart)
                {
                    recipeDef.Worker.ApplyOnPawn(pawn, null, null, emptyIngredientsList, null);
                }
                else if (recipeDef.Worker.GetPartsToApplyOn(pawn, recipeDef).Any())
                {
                    recipeDef.Worker.ApplyOnPawn(pawn,
                        recipeDef.Worker.GetPartsToApplyOn(pawn, recipeDef).RandomElement(),
                        null, emptyIngredientsList, null);
                }
            }
            else
            {
                CompProperties_UseEffectInstallImplant compProperties =
                    partDef.GetCompProperties<CompProperties_UseEffectInstallImplant>();
                if (compProperties != null)
                {
                    List<BodyPartRecord> partsWithDef = pawn.RaceProps.body.GetPartsWithDef(compProperties.bodyPart);
                    pawn.health.AddHediff(compProperties.hediffDef,
                        partsWithDef.NullOrEmpty() ? null : partsWithDef.RandomElement());
                }
            }
        }

        [HarmonyPatch(typeof(PawnGroupMakerUtility), nameof(PawnGroupMakerUtility.GeneratePawns))]
        public static class Patch_GeneratePawns
        {
            [HarmonyPostfix]
            public static void PostfixGeneratePawns(PawnGroupMakerParms parms, ref IEnumerable<Pawn> __result)
            {
                if (parms.faction?.def.GetModExtension<FactionExtension_ForceUsers>()?.notRecruitable != true)
                    return;

                var pawnList = __result.ToList();
                foreach (var pawn in pawnList)
                {
                    if (pawn.guest != null)
                    {
                        pawn.guest.Recruitable = false;
                    }
                }


                __result = pawnList;
            }
        }
    }

    [HarmonyPatch(typeof(PawnApparelGenerator), "PostProcessApparel")]
    public static class PawnApparelGenerator_PostProcessApparel_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Apparel apparel, Pawn pawn)
        {
            if (apparel == null || pawn == null || pawn.kindDef == null) return;

            // Get the mod extension
            var modExt = pawn.kindDef.GetModExtension<ModExtension_SyncedApparelColor>();
            if (modExt?.colorSyncRules == null) return;

            // Get all apparel that's already been processed
            var wornApparel = pawn.apparel?.WornApparel;
            if (wornApparel == null) return;

            // Check each rule to see if the new apparel matches source criteria
            foreach (var rule in modExt.colorSyncRules)
            {
                if (ApparelMatchesRule(apparel, rule, isSource: true) && rule.copyColorToTarget)
                {
                    // This apparel is a source, sync its stuff AND color to targets
                    SyncStuffAndColorToTargets(pawn, rule, apparel);
                }
                else if (ApparelMatchesRule(apparel, rule, isSource: false))
                {
                    // This apparel is a target, find source and sync its stuff AND color
                    SyncStuffAndColorFromSource(pawn, rule, apparel);
                }
            }
        }

        private static bool ApparelMatchesRule(Apparel apparel, ColorSyncRule rule, bool isSource)
        {
            if (apparel.def.apparel == null) return false;

            var targetLayer = isSource ? rule.sourceLayer : rule.targetLayer;
            var targetBodyPartGroup = isSource ? rule.sourceBodyPartGroup : rule.targetBodyPartGroup;

            // Check layer match
            bool layerMatch = targetLayer == null ||
                             (apparel.def.apparel.layers != null &&
                              apparel.def.apparel.layers.Contains(targetLayer));

            // Check body part group match
            bool bodyPartMatch = targetBodyPartGroup == null ||
                                (apparel.def.apparel.bodyPartGroups != null &&
                                 apparel.def.apparel.bodyPartGroups.Contains(targetBodyPartGroup));

            return layerMatch && bodyPartMatch;
        }

        private static void SyncStuffAndColorToTargets(Pawn pawn, ColorSyncRule rule, Apparel sourceApparel)
        {
            if (sourceApparel.Stuff == null) return; // No stuff to sync

            foreach (var targetApparel in pawn.apparel.WornApparel)
            {
                if (targetApparel != sourceApparel &&
                    ApparelMatchesRule(targetApparel, rule, isSource: false))
                {
                    // Sync STUFF first
                    if (targetApparel.Stuff != sourceApparel.Stuff)
                    {
                        targetApparel.SetStuffDirect(sourceApparel.Stuff);
                    }

                    // Sync COLOR second (will use the new stuff's color if not already set)
                    if (targetApparel.DrawColor != sourceApparel.DrawColor)
                    {
                        targetApparel.SetColor(sourceApparel.DrawColor, reportFailure: false);
                    }
                }
            }
        }

        private static void SyncStuffAndColorFromSource(Pawn pawn, ColorSyncRule rule, Apparel targetApparel)
        {
            var sourceApparel = FindSourceApparel(pawn, rule);
            if (sourceApparel != null && sourceApparel.Stuff != null)
            {
                // Sync STUFF first
                if (targetApparel.Stuff != sourceApparel.Stuff)
                {
                    targetApparel.SetStuffDirect(sourceApparel.Stuff);
                }

                // Sync COLOR second
                if (targetApparel.DrawColor != sourceApparel.DrawColor)
                {
                    targetApparel.SetColor(sourceApparel.DrawColor, reportFailure: false);
                }
            }
        }

        private static Apparel FindSourceApparel(Pawn pawn, ColorSyncRule rule)
        {
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                if (ApparelMatchesRule(apparel, rule, isSource: true))
                {
                    return apparel;
                }
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(Verb_CastAbility), "TryCastShot")]
    public static class Verb_CastAbility_TryCastShot_Patch
    {
        static bool Prefix(Verb_CastAbility __instance, ref bool __result)
        {
            try
            {
                Pawn targetPawn = __instance.CurrentTarget.Pawn;
                Pawn casterPawn = __instance.CasterPawn;

                if (targetPawn == null || casterPawn == null || targetPawn == casterPawn)
                {
                    return true;
                }

                AbilityDef abilityDef = __instance.ability.def;
                bool abilityHandled = false;

                // Process all reflection providers on the target
                ProcessReflectionProviders(targetPawn, (source, sourceDef, extension) =>
                {
                    if (abilityHandled) return;

                    if (extension.CanReflectAbility(abilityDef, targetPawn, casterPawn))
                    {
                        float reflectionChance = extension.GetReflectionChance(targetPawn);
                        float nullificationChance = extension.GetNullificationChance(targetPawn);

                        float roll = Rand.Value;

                        if (roll < nullificationChance)
                        {
                            ShowNullificationEffect(casterPawn, targetPawn, sourceDef, extension);
                            abilityHandled = true;
                        }
                        else if (roll < nullificationChance + reflectionChance)
                        {
                            if (ReflectAbility(__instance.ability, casterPawn, targetPawn, extension, sourceDef))
                            {
                                abilityHandled = true;
                            }
                        }
                    }
                });

                return !abilityHandled;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in reflection patch: {ex}");
                return true;
            }
        }

        private static void ProcessReflectionProviders(Pawn pawn, Action<object, Def, ModExtension_AbilityReflection> processor)
        {
            // Check apparel
            if (pawn.apparel != null)
            {
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    var extension = apparel.def.GetModExtension<ModExtension_AbilityReflection>();
                    if (extension != null)
                    {
                        processor(apparel, apparel.def, extension);
                    }
                }
            }

            // Check hediffs
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                var extension = hediff.def.GetModExtension<ModExtension_AbilityReflection>();
                if (extension != null)
                {
                    processor(hediff, hediff.def, extension);
                }
            }

            // Check genes
            if (pawn.genes != null)
            {
                foreach (var gene in pawn.genes.GenesListForReading)
                {
                    var extension = gene.def.GetModExtension<ModExtension_AbilityReflection>();
                    if (extension != null)
                    {
                        processor(gene, gene.def, extension);
                    }
                }
            }
        }

        private static bool ReflectAbility(Ability originalAbility, Pawn originalCaster, Pawn reflector,
            ModExtension_AbilityReflection extension, Def sourceDef)
        {
            try
            {
                // Create reflected ability
                Ability reflectedAbility = new Ability(reflector, originalAbility.def);

                // Show effects
                ShowReflectionEffect(originalCaster, reflector, extension, sourceDef);

                bool activationResult = reflectedAbility.Activate(new LocalTargetInfo(originalCaster), dest: default);
                return activationResult;
            }
            catch (Exception ex)
            {
                Log.Error($"Error reflecting ability: {ex}");
                return false;
            }
        }

        private static void ShowReflectionEffect(Pawn originalCaster, Pawn reflector,
            ModExtension_AbilityReflection extension, Def sourceDef)
        {
            // Message with source information
            string message = "AbilityReflected".Translate(originalCaster.LabelShort, reflector.LabelShort, sourceDef.label);
            Messages.Message(message, new LookTargets(originalCaster, reflector), MessageTypeDefOf.NeutralEvent);

            // Visual effect
            if (extension.reflectionEffect != null)
            {
                Effecter effecter = extension.reflectionEffect.Spawn();
                effecter.Trigger(new TargetInfo(reflector.PositionHeld, reflector.MapHeld),
                               new TargetInfo(originalCaster.PositionHeld, originalCaster.MapHeld));
                effecter.Cleanup();
            }

            // Sound
            if (extension.reflectionSound != null)
            {
                extension.reflectionSound?.PlayOneShot(new TargetInfo(reflector.PositionHeld, reflector.MapHeld));
            }
        }

        private static void ShowNullificationEffect(Pawn originalCaster, Pawn nullifier,
            Def sourceDef, ModExtension_AbilityReflection extension)
        {
            string message = "AbilityNullified".Translate(originalCaster.LabelShort, nullifier.LabelShort, sourceDef.label);
            Messages.Message(message, new LookTargets(originalCaster, nullifier), MessageTypeDefOf.NeutralEvent);

            if (extension.reflectionEffect != null)
            {
                Effecter effecter = extension.reflectionEffect.Spawn();
                effecter.Trigger(new TargetInfo(nullifier.PositionHeld, nullifier.MapHeld),
                               new TargetInfo(originalCaster.PositionHeld, originalCaster.MapHeld));
                effecter.Cleanup();
            }

            if (extension.reflectionSound != null)
            {
                extension.reflectionSound?.PlayOneShot(new TargetInfo(nullifier.PositionHeld, nullifier.MapHeld));
            }
        }
    }

}

