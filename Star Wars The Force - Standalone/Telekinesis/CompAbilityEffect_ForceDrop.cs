using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Telekinesis
{
    internal class CompAbilityEffect_ForceDrop : CompAbilityEffect
    {
        public new CompProperties_ForceDrop Props => (CompProperties_ForceDrop)props;

        private List<DropConfig> EffectiveDroppableItems
        {
            get
            {
                // If droppableItems is null or empty, use defaults
                if (Props.droppableItems == null || Props.droppableItems.Count == 0)
                {
                    return new List<DropConfig>
                    {
                        new DropConfig(ThingDefOf.ShipChunkIncoming, 1f),
                        new DropConfig(ThingDefOf.ShuttleCrashing, 3f),
                        new DropConfig(ThingDefOf.CrashedShipPartIncoming, 1f)
                    };
                }
                return Props.droppableItems;
            }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (!target.IsValid || target.Thing.Map == null)
            {
                Log.Error("ForceDrop: Invalid target or null map");
                return;
            }

            if (!target.Cell.InBounds(target.Thing.Map))
            {
                Log.Error($"ForceDrop: Target cell {target.Cell} out of bounds");
                return;
            }

            SpawnRandomChunk(target.Cell, target.Thing.Map);
        }

        private void SpawnRandomChunk(IntVec3 pos, Map map)
        {
            try
            {
                var droppableItems = EffectiveDroppableItems;

                // Filter out any null items
                var validDrops = droppableItems
                    .Where(x => x?.item != null)
                    .ToList();

                if (validDrops.Count == 0)
                {
                    Log.Error("ForceDrop: No valid items in droppableItems list");
                    return;
                }

                // Calculate total weight
                float totalWeight = validDrops.Sum(x => x.itemWeight);
                if (totalWeight <= 0f)
                {
                    Log.Error("ForceDrop: Total item weight must be positive");
                    return;
                }

                // Select random item
                float randomValue = Rand.Range(0f, totalWeight);
                float cumulativeWeight = 0f;

                foreach (var dropConfig in validDrops)
                {
                    cumulativeWeight += dropConfig.itemWeight;
                    if (randomValue <= cumulativeWeight && dropConfig.item != null)
                    {
                        SkyfallerMaker.SpawnSkyfaller(dropConfig.item, ThingDefOf.ShipChunk, pos, map);
                        return;
                    }
                }

                Log.Error("ForceDrop: Failed to select item for drop");
            }
            catch (Exception ex)
            {
                Log.Error($"ForceDrop error in SpawnRandomChunk: {ex}");
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return base.CanApplyOn(target, dest) && Valid(target);
        }

        public override bool Valid(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.Valid(target, showMessages))
                return false;

            if (target.Thing.Map == null || !target.Cell.InBounds(target.Thing.Map))
            {
                return false;
            }

            return true;
        }
    }

    public class CompProperties_ForceDrop : CompProperties_AbilityEffect
    {
        public List<DropConfig> droppableItems;

        public CompProperties_ForceDrop()
        {
            compClass = typeof(CompAbilityEffect_ForceDrop);
        }
    }

    public class DropConfig
    {
        public ThingDef item;
        public float itemWeight;

        public DropConfig() { }

        public DropConfig(ThingDef item, float weight)
        {
            this.item = item;
            this.itemWeight = weight;
        }
    }
}