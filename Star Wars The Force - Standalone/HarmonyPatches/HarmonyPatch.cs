using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TheForce_Standalone.Alignment;
using TheForce_Standalone.Generic;
using TheForce_Standalone.HediffComps;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.HarmonyPatches
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        public static Harmony harmonyPatch;
        static HarmonyPatches()
        {
            harmonyPatch = new Harmony("Standalone_TheForce");
            var type = typeof(HarmonyPatches);
            harmonyPatch.Patch(
                AccessTools.Method(AccessTools.FirstInner(typeof(MeditationUtility),
                    type => type.Name.Contains("AllMeditationSpotCandidates") && typeof(IEnumerator).IsAssignableFrom(type)), "MoveNext"),
                transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(Transpiler)));
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                AllMeditationSpots = new List<ThingDef> { ThingDefOf.MeditationSpot };
                foreach (var def in DefDatabase<ThingDef>.AllDefs)
                    if (def.HasModExtension<MeditationBuilding_Alignment>())
                        AllMeditationSpots.Add(def);
            });
            harmonyPatch.Patch(original: AccessTools.PropertyGetter(typeof(ShaderTypeDef), nameof(ShaderTypeDef.Shader)),
               prefix: new HarmonyMethod(typeof(TheForce_Mod),
                   nameof(TheForce_Mod.ShaderFromAssetBundle)));
            harmonyPatch.PatchAll();
        }

        public static List<ThingDef> AllMeditationSpots;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<ThingDef, IEnumerable<Thing>> AllOnMapOfPawnWithDef(Pawn pawn) => def => pawn.Map.listerBuildings.AllBuildingsColonistOfDef(def);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<Thing> AllMeditationSpotsForPawn(Pawn pawn) => AllMeditationSpots.SelectMany(AllOnMapOfPawnWithDef(pawn));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();
            var info1 = AccessTools.Method(typeof(ListerBuildings), nameof(ListerBuildings.AllBuildingsColonistOfDef));
            var idx1 = list.FindIndex(ins => ins.Calls(info1)) - 3;
            list.RemoveRange(idx1, 4);
            list.Insert(idx1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches), nameof(AllMeditationSpotsForPawn))));
            return list;
        }
    }


    

    [HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAndApparelExtras")]
    public static class Patch_DrawEquipmentAndApparelExtras
    {
        public static void Postfix(Pawn pawn, Vector3 drawPos, Rot4 facing, PawnRenderFlags flags)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
                return;

            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                HediffComp_Shield shieldComp = hediff.TryGetComp<HediffComp_Shield>();
                if (shieldComp != null)
                {
                    shieldComp.DrawWornExtras();
                }
                HediffComp_ThingHolder holderComp = hediff.TryGetComp<HediffComp_ThingHolder>();
                if (holderComp != null)
                {
                    holderComp.DrawWornExtras();
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


    [HarmonyPatch(typeof(Verb_AbilityShoot))]
    [HarmonyPatch("TryCastShot")]
    public static class Verb_AbilityShoot_TryCastShot_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Verb_AbilityShoot __instance, ref bool __result)
        {
            if (__result && __instance.Ability.CompOfType<CompAbilityEffect_ForcePower>() != null)
            {
                bool abilitySuccess = __instance.Ability.Activate(__instance.CurrentTarget, __instance.CurrentDestination);
                if (!abilitySuccess)
                {
                    __result = false;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Projectile), "ImpactSomething")]
    public static class Patch_Projectile_ImpactSomething_Catch
    {
        public static bool Prefix(Projectile __instance)
        {
            try
            {
                if (__instance == null) return true;

                var targetThing = __instance.usedTarget.Thing;
                if (!(targetThing is Pawn pawn)) return true;

                var thingHolderHediffs = pawn.health?.hediffSet?.hediffs
                    .OfType<HediffWithComps>()
                    .Where(h => h.TryGetComp<HediffComp_ProjectileHolder>() != null);

                if (thingHolderHediffs != null)
                {
                    foreach (var hediff in thingHolderHediffs)
                    {
                        var comp = hediff.TryGetComp<HediffComp_ProjectileHolder>();
                        if (comp != null && comp.Props.catchProjectile)
                        {
                            if (comp.TryCatchProjectile(__instance))
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in Projectile.ImpactSomething catch patch: {ex}");
                return true;
            }
        }
    }
}



