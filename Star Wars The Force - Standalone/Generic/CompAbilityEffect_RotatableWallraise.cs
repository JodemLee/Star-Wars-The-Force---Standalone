using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Generic
{
    internal class CompAbilityEffect_RotatableWallraise : CompAbilityEffect
    {
        private int rotationIndex = 0;
        public static Color DustColor = new Color(0.55f, 0.55f, 0.55f, 4f);

        public new CompProperties_AbilityRotatableWallraise Props => (CompProperties_AbilityRotatableWallraise)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = parent.pawn.Map;

            List<Thing> items = AffectedCells(target, map)
                .SelectMany(c => c.GetThingList(map).Where(t => t.def.category == ThingCategory.Item))
                .ToList();

            foreach (Thing item in items)
            {
                item.DeSpawn();
            }

            ThingDef wallDef = Props.wallDef ?? ThingDefOf.RaisedRocks;
            foreach (IntVec3 cell in AffectedCells(target, map))
            {
                GenSpawn.Spawn(wallDef, cell, map);
                FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), map, Rand.Range(1.5f, 3f), DustColor);
            }

            foreach (Thing item in items)
            {
                IntVec3 spawnPos = IntVec3.Invalid;
                foreach (IntVec3 radialPos in GenRadial.RadialPattern.Take(9))
                {
                    IntVec3 testPos = item.Position + radialPos;
                    if (testPos.InBounds(map) && testPos.Walkable(map) &&
                        !map.thingGrid.ThingsListAtFast(testPos).Any())
                    {
                        spawnPos = testPos;
                        break;
                    }
                }
                GenPlace.TryPlaceThing(item, spawnPos.IsValid ? spawnPos : item.Position, map, ThingPlaceMode.Near);
            }
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            if (KeyBindingDefOf.Designator_RotateRight.JustPressed)
            {
                rotationIndex = (rotationIndex + 1) % 4;
            }
            if (KeyBindingDefOf.Designator_RotateLeft.JustPressed)
            {
                rotationIndex = (rotationIndex - 1 + 4) % 4;
            }

            GenDraw.DrawFieldEdges(AffectedCells(target, parent.pawn.Map).ToList(), Valid(target) ? Color.white : Color.red);
        }

        private IEnumerable<IntVec3> AffectedCells(LocalTargetInfo target, Map map)
        {
            foreach (IntVec2 patternCell in Props.pattern)
            {
                IntVec3 rotatedCell = RotatePatternCell(patternCell, rotationIndex);
                IntVec3 worldCell = target.Cell + rotatedCell;

                if (worldCell.InBounds(map))
                {
                    yield return worldCell;
                }
            }
        }

        private IntVec3 RotatePatternCell(IntVec2 cell, int rotation)
        {
            switch (rotation)
            {
                case 1: return new IntVec3(-cell.z, 0, cell.x);  // 90°
                case 2: return new IntVec3(-cell.x, 0, -cell.z); // 180°
                case 3: return new IntVec3(cell.z, 0, -cell.x);   // 270°
                default: return new IntVec3(cell.x, 0, cell.z);   // 0°
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            foreach (IntVec3 cell in AffectedCells(target, parent.pawn.Map))
            {
                if (cell.Filled(parent.pawn.Map))
                {
                    if (throwMessages)
                    {
                        Messages.Message("CannotUseAbility".Translate(parent.def.label) + ": " + "AbilityOccupiedCells".Translate(),
                            target.ToTargetInfo(parent.pawn.Map), MessageTypeDefOf.RejectInput, historical: false);
                    }
                    return false;
                }

                if (!cell.Standable(parent.pawn.Map))
                {
                    if (throwMessages)
                    {
                        Messages.Message("CannotUseAbility".Translate(parent.def.label) + ": " + "AbilityUnwalkable".Translate(),
                            target.ToTargetInfo(parent.pawn.Map), MessageTypeDefOf.RejectInput, historical: false);
                    }
                    return false;
                }

                if (cell.GetThingList(parent.pawn.Map).Any(t => !t.def.destroyable))
                {
                    if (throwMessages)
                    {
                        Messages.Message("CannotUseAbility".Translate(parent.def.label) + ": " + "AbilityNotEnoughFreeSpace".Translate(),
                            target.ToTargetInfo(parent.pawn.Map), MessageTypeDefOf.RejectInput, historical: false);
                    }
                    return false;
                }
            }
            return true;
        }
    }

    public class CompProperties_AbilityRotatableWallraise : CompProperties_AbilityEffect
    {
        public ThingDef wallDef;
        public List<IntVec2> pattern;

        public CompProperties_AbilityRotatableWallraise()
        {
            compClass = typeof(CompAbilityEffect_RotatableWallraise);
        }
    }
}