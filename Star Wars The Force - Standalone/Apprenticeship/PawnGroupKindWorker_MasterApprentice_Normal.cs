using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Apprenticeship
{
    public static class MasterApprenticeUtility
    {
        public static void TryCreateMasterApprenticePairs(List<Pawn> pawns, Faction faction)
        {
            if (pawns.Count == 0)
                return;

            // First ensure all force users have their levels initialized
            InitializeforceLevels(pawns);

            var forceUsers = pawns.Where(p =>
            {
                var comp = p.TryGetComp<CompClass_ForceUser>();
                return comp?.IsValidForceUser == true && comp.forceLevel > 0;
            }).ToList();

            if (forceUsers.Count < 2)
                return;

            var potentialMasters = forceUsers
                .Where(p =>
                    !p.health.hediffSet.HasHediff(ForceDefOf.Force_Master) &&
                    p.abilities?.abilities?.Any(a => a.def == ForceDefOf.Force_Apprenticeship) == true)
                .OrderByDescending(p => p.GetComp<CompClass_ForceUser>().forceLevel)
                .ToList();

            // Check for existing masters who might have lost their ability but should keep their status
            var existingMasters = forceUsers
                .Where(p => p.health.hediffSet.HasHediff(ForceDefOf.Force_Master))
                .ToList();

            // Combine potential new masters with existing masters
            var allMasters = potentialMasters.Concat(existingMasters).Distinct().ToList();

            var potentialApprentices = forceUsers
                .Where(p => !p.health.hediffSet.HasHediff(ForceDefOf.Force_Apprentice))
                .OrderBy(p => p.GetComp<CompClass_ForceUser>().forceLevel)
                .ToList();

            foreach (var master in allMasters)
            {
                var masterComp = master.GetComp<CompClass_ForceUser>();
                if (masterComp == null) continue;

                // Existing masters keep their status even if they lost the ability
                bool isExistingMaster = master.health.hediffSet.HasHediff(ForceDefOf.Force_Master);
                bool hasApprenticeshipAbility = master.abilities?.abilities?.Any(a => a.def == ForceDefOf.Force_Apprenticeship) == true;

                if (!isExistingMaster && !hasApprenticeshipAbility)
                    continue;

                // Find apprentices at least 2 levels lower than master
                var eligibleApprentices = potentialApprentices
                    .Where(a =>
                    {
                        var apprenticeComp = a.GetComp<CompClass_ForceUser>();
                        return apprenticeComp != null &&
                               apprenticeComp.forceLevel <= (masterComp.forceLevel - 2);
                    })
                    .ToList();

                if (eligibleApprentices.Count == 0)
                    continue;

                var masterHediff = master.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Master) as Hediff_Master
                    ?? HediffMaker.MakeHediff(ForceDefOf.Force_Master, master) as Hediff_Master;

                if (masterHediff == null) continue;

                if (!isExistingMaster)
                {
                    master.health.AddHediff(masterHediff);
                    masterHediff.ChangeApprenticeCapacitySetting(Force_ModSettings.apprenticeCapacity);
                }

                // Select closest level apprentice (but still lower level)
                var apprentice = eligibleApprentices
                    .OrderBy(a => masterComp.forceLevel - a.GetComp<CompClass_ForceUser>().forceLevel)
                    .First();

                potentialApprentices.Remove(apprentice);

                // Create apprentice relationship
                var apprenticeHediff = HediffMaker.MakeHediff(ForceDefOf.Force_Apprentice, apprentice) as Hediff_Apprentice;
                if (apprenticeHediff != null)
                {
                    apprenticeHediff.master = master;
                    apprentice.health.AddHediff(apprenticeHediff);
                    masterHediff.apprentices.Add(apprentice);

                    if (!master.relations.DirectRelationExists(ForceDefOf.Force_ApprenticeRelation, apprentice))
                        master.relations.AddDirectRelation(ForceDefOf.Force_ApprenticeRelation, apprentice);

                    if (!apprentice.relations.DirectRelationExists(ForceDefOf.Force_MasterRelation, master))
                        apprentice.relations.AddDirectRelation(ForceDefOf.Force_MasterRelation, master);
                }
            }
        }
        private static void InitializeforceLevels(List<Pawn> pawns)
        {
            foreach (var pawn in pawns)
            {
                var comp = pawn.TryGetComp<CompClass_ForceUser>();
                if (comp != null && comp.IsValidForceUser && comp.forceLevel <= 0)
                {
                    var forceUserExt = pawn.kindDef?.GetModExtension<ModExtension_ForceUser>();
                    if (forceUserExt != null)
                    {
                        comp.forceLevel = forceUserExt.forceLevelRange.RandomInRange;
                        comp.RecalculateMaxFP();
                        comp.RecoverFP(comp.MaxFP);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PawnGroupMakerUtility), nameof(PawnGroupMakerUtility.GeneratePawns))]
    public static class Patch_GeneratePawns
    {
        [HarmonyPostfix]
        public static void PostfixGeneratePawns(PawnGroupMakerParms parms, ref IEnumerable<Pawn> __result)
        {
            if (parms.faction?.def.GetModExtension<FactionExtension_ForceUsers>()?.enableMasterApprenticeSystem != true)
                return;

            var pawnList = __result.ToList();
            MasterApprenticeUtility.TryCreateMasterApprenticePairs(pawnList, parms.faction);
            __result = pawnList;
        }
    }

    public class FactionExtension_ForceUsers : DefModExtension
    {
        public bool enableMasterApprenticeSystem = true;
    }
}