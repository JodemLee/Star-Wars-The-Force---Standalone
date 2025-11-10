using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheForce_Standalone
{
    public static class ForceSensitivityUtils
    {
        private static readonly HashSet<Pawn> ForceUserCache = new HashSet<Pawn>();
        private static readonly HashSet<Pawn> NonForceUserCache = new HashSet<Pawn>();

        // Pre-cached defs for fast lookup
        public static readonly HashSet<TraitDef> ForceTraitDefs = new HashSet<TraitDef>();
        public static readonly HashSet<HediffDef> ForceHediffDefs = new HashSet<HediffDef>();
        public static readonly HashSet<GeneDef> ForceGeneDefs = new HashSet<GeneDef>();
        public static readonly HashSet<PawnKindDef> ForcePawnKindDefs = new HashSet<PawnKindDef>();

        [StaticConstructorOnStartup]
        public static class StaticInitializer
        {
            static StaticInitializer()
            {
                // Cache all trait defs with force sensitivity
                ForceTraitDefs.UnionWith(DefDatabase<TraitDef>.AllDefs.Where(traitDef =>
                    traitDef.GetModExtension<ModExtension_ForceSensitivity>() != null));

                // Cache all hediff defs with force sensitivity
                ForceHediffDefs.UnionWith(DefDatabase<HediffDef>.AllDefs.Where(hediffDef =>
                    hediffDef.GetModExtension<ModExtension_ForceSensitivity>() != null));

                // Cache all gene defs with force sensitivity
                ForceGeneDefs.UnionWith(DefDatabase<GeneDef>.AllDefs.Where(geneDef =>
                    geneDef.GetModExtension<ModExtension_ForceSensitivity>() != null));

                // Cache all pawn kind defs with force user extension
                ForcePawnKindDefs.UnionWith(DefDatabase<PawnKindDef>.AllDefs.Where(kindDef =>
                    kindDef.GetModExtension<ModExtension_ForceUser>() != null));
            }
        }

        public static bool IsValidForceUser(this Pawn pawn)
        {
            if (pawn == null || pawn.story?.traits == null || pawn.TryGetComp<CompClass_ForceUser>() == null)
                return false;

            if (ForceUserCache.Contains(pawn))
                return true;

            if (NonForceUserCache.Contains(pawn))
                return false;

            bool result = CalculateForceUserStatus(pawn);

            if (result)
                ForceUserCache.Add(pawn);
            else
                NonForceUserCache.Add(pawn);

            return result;
        }

        private static bool CalculateForceUserStatus(Pawn pawn)
        {

            if (HasForceSensitiveVeto(pawn))
                return false;

            if (pawn.story.traits.allTraits.Any(trait => ForceTraitDefs.Contains(trait.def)))
                return true;

            if (pawn.kindDef != null && ForcePawnKindDefs.Contains(pawn.kindDef))
                return true;

            if (pawn.health?.hediffSet != null)
            {
                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    if (ForceHediffDefs.Contains(hediff.def))
                        return true;
                }
            }

            if (pawn.genes != null)
            {
                foreach (var geneDef in ForceGeneDefs)
                {
                    if (pawn.genes.HasActiveGene(geneDef))
                        return true;
                }
            }

            return false;
        }

        private static bool HasForceSensitiveVeto(Pawn pawn)
        {
            foreach (var trait in pawn.story.traits.allTraits)
            {
                var ext = trait.def.GetModExtension<ModExtension_ForceSensitivity>();
                if (ext != null && !ext.forceSensitive)
                    return true;
            }

            if (pawn.health?.hediffSet != null)
            {
                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    var ext = hediff.def.GetModExtension<ModExtension_ForceSensitivity>();
                    if (ext != null && !ext.forceSensitive)
                        return true;
                }
            }

            if (pawn.genes != null)
            {
                foreach (var gene in pawn.genes.GenesListForReading)
                {
                    var ext = gene.def.GetModExtension<ModExtension_ForceSensitivity>();
                    if (ext != null && !ext.forceSensitive)
                        return true;
                }
            }

            return false;
        }

        public static void ClearCacheForPawn(Pawn pawn)
        {
            ForceUserCache.Remove(pawn);
            NonForceUserCache.Remove(pawn);
        }

        public static void ClearAllCache()
        {
            ForceUserCache.Clear();
            NonForceUserCache.Clear();
        }

        public static void MarkForRedraw(this CompClass_ForceUser comp)
        {
            if (comp.parent is Pawn pawn)
            {
                PortraitsCache.SetDirty(pawn);
            }
        }

        public static bool IsMaster(Pawn pawn)
        {
            var forceComp = pawn?.GetComp<CompClass_ForceUser>();
            return (bool)(pawn.abilities.abilities?.Any(a => a.def == ForceDefOf.Force_Apprenticeship));
        }

        public static bool IsApprentice(Pawn pawn)
        {
            var forceComp = pawn?.GetComp<CompClass_ForceUser>();
            return forceComp?.Apprenticeship?.master != null;
        }

        public static Pawn GetMaster(Pawn apprentice)
        {
            var forceComp = apprentice?.GetComp<CompClass_ForceUser>();
            return forceComp?.Apprenticeship?.master;
        }

        public static IEnumerable<Pawn> GetApprentices(Pawn master)
        {
            var forceComp = master?.GetComp<CompClass_ForceUser>();
            return forceComp?.Apprenticeship?.apprentices ?? Enumerable.Empty<Pawn>();
        }

        public static int GetApprenticeCount(Pawn master)
        {
            var forceComp = master?.GetComp<CompClass_ForceUser>();
            return forceComp?.Apprenticeship?.apprentices?.Count ?? 0;
        }

        public static int GetGraduatedApprenticeCount(Pawn master)
        {
            var forceComp = master?.GetComp<CompClass_ForceUser>();
            return forceComp?.Apprenticeship?.graduatedApprenticesCount ?? 0;
        }

        public static float GetMaxFP(Pawn pawn)
        {
            if (pawn == null) return 0f;

            var forceComp = pawn.GetComp<CompClass_ForceUser>();
            return forceComp?.MaxFP ?? 0f;
        }

        public static float GetCurrentFP(Pawn pawn)
        {
            if (pawn == null) return 0f;

            var forceComp = pawn.GetComp<CompClass_ForceUser>();
            return forceComp?.currentFP ?? 0f;
        }

        public static int GetForceLevel(Pawn pawn)
        {
            if (pawn == null) return 0;

            var forceComp = pawn.GetComp<CompClass_ForceUser>();
            if (forceComp?.IsValidForceUser == true)
                return forceComp.forceLevel;

            return 0;
        }

        public static bool IsForceGhost(this Pawn pawn)
        {
            if (pawn == null) return false;

            return pawn.health.hediffSet.HasHediff(ForceDefOf.Force_Ghost) ||
                                pawn.health.hediffSet.HasHediff(ForceDefOf.Force_SithGhost);
        }
    }

    [HarmonyPatch(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.RemoveGene))]
    public static class Patch_Pawn_GeneTracker_RemoveGene
    {
        private static void Postfix(Pawn_GeneTracker __instance, Gene gene)
        {
            if (gene?.def != null && ForceSensitivityUtils.ForceGeneDefs.Contains(gene.def))
            {
                Pawn pawn = __instance.pawn;
                if (pawn != null)
                {
                    // Check if the pawn still has any active force-sensitive genes
                    bool stillHasForceGene = false;
                    foreach (var forceGeneDef in ForceSensitivityUtils.ForceGeneDefs)
                    {
                        if (__instance.HasActiveGene(forceGeneDef))
                        {
                            stillHasForceGene = true;
                            break;
                        }
                    }

                    if (!stillHasForceGene)
                    {
                        ForceSensitivityUtils.ClearCacheForPawn(pawn);
                        var comp = pawn.TryGetComp<CompClass_ForceUser>();
                        comp?.MarkForRedraw();
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_GeneTracker))]
    [HarmonyPatch("AddGene")]
    [HarmonyPatch(new Type[] { typeof(GeneDef), typeof(bool) })]
    public static class Patch_Pawn_GeneTracker_AddGene
    {
        private static void Postfix(Pawn_GeneTracker __instance, GeneDef geneDef, bool xenogene, Gene __result)
        {
            if (geneDef != null && ForceSensitivityUtils.ForceGeneDefs.Contains(geneDef))
            {
                Pawn pawn = __instance.pawn;
                if (pawn != null)
                {
                    // Only update if this gene is actually active and makes the pawn a force user
                    if (__instance.HasActiveGene(geneDef))
                    {
                        ForceSensitivityUtils.ClearCacheForPawn(pawn);
                        var comp = pawn.TryGetComp<CompClass_ForceUser>();
                        comp?.MarkForRedraw();
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(TraitSet), "GainTrait")]
    public static class TraitSet_GainTraitForceSensitivity
    {
        public static void Postfix(Pawn ___pawn, Trait trait)
        {
            if (trait?.def != null && ForceSensitivityUtils.ForceTraitDefs.Contains(trait.def))
            {
                ForceSensitivityUtils.ClearCacheForPawn(___pawn);            }
        }
    }

    [HarmonyPatch(typeof(TraitSet), "RemoveTrait")]
    public static class TraitSet_RemoveTraitForceSensitivity
    {
        public static void Postfix(Pawn ___pawn, Trait trait)
        {

            if (trait?.def != null && ForceSensitivityUtils.ForceTraitDefs.Contains(trait.def))
            {
                ForceSensitivityUtils.ClearCacheForPawn(___pawn);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff), new Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) })]
    public static class Pawn_HealthTracker_AddHediffForceSensitivity
    {
        public static void Postfix(Pawn_HealthTracker __instance, Hediff hediff, Pawn ___pawn)
        {
            if (hediff?.def != null && ForceSensitivityUtils.ForceHediffDefs.Contains(hediff.def))
            {
                ForceSensitivityUtils.ClearCacheForPawn(___pawn);
                var comp = ___pawn.TryGetComp<CompClass_ForceUser>();
                comp?.MarkForRedraw();
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.RemoveHediff))]
    public static class Pawn_HealthTracker_RemoveHediffForceSensitivity
    {
        public static void Postfix(Pawn_HealthTracker __instance, Hediff hediff, Pawn ___pawn)
        {
            if (hediff?.def != null && ForceSensitivityUtils.ForceHediffDefs.Contains(hediff.def))
            {
                ForceSensitivityUtils.ClearCacheForPawn(___pawn);
                var comp = ___pawn.TryGetComp<CompClass_ForceUser>();
                comp?.MarkForRedraw();
            }
        }
    }
}
