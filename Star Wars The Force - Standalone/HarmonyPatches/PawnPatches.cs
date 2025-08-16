using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using TheForce_Standalone.Darkside.SithSorcery;
using TheForce_Standalone.Generic;
using Verse;

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

    [HarmonyPatch(typeof(Ability), "PreActivate")]
    public static class PatchAbility_DisplayIconInThoughtBubbleForEnemies
    {
        public static void Postfix(Ability __instance)
        {
            if (__instance.pawn.IsPlayerControlled || !__instance.pawn.IsPlayerControlled)
            {
                MoteMaker.MakeThoughtBubble(__instance.pawn, __instance.def.iconPath, maintain: false);
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
    }
}
