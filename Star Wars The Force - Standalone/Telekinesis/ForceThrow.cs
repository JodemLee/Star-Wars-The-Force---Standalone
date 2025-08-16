using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{

    [StaticConstructorOnStartup]
    public class Projectile_ForceThrow : Projectile, IThingHolder
    {
        public Thing targetThing;
        public new int DamageAmount;
        public IntVec3 destCell;
        protected ThingOwner<Thing> carriedThing;
        private static readonly Material ShadowMaterial = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowCircle", ShaderDatabase.Transparent);

        public Projectile_ForceThrow()
        {
            carriedThing = new ThingOwner<Thing>(this);
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return carriedThing;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad && carriedThing.Count == 0)
            {
                if (targetThing != null && targetThing.Spawned)
                {
                    carriedThing.TryAdd(targetThing);
                    targetThing.DeSpawn(DestroyMode.Vanish);
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            try
            {
                if (this.Map == null || !this.Spawned) return;

                var easedPosition = EasingFunctions.EaseInOutQuint(DistanceCoveredFraction);
                var position = Vector3.Lerp(origin, destCell.ToVector3(), easedPosition);
                drawLoc = position;

                if (carriedThing == null || carriedThing.Count == 0) return;

                Thing thrownThing = carriedThing[0];
                if (thrownThing == null || thrownThing.Destroyed) return;

                if (thrownThing is Corpse corpse)
                {
                    if (corpse.InnerPawn == null) return;
                    corpse.InnerPawn.Drawer?.renderer?.RenderPawnAt(drawLoc, Rot4.South, true);
                }
                else if (thrownThing is Pawn pawn)
                {
                    if (pawn.Drawer?.renderer == null) return;
                    Rot4 rotation = pawn.Rotation;
                    var parms = new PawnDrawParms
                    {
                        pawn = pawn,
                        facing = rotation,
                        flags = PawnRenderFlags.NeverAimWeapon,
                        dead = pawn.Dead,
                        crawling = pawn.Crawling,
                        swimming = pawn.Swimming
                    };


                    if (rotation == Rot4.East || rotation == Rot4.West)
                    {
                        drawLoc.z += 0.2f;
                    }

                    // Render with forced south rotation to prevent equipment issues
                    pawn.Drawer.renderer.RenderPawnAt(drawLoc, Rot4.South, true);
                }
                else if (thrownThing?.Graphic is { } graphic)
                {
                    UnityEngine.Graphics.DrawMesh(
                        MeshPool.GridPlane(graphic.drawSize),
                        drawLoc,
                        Quaternion.identity,
                        graphic.MatSingle,
                        0
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ForceThrowProjectile.DrawAt: {ex}");
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield)
        {
            if (hitThing != null)
            {
                hitThing.TakeDamage(new DamageInfo(DamageDefOf.Blunt, DamageAmount, 0f, -1f, this.launcher, null, null, DamageInfo.SourceCategory.ThingOrUnknown));
            }

            if (carriedThing.Count > 0 && Map != null)
            {
                IntVec3 finalCell = GetFinalLandingCell();
                TryPlaceThingAt(finalCell);
            }

            base.Impact(hitThing, blockedByShield);
        }
        private IntVec3 GetFinalLandingCell()
        {
            if (carriedThing.Count == 0) return Position;

            Thing thrownThing = carriedThing[0];
            if (destCell.IsValid && destCell.Standable(Map) && destCell.GetFirstThing(Map, thrownThing.def) == null)
                return destCell;

            foreach (var cell in GenAdj.CellsAdjacent8Way(new TargetInfo(Position, Map)))
                if (cell.InBounds(Map) && cell.Standable(Map) && cell.GetFirstThing(Map, thrownThing.def) == null)
                    return cell;

            return Position;
        }

        private void TryPlaceThingAt(IntVec3 cell)
        {
            if (carriedThing.Count == 0) return;

            Thing thrownThing = carriedThing[0];
            if (GenPlace.TryPlaceThing(thrownThing, cell, Map, ThingPlaceMode.Near) &&
                cell.GetFirstThing(Map, thrownThing.def) is Thing existing &&
                existing != thrownThing &&
                existing.def == thrownThing.def)
            {
                existing.TryAbsorbStack(thrownThing, true);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref carriedThing, "carriedThing", this);
            Scribe_Values.Look(ref DamageAmount, "DamageAmount");
            Scribe_Values.Look(ref destCell, "destCell");
        }
    }
}