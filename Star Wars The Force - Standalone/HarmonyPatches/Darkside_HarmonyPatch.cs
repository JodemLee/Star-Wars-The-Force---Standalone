using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using TheForce_Standalone.Alignment;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.HarmonyPatches
{
    // Add this helper class to avoid code duplication
    public static class AlignmentRecordTracker
    {
        public static void IncrementAlignmentRecord(Pawn pawn, AlignmentType alignmentType)
        {
            if (pawn?.records == null) return;

            if (alignmentType == AlignmentType.Darkside)
            {
                pawn.records.Increment(ForceDefOf.Force_DarksideActions);
            }
            else if (alignmentType == AlignmentType.Lightside)
            {
                pawn.records.Increment(ForceDefOf.Force_LightsideActions);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Darkside_Patch
    {
        public static void Prefix(Pawn __instance, DamageInfo? dinfo)
        {
            if (dinfo.HasValue && dinfo.Value.Instigator is Pawn attacker)
            {
                if (__instance.RaceProps.Humanlike)
                {
                    if (!attacker.HostileTo(__instance.Faction))
                    {
                        var comp = attacker.GetComp<CompClass_ForceUser>();
                        if (comp != null)
                        {
                            comp.Alignment.AddDarkSideAttunement(0.1f);
                            AlignmentRecordTracker.IncrementAlignmentRecord(attacker, AlignmentType.Darkside);
                            AlignmentActionLogger.Instance.LogAction(
                                attacker,
                                "Killing Non-Hostile",
                                AlignmentType.Darkside,
                                0.1f,
                                "Combat"
                            );
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(MemoryThoughtHandler))]
    [HarmonyPatch("TryGainMemory")]
    [HarmonyPatch(new Type[] { typeof(Thought_Memory), typeof(Pawn) })]
    public static class MemoryThoughtHandler_TryGainMemory_Patch
    {
        public static void Postfix(Thought_Memory newThought, MemoryThoughtHandler __instance)
        {
            if (newThought?.def == null || __instance?.pawn == null)
                return;

            if (__instance.GetFirstMemoryOfDef(newThought.def) != null)
                return;

            var alignmentExtension = newThought.def.GetModExtension<ThoughtAlignmentExtension>();
            if (alignmentExtension == null)
                return;

            var comp = __instance.pawn.GetComp<CompClass_ForceUser>();
            if (comp == null) return;

            string thoughtName = newThought.def.LabelCap;

            switch (alignmentExtension.alignment)
            {
                case AlignmentType.Darkside:
                    comp.Alignment.AddDarkSideAttunement(alignmentExtension.alignmentIncrease);
                    AlignmentRecordTracker.IncrementAlignmentRecord(__instance.pawn, AlignmentType.Darkside);
                    AlignmentActionLogger.Instance.LogAction(
                        __instance.pawn,
                        thoughtName,
                        AlignmentType.Darkside,
                        alignmentExtension.alignmentIncrease,
                        "Thought"
                    );
                    break;
                case AlignmentType.Lightside:
                    comp.Alignment.AddLightSideAttunement(alignmentExtension.alignmentIncrease);
                    AlignmentRecordTracker.IncrementAlignmentRecord(__instance.pawn, AlignmentType.Lightside);
                    AlignmentActionLogger.Instance.LogAction(
                        __instance.pawn,
                        thoughtName,
                        AlignmentType.Lightside,
                        alignmentExtension.alignmentIncrease,
                        "Thought"
                    );
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(Faction), "Notify_MemberTookDamage")]
    public static class Notify_MemberTookDamage_Patch
    {
        public static void Postfix(Pawn member, DamageInfo dinfo)
        {
            if (dinfo.Instigator == null || dinfo.Instigator.Faction == null)
                return;

            if (dinfo.Instigator is not Pawn attacker)
                return;

            if (!dinfo.Def.ExternalViolenceFor(member))
                return;

            if (member.Faction?.HostileTo(dinfo.Instigator.Faction) == true)
                return;

            if (member.InAggroMentalState ||
                attacker.InAggroMentalState ||
                (member.InMentalState && member.MentalStateDef.IsExtreme && member.MentalStateDef.category == MentalStateCategory.Malicious) ||
                PrisonBreakUtility.IsPrisonBreaking(member) ||
                member.IsQuestHelper() ||
                SlaveRebellionUtility.IsRebelling(attacker) ||
                member.IsSlaveOfColony)
                return;

            if (IsMutuallyHostileCrossfire(dinfo, member, attacker))
                return;

            var comp = attacker.GetComp<CompClass_ForceUser>();
            if (comp != null)
            {
                float damageAmount = Mathf.Min(100f, dinfo.Amount);
                float alignmentChange = 0.01f * (damageAmount / 100f);

                comp.Alignment.AddDarkSideAttunement(alignmentChange);
                AlignmentRecordTracker.IncrementAlignmentRecord(attacker, AlignmentType.Darkside);
                AlignmentActionLogger.Instance.LogAction(
                    attacker,
                    "Harming Ally",
                    AlignmentType.Darkside,
                    alignmentChange,
                    "Combat"
                );
            }
        }

        private static bool IsMutuallyHostileCrossfire(DamageInfo dinfo, Pawn member, Pawn attacker)
        {
            if (attacker.mindState?.enemyTarget != null && member.mindState?.enemyTarget != null)
            {
                if (attacker.mindState.enemyTarget == member.mindState.enemyTarget)
                    return true;

                // Check if both are hostile to the same faction
                var attackerTargetFaction = attacker.mindState.enemyTarget.Faction;
                var memberTargetFaction = member.mindState.enemyTarget.Faction;

                if (attackerTargetFaction != null && memberTargetFaction != null &&
                    attackerTargetFaction == memberTargetFaction)
                    return true;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_Slight), "RandomSelectionWeight")]
    public static class InteractionWorker_Slight_RandomSelectionWeight_Patch
    {
        public static void Postfix(Pawn initiator, Pawn recipient)
        {
            var comp = initiator.GetComp<CompClass_ForceUser>();
            if (comp != null)
            {
                comp.Alignment.AddDarkSideAttunement(0.01f);
                AlignmentRecordTracker.IncrementAlignmentRecord(initiator, AlignmentType.Darkside);
                AlignmentActionLogger.Instance.LogAction(
                    initiator,
                    "Social Slight",
                    AlignmentType.Darkside,
                    0.01f,
                    "Social"
                );
            }
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_Insult), "RandomSelectionWeight")]
    public static class InteractionWorker_Insult_RandomSelectionWeight_Patch
    {
        public static void Postfix(Pawn initiator, Pawn recipient)
        {
            var comp = initiator.GetComp<CompClass_ForceUser>();
            if (comp != null)
            {
                comp.Alignment.AddDarkSideAttunement(0.01f);
                AlignmentRecordTracker.IncrementAlignmentRecord(initiator, AlignmentType.Darkside);
                AlignmentActionLogger.Instance.LogAction(
                    initiator,
                    "Insult",
                    AlignmentType.Darkside,
                    0.01f,
                    "Social"
                );
            }
        }
    }

    [HarmonyPatch(typeof(TendUtility))]
    [HarmonyPatch("DoTend")]
    public static class TendUtility_DoTend_Patch
    {
        public static void Postfix(Pawn doctor, Pawn patient, Medicine medicine)
        {
            if (doctor != null)
            {
                float quality = TendUtility.CalculateBaseTendQuality(doctor, patient, medicine?.def);
                float alignmentIncrease = 0.01f * quality;

                var comp = doctor.GetComp<CompClass_ForceUser>();
                if (comp != null)
                {
                    comp.Alignment.AddLightSideAttunement(alignmentIncrease);
                    AlignmentRecordTracker.IncrementAlignmentRecord(doctor, AlignmentType.Lightside);
                    AlignmentActionLogger.Instance.LogAction(
                        doctor,
                        "Healing",
                        AlignmentType.Lightside,
                        alignmentIncrease,
                        "Medical"
                    );
                }
            }
        }
    }

    [HarmonyPatch(typeof(IdeoUtility))]
    [HarmonyPatch("Notify_QuestCleanedUp")]
    public static class QuestManager_Notify_QuestCleanedUp_Patch
    {
        public static void Postfix(Quest quest, QuestState state)
        {
            if (quest != null && quest.charity && ModsConfig.IdeologyActive)
            {
                if (state == QuestState.EndedSuccess)
                {
                    List<Pawn> colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists;
                    foreach (Pawn colonist in colonists)
                    {
                        var comp = colonist.GetComp<CompClass_ForceUser>();
                        if (comp != null)
                        {
                            comp.Alignment.AddLightSideAttunement(0.01f);
                            AlignmentRecordTracker.IncrementAlignmentRecord(colonist, AlignmentType.Lightside);
                            AlignmentActionLogger.Instance.LogAction(
                                colonist,
                                "Charity Quest",
                                AlignmentType.Lightside,
                                0.01f,
                                "Quest"
                            );
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PeaceTalks))]
    [HarmonyPatch("Outcome_Success")]
    public static class PeaceTalks_Outcome_Success_Patch
    {
        public static void Postfix(Caravan caravan)
        {
            ModifyAlignmentOnOutcome(caravan, 0.3f, "Peace Talks Success");
        }

        private static void ModifyAlignmentOnOutcome(Caravan caravan, float alignmentIncrease, string actionName)
        {
            foreach (Pawn pawn in caravan.PawnsListForReading)
            {
                var comp = pawn.GetComp<CompClass_ForceUser>();
                if (comp != null)
                {
                    comp.Alignment.AddLightSideAttunement(alignmentIncrease);
                    AlignmentRecordTracker.IncrementAlignmentRecord(pawn, AlignmentType.Lightside);
                    AlignmentActionLogger.Instance.LogAction(
                        pawn,
                        actionName,
                        AlignmentType.Lightside,
                        alignmentIncrease,
                        "Diplomacy"
                    );
                }
            }
        }
    }

    [HarmonyPatch(typeof(PeaceTalks))]
    [HarmonyPatch("Outcome_Triumph")]
    public static class PeaceTalks_Outcome_Triumph_Patch
    {
        public static void Postfix(Caravan caravan)
        {
            ModifyAlignmentOnOutcome(caravan, 0.8f, "Peace Talks Triumph");
        }

        private static void ModifyAlignmentOnOutcome(Caravan caravan, float alignmentIncrease, string actionName)
        {
            foreach (Pawn pawn in caravan.PawnsListForReading)
            {
                var comp = pawn.GetComp<CompClass_ForceUser>();
                if (comp != null)
                {
                    comp.Alignment.AddLightSideAttunement(alignmentIncrease);
                    AlignmentRecordTracker.IncrementAlignmentRecord(pawn, AlignmentType.Lightside);
                    AlignmentActionLogger.Instance.LogAction(
                        pawn,
                        actionName,
                        AlignmentType.Lightside,
                        alignmentIncrease,
                        "Diplomacy"
                    );
                }
            }
        }
    }

    [HarmonyPatch(typeof(MechanitorUtility), "ShouldBeMechanitor")]
    public static class ShouldBeMechanitor_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result, Pawn pawn)
        {
            if (pawn.health.hediffSet.HasHediff(ForceDefOf.Force_MechuLinkImplant))
            {
                __result = true;
            }
        }
    }
}