using HarmonyLib;
using RimWorld;
using TheForce_Standalone.Generic;
using TheForce_Standalone.HediffComps;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.HarmonyPatches
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            harmonyPatch = new Harmony("Standalone_TheForce");
            var type = typeof(HarmonyPatches);
            harmonyPatch.PatchAll();
        }

        public static Harmony harmonyPatch;


        [HarmonyPatch(typeof(TraitSet), "GainTrait")]
        public static class TraitSet_GainTraitForceSensitivity
        {
            public static void Postfix(Pawn ___pawn, Trait trait)
            {

                if ((___pawn.story?.traits?.HasTrait(TraitDef.Named("Force_NeutralSensitivity")) ?? false)
                    || (___pawn.story?.traits?.HasTrait(TraitDef.Named("Force_LightAffinity")) ?? false)
                    || (___pawn.story?.traits?.HasTrait(TraitDef.Named("Force_DarkAffinity")) ?? false))
                {
                    int traitDegree = trait.Degree;
                    if (traitDegree >= 0)
                    {
                        var forceUserComp = ___pawn.TryGetComp<CompClass_ForceUser>();
                        forceUserComp.RecalculateMaxFP();
                        forceUserComp.Abilities.AddAbilityPoint(1);
                    }
                }
            }

        }




        [HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAndApparelExtras")]
        public static class Patch_DrawEquipmentAndApparelExtras
        {
            public static void Postfix(Pawn pawn, Vector3 drawPos, Rot4 facing, PawnRenderFlags flags)
            {
                if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
                    return;

                // Get all shield hediffs on the pawn
                foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                {
                    HediffComp_Shield shieldComp = hediff.TryGetComp<HediffComp_Shield>();
                    if (shieldComp != null)
                    {
                        shieldComp.DrawWornExtras();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_HealthTracker), "PreApplyDamage")]
        public static class Patch_PreApplyDamage
        {
            public static bool Prefix(Pawn_HealthTracker __instance, DamageInfo dinfo, out bool __state, out bool absorbed)
            {
                __state = false;
                absorbed = false;

                if (__instance.hediffSet?.hediffs != null)
                {
                    foreach (Hediff hediff in __instance.hediffSet.hediffs)
                    {
                        HediffComp_Shield shieldComp = hediff.TryGetComp<HediffComp_Shield>();
                        if (shieldComp != null && shieldComp.CheckPreAbsorbDamage(dinfo))
                        {
                            absorbed = true;
                            __state = true;
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Ideo), "SetIcon")]
        public static class Patch_Ideo_SetIcon
        {
            [HarmonyPostfix]
            public static void PostFix(Ideo __instance)
            {
                if (__instance.culture != null && __instance.culture.HasModExtension<DefCultureExtension>())
                {

                    var ext = __instance.culture.GetModExtension<DefCultureExtension>();

                    if (ext.ideoIconDef != null)
                    {
                        __instance.iconDef = ext.ideoIconDef;
                    }
                    if (ext.ideoIconColor != null)
                    {
                        __instance.colorDef = ext.ideoIconColor;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Faction), nameof(Faction.TryMakeInitialRelationsWith))]
        public static class Faction_TryMakeInitialRelationsWith_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Faction __instance, Faction other)
            {
                if (__instance.HostileTo(other)) return;

                var ext1 = __instance.def.GetModExtension<ModExtension_FactionExtension>();
                var ext2 = other.def.GetModExtension<ModExtension_FactionExtension>();

                bool shouldBeHostile =
                    (ext1?.permanentEnemyFactions?.Contains(other.def) == true ||
                    (ext2?.permanentEnemyFactions?.Contains(__instance.def) == true));

                if (shouldBeHostile)
                {
                    FactionRelation relation = __instance.RelationWith(other, true);
                    relation.baseGoodwill = -100;
                    relation.kind = FactionRelationKind.Hostile;
                    FactionRelation otherRelation = other.RelationWith(__instance, true);
                    otherRelation.baseGoodwill = -100;
                    otherRelation.kind = FactionRelationKind.Hostile;
                }
            }
        }
    }
}


