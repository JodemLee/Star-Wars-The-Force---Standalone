using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheForce_Standalone.Telekinesis;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TheForce_Standalone.HediffComps
{
    [StaticConstructorOnStartup]
    internal class HediffComp_ThingHolder : HediffComp, ISuspendableThingHolder
    {
        protected ThingOwner<Thing> innerContainer;
        public Material thingMaterial;
        protected PawnTextureAtlasFrameSet frameSet;
        private static readonly Material ShadowMaterial = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowCircle", ShaderDatabase.Transparent, new Color(1f, 1f, 1f, 0.5f));
        public HediffCompProperties_ThingHolder Props =>
            (HediffCompProperties_ThingHolder)props;

        public Thing HeldThing => innerContainer?.InnerListForReading.FirstOrDefault();
        public bool CanLaunchProjectile => HeldThing != null &&
          Pawn.Drafted &&
          Pawn.Spawned;
        public bool IsContentsSuspended => true;
        public IThingHolder ParentHolder => parent.pawn;

        private Vector3? customDrawPos = null;
        private Quaternion? customDrawRot = null;

        public virtual Vector3 DrawPosition
        {
            get => customDrawPos ?? GetDefaultDrawPos();
            set => customDrawPos = value;
        }

        public virtual Quaternion DrawRotation
        {
            get => customDrawRot ?? GetDefaultDrawRot();
            set => customDrawRot = value;
        }

        private Vector3 GetDefaultDrawPos()
        {
            Vector3 drawPos = Pawn.Drawer.DrawPos;
            drawPos.z += 1;
            float wave = Mathf.Sin(Time.realtimeSinceStartup * 1.5f) * 0.1f;
            drawPos.z += wave;
            drawPos.x += 1;
            return drawPos;
        }

        private Quaternion GetDefaultDrawRot()
        {
            return parent.pawn.Rotation.AsQuat;
        }

        public float baseDamage = 1f;

        public HediffComp_ThingHolder()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        public override void CompPostMake()
        {
            base.CompPostMake();
            if (innerContainer == null)
            {
                innerContainer = new ThingOwner<Thing>(this);
            }
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        public bool TryStoreThing(Thing thing)
        {
            if (thing == null || thing.Destroyed)
            {
                return false;
            }

            if (innerContainer == null)
            {
                innerContainer = new ThingOwner<Thing>(this);
            }

            if (HeldThing != null)
            {
                return false;
            }

            if (innerContainer.TryAdd(thing))
            {
                return true;
            }
            return false;
        }

        public virtual void DrawWornExtras()
        {
            if (HeldThing != null)
            {
                try
                {
                    if (this.parent.pawn.Map == null) return;
                    if (HeldThing == null) return;

                    Thing thrownThing = HeldThing;
                    if (thrownThing == null || thrownThing.Destroyed) return;

                    Vector3 drawPos = DrawPosition;
                    Quaternion drawRot = DrawRotation;
                    Vector3 groundPos = Pawn.Drawer.DrawPos;

                    DrawShadow(groundPos, 1.5f);

                    if (thrownThing is Corpse corpse)
                    {
                        drawPos.x += 1;
                        if (corpse.InnerPawn == null) return;
                        corpse.InnerPawn.Drawer?.renderer?.RenderPawnAt(drawPos, parent.pawn.Rotation, true);
                    }
                    else if (thrownThing is Pawn pawn)
                    {
                        drawPos.x += 1;
                        if (pawn.Drawer?.renderer == null) return;
                        pawn.Drawer.renderer.RenderPawnAt(drawPos, parent.pawn.Rotation, false);
                    }
                    else if (thrownThing?.Graphic is { } graphic)
                    {
                        drawPos.x += HeldThing.def.Size.x;

                        UnityEngine.Graphics.DrawMesh(
                            MeshPool.GridPlane(graphic.drawSize),
                            drawPos,
                            drawRot,
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
        }

        public void DrawShadow(Vector3 drawLoc, float height)
        {
            Material shadowMaterial = ShadowMaterial;
            if (!(shadowMaterial == null))
            {
                float num = Mathf.Lerp(1f, 0.6f, height);
                Vector3 s = new Vector3(num, 1f, num);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawLoc, Quaternion.identity, s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, shadowMaterial, 0);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (!Pawn.Drafted) yield break;

            yield return new Command_Action
            {
                defaultLabel = Props.labelKey.Translate(HeldThing?.LabelShort ?? "thing"),
                defaultDesc = Props.descriptionKey.Translate(HeldThing?.LabelShort ?? "thing"),
                icon = ContentFinder<Texture2D>.Get(Props.iconPath),
                action = () => Find.Targeter.BeginTargeting(
                    new TargetingParameters
                    {
                        canTargetLocations = true,
                        canTargetPawns = true,
                        canTargetBuildings = true,
                        mapObjectTargetsMustBeAutoAttackable = true,
                        validator = (target) => target.Cell.DistanceTo(Pawn.Position) <= Props.range
                    },
                    action: (LocalTargetInfo target) =>
                    {
                        LaunchProjectileAt(target.Cell, target);
                    },
                    highlightAction: (LocalTargetInfo target) =>
                    {
                        GenDraw.DrawRadiusRing(Pawn.Position, Props.range);
                    },
                    targetValidator: (LocalTargetInfo target) =>
                    {
                        GenDraw.DrawTargetHighlight(target);
                        return target.Cell.DistanceTo(Pawn.Position) <= Props.range;
                    },
                    caster: parent.pawn
                ),
                Disabled = !CanLaunchProjectile,
                disabledReason = ""
            };

            if (HeldThing != null && HeldThing is Pawn && ModsConfig.AnomalyActive)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Force.ExplodePawn".Translate(HeldThing.LabelShort),
                    defaultDesc = "Force.ExplodePawnDesc".Translate(HeldThing.LabelShort),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Detonate"),
                    action = () =>
                    {
                        ExplodeHeldPawn();
                    }
                };
            }

            if (HeldThing != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Force.ReleaseThing".Translate(HeldThing.LabelShort),
                    defaultDesc = "Force.ReleaseThingDesc".Translate(HeldThing.LabelShort),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/DropCarriedPawn"),
                    action = () =>
                    {
                        if (TryReleaseThing(out Thing releasedThing))
                        {
                            Pawn.health.RemoveHediff(this.parent);
                        }
                    }
                };
            }
        }

        public void ExplodeHeldPawn()
        {
            try
            {
                if (HeldThing == null || HeldThing.Destroyed || HeldThing is not Pawn heldPawn)
                {
                    Log.Warning("[ForceThrow] Attempted to explode null or non-pawn held thing");
                    return;
                }

                Map map = heldPawn.MapHeld;
                if (map == null)
                {
                    Log.Warning("[ForceThrow] Cannot explode pawn - no map reference");
                    return;
                }

                IntVec3 positionHeld = heldPawn.PositionHeld;
                if (!positionHeld.IsValid)
                {
                    positionHeld = parent.pawn.Position;
                }

                positionHeld.x += 1;
                positionHeld.z += 1;

                if (!positionHeld.InBounds(map))
                {
                    positionHeld = parent.pawn.Position;
                }

                if (heldPawn.SpawnedOrAnyParentSpawned)
                {
                    heldPawn.Kill(new DamageInfo(DamageDefOf.Bomb, 99999f, 0f, -1f, parent.pawn));
                }

                if (heldPawn.Corpse != null && !heldPawn.Corpse.Destroyed)
                {
                    heldPawn.Corpse.Destroy();
                }

                EffecterDefOf.MeatExplosion?.Spawn(positionHeld, map)?.Cleanup();

                FilthMaker.TryMakeFilth(positionHeld, map, ThingDefOf.Filth_Blood, Rand.Range(5, 10));

                float meatStatValue = heldPawn.GetStatValue(StatDefOf.MeatAmount);
                int meatAmount = Mathf.Max(GenMath.RoundRandom(meatStatValue), 3);

                ThingDef meatDef = heldPawn.def?.race?.meatDef ?? ThingDefOf.Meat_Human;
                if (meatDef == null)
                {
                    meatDef = ThingDefOf.Meat_Human;
                }

                int stackLimit = meatDef.stackLimit;
                int meatPieces = Mathf.Max(3, Mathf.CeilToInt((float)meatAmount / Mathf.Max(1, stackLimit)));

                for (int i = 0; i < meatPieces; i++)
                {
                    if (RCellFinder.TryFindRandomCellNearWith(positionHeld,
                        (IntVec3 x) => x.Walkable(map) && x.InBounds(map),
                        map, out var result, 1, 4))
                    {
                        Thing meat = ThingMaker.MakeThing(meatDef);
                        meat.stackCount = Mathf.Max(1, meatAmount / meatPieces);
                        GenSpawn.Spawn(meat, result, map);
                    }
                }

                if (Pawn.health.hediffSet.hediffs.Contains(this.parent))
                {
                    var forceComp = Pawn.TryGetComp<CompClass_ForceUser>();
                    if (forceComp != null)
                    {
                        forceComp.Alignment.AddDarkSideAttunement(25);
                    }
                    Pawn.health.RemoveHediff(this.parent);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[ForceThrow] Error in ExplodeHeldPawn: {ex}");
                TryReleaseThing(out _);
                if (Pawn.health.hediffSet.hediffs.Contains(this.parent))
                {
                    Pawn.health.RemoveHediff(this.parent);
                }
            }
        }

        public virtual void LaunchProjectileAt(IntVec3 targetCell, LocalTargetInfo targetInfo)
        {
            if (!CanLaunchProjectile || !targetCell.IsValid)
            {
                Log.Warning("[ForceThrow] Cannot launch - invalid conditions");
                return;
            }

            if (parent.pawn == null || parent.pawn.Map == null)
            {
                Log.Warning("[ForceThrow] Cannot launch - null pawn or map");
                return;
            }

            Thing heldThing = HeldThing;
            if (heldThing == null) return;

            ThingDef projectileDef = DefDatabase<ThingDef>.GetNamed("Force_ThrowItem");
            if (projectileDef == null)
            {
                Log.Error("[ForceThrow] Missing projectile def 'Force_ThrowItem'");
                return;
            }

            float size = Mathf.Lerp(1, 1, 1);
            Vector3 drawPos = Pawn.Drawer.DrawPos;
            drawPos.z += 1;
            drawPos.x += 1;
            float wave = Mathf.Sin(Time.realtimeSinceStartup * 1.5f) * 0.1f;
            drawPos.z += wave;

            TelekinesisUtility.LaunchThingNoDespawn(
                thing: heldThing,
                usedThing: targetInfo,
                destination: targetCell,
                caster: parent.pawn,
                projectileDef: projectileDef,
                baseSpeed: baseDamage,
                drawPos.ToIntVec3()
            );

            innerContainer.Remove(heldThing);

            if (Props.soundCast != null)
            {
                Props.soundCast.PlayOneShot(new TargetInfo(parent.pawn.Position, parent.pawn.Map));
            }

            Pawn.health.RemoveHediff(this.parent);
        }

        public virtual bool TryReleaseThing(out Thing releasedThing)
        {
            releasedThing = HeldThing;
            if (releasedThing == null)
            {
                return false;
            }

            if (parent.pawn.Map == null)
            {
                innerContainer.Remove(releasedThing);
                return true;
            }

            return innerContainer.TryDrop(releasedThing, parent.pawn.Position, parent.pawn.Map, ThingPlaceMode.Near, 1, out _);
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            TryReleaseThing(out _);
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            TryReleaseThing(out _);
            base.Notify_PawnDied(dinfo, culprit);
        }

        public override string CompTipStringExtra
        {
            get
            {
                string tip = "";

                var heldThing = HeldThing;
                if (heldThing != null)
                {
                    return $"Currently holding: {heldThing.LabelShortCap}";
                }

                return tip;
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && innerContainer == null)
            {
                innerContainer = new ThingOwner<Thing>(this);
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

        }

        public virtual bool CanCatchProjectile(Projectile projectile)
        {
            return false;
        }

        public virtual bool TryCatchProjectile(Projectile projectile)
        {
            return false;
        }
    }


    internal class HediffCompProperties_ThingHolder : HediffCompProperties
    {
        public float range = 30f;
        public SoundDef soundCast;
        public string labelKey = "Force.LaunchProjectile";
        public string descriptionKey = "Force.LaunchProjectileDesc";
        public string iconPath;
        public bool catchProjectile = false;

        public HediffCompProperties_ThingHolder()
        {
            compClass = typeof(HediffComp_ThingHolder);
        }
    }


    [StaticConstructorOnStartup]
    public static class NonPublicFields
    {
        public static FieldInfo Projectile_ticksToImpact = AccessTools.Field(typeof(Projectile), "ticksToImpact");
        public static FieldInfo Projectile_origin = AccessTools.Field(typeof(Projectile), "origin");
        public static FieldInfo Projectile_destination = AccessTools.Field(typeof(Projectile), "destination");
        public static FieldInfo Projectile_usedTarget = AccessTools.Field(typeof(Projectile), "usedTarget");
    }
}