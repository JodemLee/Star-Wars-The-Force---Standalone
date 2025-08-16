using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TheForce_Standalone.Alignment;
using Verse;
using Verse.AI;

namespace TheForce_Standalone.HarmonyPatches
{
    public class Base : Mod
    {
        public static List<ThingDef> AllMeditationSpots;

        public Base(ModContentPack content) : base(content)
        {
            HarmonyPatches.harmonyPatch.Patch(
                AccessTools.Method(AccessTools.FirstInner(typeof(MeditationUtility),
                    type => type.Name.Contains("AllMeditationSpotCandidates") && typeof(IEnumerator).IsAssignableFrom(type)), "MoveNext"),
                transpiler: new HarmonyMethod(typeof(Base), nameof(Transpiler)));
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                AllMeditationSpots = new List<ThingDef> { ThingDefOf.MeditationSpot };
                foreach (var def in DefDatabase<ThingDef>.AllDefs)
                    if (def.HasModExtension<MeditationBuilding_Alignment>())
                        AllMeditationSpots.Add(def);
            });
        }

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
            list.Insert(idx1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Base), nameof(AllMeditationSpotsForPawn))));
            return list;
        }
    }
    [HarmonyPatch(typeof(JobDriver_Meditate), "MeditationTick")]
    public static class Patch_JobDriver_Meditate_MeditationTick
    {
        [HarmonyPrefix]
        public static void Prefix(JobDriver_Meditate __instance)
        {
            try
            {
                Pawn pawn = __instance.pawn;
                LocalTargetInfo spotTarget = __instance.job.GetTarget(TargetIndex.A);
                Thing meditationSpot = spotTarget.Thing;
                LocalTargetInfo focusTarget = __instance.Focus;
                Thing focus = focusTarget.Thing;
                Thing meditationTarget = meditationSpot ?? focus;

                if (ShouldSkipMeditation(__instance, meditationTarget))
                {
                    return;
                }

                HandleMeditationEffects(meditationTarget, pawn, focus);

                var forceUser = pawn.TryGetComp<CompClass_ForceUser>();
                float amount = MeditationUtility.PsyfocusGainPerTick(pawn, focus) * 60f;
                forceUser.Leveling.AddForceExperience(amount);
            }
            catch (Exception ex)
            {
            }
        }

        private static bool ShouldSkipMeditation(JobDriver_Meditate instance, Thing target)
        {
            return false;
        }

        private static void HandleMeditationEffects(Thing target, Pawn pawn, Thing focus)
        {
            if (target == null)
            {
                return;
            }

            var alignment = target.def.GetModExtension<MeditationBuilding_Alignment>();
            var forceUser = pawn.TryGetComp<CompClass_ForceUser>();
            if (alignment == null)
            {
                return;
            }

            float amount = MeditationUtility.PsyfocusGainPerTick(pawn, focus) * 60f;
            
            if (alignment.alignmenttoIncrease == AlignmentType.Darkside)
            {
                forceUser.Alignment.AddDarkSideAttunement(amount);
            }
            if (alignment.alignmenttoIncrease == AlignmentType.Lightside)
            {
                forceUser.Alignment.AddLightSideAttunement(amount);
            }

            if (alignment.alignmenttoDecrease == AlignmentType.Darkside)
            {
                forceUser.Alignment.RemoveDarkSideAttunement(amount);
            }

            if (alignment.alignmenttoDecrease == AlignmentType.Lightside)
            {
                forceUser.Alignment.RemoveLightSideAttunement(amount);
            }
        }

        //private static void HandleMeditationAnimation(Pawn pawn)
        //{
        //    try
        //    {
        //        AnimationDef meditationAnimation = DefDatabase<AnimationDef>.GetNamed("Force_FloatingMeditation");
        //        if (meditationAnimation == null)
        //        {
        //            return;
        //        }

        //        if (pawn.Drawer.renderer.CurAnimation != meditationAnimation)
        //        {
        //            pawn.Drawer.renderer.SetAnimation(meditationAnimation);
        //        }

        //        if (pawn.IsHashIntervalTick(100))
        //        {
        //            SpawnMeditationFlecks(pawn);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //}

        //private static void SpawnMeditationFlecks(Pawn pawn)
        //{
        //    var validCells = GenAdj.CellsAdjacent8Way(pawn)
        //        .Where(c => c.InBounds(pawn.Map) && c.Walkable(pawn.Map))
        //        .ToList();

        //    if (validCells.Count == 0)
        //    {
        //        return;
        //    }

        //    var fleckData = FleckMaker.GetDataStatic(
        //        validCells.RandomElement().ToVector3(),
        //        pawn.Map,
        //        ForceDefOf.Force_FleckStone,
        //        Rand.Range(1f, 2.5f));
        //    fleckData.rotation = Rand.Range(0f, 360f);
        //    pawn.Map.flecks.CreateFleck(fleckData);
        //}
    }

    [HarmonyPatch(typeof(JobDriver), "Cleanup")]
    public static class Patch_JobDriver_Notify_JobEnded
    {
        [HarmonyPostfix]
        public static bool Prefix(JobDriver __instance)
        {
            
            if (__instance is JobDriver_Meditate)
            {
                Pawn pawn = __instance.pawn;

                if (ModsConfig.RoyaltyActive && pawn.HasPsylink)
                {
                    
                    AnimationDef meditationAnimation = DefDatabase<AnimationDef>.GetNamed("Force_FloatingMeditation", false);

                    if (meditationAnimation != null && pawn.Drawer.renderer.CurAnimation == meditationAnimation)
                    {
                        
                        pawn.Drawer.renderer.SetAnimation(null);
                    }
                }
            }
            return true;
        }
    }
}
