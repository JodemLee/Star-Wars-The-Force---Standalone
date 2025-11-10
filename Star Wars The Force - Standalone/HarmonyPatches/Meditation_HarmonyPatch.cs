using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TheForce_Standalone.Alignment;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TheForce_Standalone.HarmonyPatches
{
 
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
                Log.Error("$Error in MeditationTick - Force Standalone" + $"{ex}");
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

    [HarmonyPatch(typeof(ThoughtWorker_PassionateWork), "CurrentStateInternal")]
    public static class Patch_ThoughtWorker_PassionateWork
    {
        [HarmonyPostfix]
        public static void Postfix(ThoughtState __result, Pawn p)
        {
            try
            {
                if (!__result.Active || p.jobs?.curDriver == null)
                    return;
                SkillDef activeSkill = p.jobs.curDriver.ActiveSkill;
                if (activeSkill == null)
                    return;
                SkillRecord skill = p.skills?.GetSkill(activeSkill);
                if (skill == null)
                    return;
                var forceUser = p.TryGetComp<CompClass_ForceUser>();
                if (forceUser == null)
                    return;
                float xpAmount = CalculatePassionXP(__result, skill);
                forceUser.Leveling.AddForceExperience(xpAmount);
            }
            catch (System.Exception ex)
            {
                Log.Error($"Error in PassionateWork postfix: {ex.Message}");
            }
        }

        private static float CalculatePassionXP(ThoughtState thoughtState, SkillRecord skill)
        {
            float baseXP = 0.1f; // Base XP per tick
            if (skill.passion == Passion.Major)
            {
                baseXP *= 2f;
            }

            if (thoughtState.StageIndex == 1)
            {
                baseXP *= 1.5f;
            }

            return baseXP;
        }
    }

    //[HarmonyPatch(typeof(JobDriver), "DriverTick")]
    //public static class Patch_JobDriver_Wait_Tick
    //{
    //    [HarmonyPrefix]
    //    public static void Prefix(JobDriver __instance)
    //    {
    //        try
    //        {
    //            // Check if this is a Wait job and specifically the chicken dance job
    //            if (__instance is JobDriver_Wait && __instance.job.def.defName == "Force_ChickenDance")
    //            {
    //                HandleDanceAnimation(__instance.pawn);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            // Consider logging the exception for debugging
    //        }
    //    }

    //    private static void HandleDanceAnimation(Pawn pawn)
    //    {
    //        AnimationDef danceAnimation = DefDatabase<AnimationDef>.GetNamed("Force_ChickenDance");
    //        if (danceAnimation == null)
    //        {
    //            return;
    //        }

    //        if (pawn.Drawer.renderer.CurAnimation != danceAnimation)
    //        {
    //            pawn.Drawer.renderer.SetAnimation(danceAnimation);
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(JobDriver), "Cleanup")]
    //public static class Patch_JobDriver_Notify_JobEndedChickenDance
    //{
    //    [HarmonyPostfix]
    //    public static void Postfix(JobDriver __instance, JobCondition condition)
    //    {
    //        try
    //        {
    //            // Check if this is a Wait job and specifically the chicken dance job
    //            if (__instance is JobDriver_Wait && __instance.job.def.defName == "Force_ChickenDance")
    //            {
    //                Pawn pawn = __instance.pawn;
    //                AnimationDef danceAnimation = DefDatabase<AnimationDef>.GetNamed("Force_ChickenDance", false);

    //                if (danceAnimation != null && pawn.Drawer.renderer.CurAnimation == danceAnimation)
    //                {
    //                    pawn.Drawer.renderer.SetAnimation(null);
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            // Consider logging the exception for debugging
    //        }
    //    }
    //}
}
