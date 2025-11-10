using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Generic
{
    public class CompAuraEffect : ThingComp
    {
        private CompProperties_AuraEffect Props => (CompProperties_AuraEffect)props;
        private int nextCheckTick = 0;
        private HashSet<Pawn> affectedPawns = new HashSet<Pawn>();
        private List<Pawn> cleanupList = new List<Pawn>();

        public override void CompTick()
        {
            base.CompTick();

            if (Find.TickManager.TicksGame >= nextCheckTick && parent.Spawned)
            {
                try
                {
                    CheckPawnsInRadius();
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in CompAuraEffect tick for {parent}: {ex}");
                }
                nextCheckTick = Find.TickManager.TicksGame + Props.checkIntervalTicks;
            }
        }

        private void CheckPawnsInRadius()
        {
            if (parent.Map == null || parent.Destroyed)
                return;

            // Phase 1: Check existing pawns
            cleanupList.Clear();
            foreach (Pawn pawn in affectedPawns)
            {
                if (!ValidatePawn(pawn))
                {
                    cleanupList.Add(pawn);
                    continue;
                }

                if (OutOfRange(pawn))
                {
                    if (Props.removeWhenLeavingRadius && !ShouldPreserveHediff(pawn))
                    {
                        RemoveHediffs(pawn);
                    }
                    cleanupList.Add(pawn);
                }
            }

            // Clean up departed pawns
            foreach (Pawn pawn in cleanupList)
            {
                affectedPawns.Remove(pawn);
            }

            // Phase 2: Check new pawns in radius
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(parent.Position, parent.Map, Props.radius, true))
            {
                if (thing is Pawn pawn && ValidatePawn(pawn))
                {
                    bool isNew = !affectedPawns.Contains(pawn);
                    if (isNew)
                    {
                        affectedPawns.Add(pawn);
                    }
                    ApplyHediffs(pawn, isNew);
                }
            }
        }

        private bool ShouldPreserveHediff(Pawn pawn)
        {
            bool CheckHediff(HediffDef def)
            {
                if (def == null) return false;
                var hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(def);
                return hediff != null && HasPersistentComp(hediff);
            }

            return CheckHediff(Props.allyHediff) || CheckHediff(Props.enemyHediff);
        }

        private bool HasPersistentComp(Hediff hediff)
        {
            return hediff.def?.comps?.Any(c =>
                c.compClass == typeof(HediffComp_SeverityPerDay) ||
                c.compClass == typeof(HediffComp_SeverityPerSecond) ||
                c.compClass == typeof(HediffComp_Disappears)
            ) ?? false;
        }

        private bool ValidatePawn(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && !pawn.Dead && pawn.health != null;
        }

        private bool OutOfRange(Pawn pawn)
        {
            return pawn.Position.DistanceTo(parent.Position) > Props.radius;
        }

        private void ApplyHediffs(Pawn pawn, bool isNew)
        {
            if (pawn.Faction == Faction.OfPlayer && Props.allyHediff != null)
            {
                AdjustHediff(pawn, Props.allyHediff, Props.allySeverityChange, isNew);
            }
            else if (pawn.HostileTo(Faction.OfPlayer) && Props.enemyHediff != null)
            {
                AdjustHediff(pawn, Props.enemyHediff, Props.enemySeverityChange, isNew);
            }
        }

        private void AdjustHediff(Pawn pawn, HediffDef hediffDef, float severityChange, bool isNew)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                pawn.health.AddHediff(hediff);
                hediff.Severity = hediff.def.initialSeverity;
            }
            else
            {
                hediff.Severity += severityChange;
            }
        }

        private void RemoveHediffs(Pawn pawn)
        {
            void RemoveIfExists(HediffDef def)
            {
                if (def == null) return;
                var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
                if (hediff != null && !HasPersistentComp(hediff))
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }

            RemoveIfExists(Props.allyHediff);
            RemoveIfExists(Props.enemyHediff);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map);
            CleanAllHediffs();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            CleanAllHediffs();
        }

        private void CleanAllHediffs()
        {
            foreach (Pawn pawn in affectedPawns)
            {
                if (ValidatePawn(pawn))
                {
                    RemoveHediffs(pawn);
                }
            }
            affectedPawns.Clear();
        }

        public override string CompInspectStringExtra()
        {
            return parent.Spawned
                ? $"Active Aura (Radius: {Props.radius.ToString("F1")}m)\nAffected: {affectedPawns.Count}"
                : string.Empty;
        }
    }

    public class CompProperties_AuraEffect : CompProperties
    {
        public HediffDef allyHediff;
        public HediffDef enemyHediff;
        public float radius = 5f;
        public int checkIntervalTicks = 60;
        public float allySeverityChange = 0.1f;
        public float enemySeverityChange = 0.1f;
        public bool removeWhenLeavingRadius = true;

        public CompProperties_AuraEffect()
        {
            compClass = typeof(CompAuraEffect);
        }
    }
}
