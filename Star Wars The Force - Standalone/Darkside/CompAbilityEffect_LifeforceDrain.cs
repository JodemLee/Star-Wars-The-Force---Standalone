using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static UnityEngine.GraphicsBuffer;

namespace TheForce_Standalone.Darkside
{
    internal class CompAbilityEffect_LifeforceDrain : CompAbilityEffect
    {
        public new CompProperties_AbilityLifeforceDrain Props => (CompProperties_AbilityLifeforceDrain)props;

        private float Radius => 3f; // Fixed radius of 3 cells

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Need_Lifeforce lifeforceNeed = parent.pawn.needs.TryGetNeed<Need_Lifeforce>();
            if (lifeforceNeed == null) return;

            Map map = parent.pawn.Map;
            bool anythingDrained = false;
            float totalLifeforceGained = 0f;
            int pawnsDrained = 0;
            int plantsDrained = 0;
            int terrainDrained = 0;

            // Apply drain to all targets in radius
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Cell, Radius, true))
            {
                if (!cell.InBounds(map)) continue;

                float lifeforceFromCell = 0f;

                // Drain from pawns (if any)
                Pawn pawn = cell.GetFirstPawn(map);
                if (pawn != null && pawn != parent.pawn)
                {
                    DrainFromPawn(pawn, lifeforceNeed);
                }

                // Drain from plants (if any)
                Plant plant = cell.GetPlant(map);
                if (plant != null && !plant.Destroyed)
                {
                    lifeforceFromCell += DrainFromPlant(plant, lifeforceNeed);
                    if (lifeforceFromCell > 0f) plantsDrained++;
                }

                // Always drain from terrain (regardless of pawns/plants)
                float terrainLifeforce = DrainFromTerrain(cell, map, lifeforceNeed);
                lifeforceFromCell += terrainLifeforce;
                if (terrainLifeforce > 0f) terrainDrained++;

                totalLifeforceGained += lifeforceFromCell;

                if (lifeforceFromCell > 0f)
                    anythingDrained = true;
            }

            // Show summary message
            if (anythingDrained)
            {
                string message = $"{parent.pawn.LabelShort} drains lifeforce from the area (+{totalLifeforceGained.ToStringPercent()})";

                List<string> sources = new List<string>();
                if (pawnsDrained > 0) sources.Add($"{pawnsDrained} pawn(s)");
                if (plantsDrained > 0) sources.Add($"{plantsDrained} plant(s)");
                if (terrainDrained > 0) sources.Add($"{terrainDrained} terrain tile(s)");

                if (sources.Count > 0)
                {
                    message += $" [from {string.Join(", ", sources)}]";
                }

                Messages.Message(message, parent.pawn, MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                Messages.Message(
                    "No lifeforce could be drained from the area.",
                    parent.pawn,
                    MessageTypeDefOf.NeutralEvent
                );
            }
        }

        private void DrainFromPawn(Pawn pawn, Need_Lifeforce lifeforceNeed)
        {
            // Assuming FeedOnPawn returns the amount of lifeforce gained
            lifeforceNeed.FeedOnPawn(pawn);
        }


        private float DrainFromPlant(Plant plant, Need_Lifeforce lifeforceNeed)
        {
            if (plant.Destroyed) return 0f;

            float lifeforceGained = plant.HitPoints * 0.001f;
            lifeforceGained *= plant.Growth;
            lifeforceNeed.CurLevel += lifeforceGained;

            int damage = Mathf.RoundToInt(lifeforceGained * 200f);
            plant.TakeDamage(new DamageInfo(DamageDefOf.Psychic, damage, 1f, -1f, parent.pawn));

            if (plant.HitPoints <= 0)
            {
                plant.Destroy();
            }

            return lifeforceGained;
        }

        private float DrainFromTerrain(IntVec3 cell, Map map, Need_Lifeforce lifeforceNeed)
        {
            TerrainDef terrain = map.terrainGrid.TerrainAt(cell);
            float lifeforceGained = GetLifeforceFromTerrain(terrain);

            if (lifeforceGained > 0f)
            {
                lifeforceNeed.CurLevel += lifeforceGained;

                if (ShouldConvertTerrain(terrain))
                {
                    map.terrainGrid.SetTerrain(cell, TerrainDefOf.Sandstone_Smooth);
                }
            }

            return lifeforceGained;
        }

        private float GetLifeforceFromTerrain(TerrainDef terrain)
        {
            if (terrain == null) return 0f;
            float baseGain = terrain.fertility * 0.1f;
            if (terrain.fertility >= 0.7f)
                return baseGain * Props.fertileTerrainMultiplier;
            else if (terrain.fertility >= 0.3f)
                return baseGain * Props.normalTerrainMultiplier;
            else
                return baseGain * Props.barrenTerrainMultiplier;
        }

        private bool ShouldConvertTerrain(TerrainDef terrain)
        {
            if (terrain == null) return false;
            if (terrain.IsWater) return false;
            if (terrain.passability == Traversability.Impassable) return false;
            if (terrain == TerrainDefOf.Sandstone_Smooth) return false;

            return true;
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            if (target.IsValid && target.Cell.InBounds(parent.pawn.Map))
            {
                GenDraw.DrawRadiusRing(target.Cell, Radius, Color.red);
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!base.CanApplyOn(target, dest))
                return false;

            return target.Cell.IsValid;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            if (!target.IsValid || !target.Cell.InBounds(parent.pawn.Map))
                return null;

            Map map = parent.pawn.Map;
            float potentialLifeforce = 0f;
            int pawnsInRange = 0;
            int plantsInRange = 0;
            int fertileTiles = 0;

            // Calculate potential lifeforce from all targets in radius
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Cell, Radius, true))
            {
                if (!cell.InBounds(map)) continue;

                // Check for pawns
                Pawn pawn = cell.GetFirstPawn(map);
                if (pawn != null && pawn != parent.pawn)
                {
                    pawnsInRange++;
                    Need_Lifeforce lifeforceNeed = parent.pawn.needs.TryGetNeed<Need_Lifeforce>();
                    if (lifeforceNeed != null)
                    {
                        potentialLifeforce += lifeforceNeed.CalculateLifeforceFromPawn(pawn);
                    }
                }

                // Check for plants
                Plant plant = cell.GetPlant(map);
                if (plant != null && !plant.Destroyed)
                {
                    plantsInRange++;
                    potentialLifeforce += plant.HitPoints * 0.001f * plant.Growth;
                }

                // Check terrain
                TerrainDef terrain = map.terrainGrid.TerrainAt(cell);
                float terrainLifeforce = GetLifeforceFromTerrain(terrain);
                if (terrainLifeforce > 0f)
                {
                    fertileTiles++;
                    potentialLifeforce += terrainLifeforce;
                }
            }

            if (potentialLifeforce > 0f)
            {
                List<string> sources = new List<string>();
                if (pawnsInRange > 0) sources.Add($"{pawnsInRange} pawn(s)");
                if (plantsInRange > 0) sources.Add($"{plantsInRange} plant(s)");
                if (fertileTiles > 0) sources.Add($"{fertileTiles} tile(s)");

                string sourceText = sources.Count > 0 ? $" from {string.Join(", ", sources)}" : "";

                return $"Potential lifeforce: +{potentialLifeforce.ToStringPercent()}{sourceText}";
            }

            return "No lifeforce sources in area";
        }
    }

    public class CompProperties_AbilityLifeforceDrain : CompProperties_AbilityEffect
    {
        public float fertileTerrainMultiplier = 0.05f;
        public float normalTerrainMultiplier = 0.02f;
        public float barrenTerrainMultiplier = 0.005f;

        public CompProperties_AbilityLifeforceDrain()
        {
            compClass = typeof(CompAbilityEffect_LifeforceDrain);
        }
    }
}