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
                {
                    var comp = p.GetComp<CompClass_ForceUser>();
                    return comp?.Apprenticeship != null &&
                           comp.Apprenticeship.apprentices.Count < comp.Apprenticeship.apprenticeCapacity &&
                           p.abilities?.abilities?.Any(a => a.def == ForceDefOf.Force_Apprenticeship) == true;
                })
                .OrderByDescending(p => p.GetComp<CompClass_ForceUser>().forceLevel)
                .ToList();

            var existingMasters = forceUsers
                .Where(p =>
                {
                    var comp = p.GetComp<CompClass_ForceUser>();
                    return comp?.Apprenticeship?.apprentices?.Count > 0;
                })
                .ToList();

            var allMasters = potentialMasters.Concat(existingMasters).Distinct().ToList();

            var potentialApprentices = forceUsers
                .Where(p =>
                {
                    var comp = p.GetComp<CompClass_ForceUser>();
                    return comp?.Apprenticeship?.master == null;
                })
                .OrderBy(p => p.GetComp<CompClass_ForceUser>().forceLevel)
                .ToList();

            foreach (var master in allMasters)
            {
                var masterComp = master.GetComp<CompClass_ForceUser>();
                if (masterComp?.Apprenticeship == null) continue;

                bool isExistingMaster = masterComp.Apprenticeship.apprentices.Count > 0;
                bool hasApprenticeshipAbility = master.abilities?.abilities?.Any(a => a.def == ForceDefOf.Force_Apprenticeship) == true;

                if (!isExistingMaster && !hasApprenticeshipAbility)
                    continue;

                var eligibleApprentices = potentialApprentices
                    .Where(a =>
                    {
                        var apprenticeComp = a.GetComp<CompClass_ForceUser>();
                        return apprenticeComp != null &&
                               apprenticeComp.forceLevel <= (masterComp.forceLevel - 2) &&
                               apprenticeComp.Apprenticeship.master == null;
                    })
                    .ToList();

                if (eligibleApprentices.Count == 0)
                    continue;

                var apprentice = eligibleApprentices
                    .OrderBy(a => masterComp.forceLevel - a.GetComp<CompClass_ForceUser>().forceLevel)
                    .First();

                potentialApprentices.Remove(apprentice);

                // Set up the apprenticeship relationship
                var apprenticeComp = apprentice.GetComp<CompClass_ForceUser>();
                if (apprenticeComp?.Apprenticeship != null)
                {
                    masterComp.Apprenticeship.apprentices.Add(apprentice);
                    apprenticeComp.Apprenticeship.master = master;

                    // Add reciprocal relations
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
                if (comp != null && comp.IsValidForceUser && comp.forceLevel <= 1)
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

        // Helper methods for backwards compatibility
        public static bool IsMaster(Pawn pawn)
        {
            var comp = pawn?.GetComp<CompClass_ForceUser>();
            return comp?.Apprenticeship?.apprentices?.Count > 0;
        }

        public static bool IsApprentice(Pawn pawn)
        {
            var comp = pawn?.GetComp<CompClass_ForceUser>();
            return comp?.Apprenticeship?.master != null;
        }

        public static Pawn GetMaster(Pawn apprentice)
        {
            var comp = apprentice?.GetComp<CompClass_ForceUser>();
            return comp?.Apprenticeship?.master;
        }

        public static IEnumerable<Pawn> GetApprentices(Pawn master)
        {
            var comp = master?.GetComp<CompClass_ForceUser>();
            return comp?.Apprenticeship?.apprentices ?? Enumerable.Empty<Pawn>();
        }

        public static bool CanBecomeMaster(Pawn pawn)
        {
            var comp = pawn?.GetComp<CompClass_ForceUser>();
            return comp?.IsValidForceUser == true &&
                   comp.forceLevel >= 3 && // Minimum level to be a master
                   comp.Pawn?.abilities.GetAbility(ForceDefOf.Force_Apprenticeship) != null &&
                   (comp.Apprenticeship?.apprentices?.Count ?? 0) < (comp.Apprenticeship?.apprenticeCapacity ?? 0);
        }

        public static bool CanBecomeApprentice(Pawn pawn, Pawn potentialMaster = null)
        {
            var comp = pawn?.GetComp<CompClass_ForceUser>();
            if (comp?.IsValidForceUser != true || comp.Apprenticeship?.master != null)
                return false;

            if (potentialMaster != null)
            {
                var masterComp = potentialMaster.GetComp<CompClass_ForceUser>();
                return masterComp?.forceLevel > comp.forceLevel + 1 && // Master should be at least 2 levels higher
                       (masterComp.Apprenticeship?.apprentices?.Count ?? 0) < (masterComp.Apprenticeship?.apprenticeCapacity ?? 0);
            }

            return true;
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
        public bool notRecruitable = false;
    }
}