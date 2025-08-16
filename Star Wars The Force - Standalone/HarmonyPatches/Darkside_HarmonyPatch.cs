using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using TheForce_Standalone.Alignment;
using Verse;

namespace TheForce_Standalone.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Darkside_Patch
    {
        public static void Prefix(Pawn __instance, DamageInfo? dinfo)
        {
            if (Force_ModSettings.IncreaseDarksideOnKill)
            {
                if (dinfo.HasValue && dinfo.Value.Instigator is Pawn attacker)
                {
                    if (__instance.RaceProps.Humanlike)
                    {
                        if (!attacker.HostileTo(__instance.Faction))
                        {
                            var comp = attacker.GetComp<CompClass_ForceUser>();
                            comp?.Alignment.AddDarkSideAttunement(0.1f);
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

            // Check if the thought has an alignment effect
            var alignmentExtension = newThought.def.GetModExtension<ThoughtAlignmentExtension>();
            if (alignmentExtension == null)
                return;

            var comp = __instance.pawn.GetComp<CompClass_ForceUser>();
            if (comp == null) return;

            // Apply the correct alignment based on extension
            switch (alignmentExtension.alignment)
            {
                case AlignmentType.Darkside:
                    comp.Alignment.AddDarkSideAttunement(alignmentExtension.alignmentIncrease);
                    break;
                case AlignmentType.Lightside:
                    comp.Alignment.AddLightSideAttunement(alignmentExtension.alignmentIncrease);
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(Faction), "Notify_MemberTookDamage")]
    public static class Notify_MemberTookDamage_Patch
    {
        public static void Postfix(Pawn member, DamageInfo dinfo)
        {
            if (!Force_ModSettings.IncreaseDarksideOnKill || dinfo.Instigator is not Pawn attacker || member.HostileTo(attacker))
            {
                return;
            }

            var comp = attacker.GetComp<CompClass_ForceUser>();
            comp?.Alignment.AddDarkSideAttunement(0.01f);
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_Slight), "RandomSelectionWeight")]
    public static class InteractionWorker_Slight_RandomSelectionWeight_Patch
    {
        public static void Postfix(Pawn initiator)
        {
            var comp = initiator.GetComp<CompClass_ForceUser>();
            comp?.Alignment.AddDarkSideAttunement(0.01f);
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_Insult), "RandomSelectionWeight")]
    public static class InteractionWorker_Insult_RandomSelectionWeight_Patch
    {
        public static void Postfix(Pawn initiator)
        {
            var comp = initiator.GetComp<CompClass_ForceUser>();
            comp?.Alignment.AddDarkSideAttunement(0.01f);
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
                float alignmentIncreaseIncrease = 0.01f * quality;

                var comp = doctor.GetComp<CompClass_ForceUser>();
                comp?.Alignment.AddLightSideAttunement(alignmentIncreaseIncrease);
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
                    List<Pawn> colonists = PawnsFinder.AllCaravansAndTravellingTransporters_Alive;
                    foreach (Pawn colonist in colonists)
                    {
                        var comp = colonist.GetComp<CompClass_ForceUser>();
                        comp?.Alignment.AddLightSideAttunement(0.01f);
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
            ModifyAlignmentOnOutcome(caravan, 0.3f);
        }

        private static void ModifyAlignmentOnOutcome(Caravan caravan, float alignmentIncreaseIncrease)
        {
            foreach (Pawn pawn in caravan.PawnsListForReading)
            {
                var comp = pawn.GetComp<CompClass_ForceUser>();
                comp?.Alignment.AddLightSideAttunement(alignmentIncreaseIncrease);
            }
        }
    }

    [HarmonyPatch(typeof(PeaceTalks))]
    [HarmonyPatch("Outcome_Triumph")]
    public static class PeaceTalks_Outcome_Triumph_Patch
    {
        public static void Postfix(Caravan caravan)
        {
            ModifyAlignmentOnOutcome(caravan, 0.8f);
        }

        private static void ModifyAlignmentOnOutcome(Caravan caravan, float alignmentIncreaseIncrease)
        {
            foreach (Pawn pawn in caravan.PawnsListForReading)
            {
                var comp = pawn.GetComp<CompClass_ForceUser>();
                comp?.Alignment.AddLightSideAttunement(alignmentIncreaseIncrease);
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







