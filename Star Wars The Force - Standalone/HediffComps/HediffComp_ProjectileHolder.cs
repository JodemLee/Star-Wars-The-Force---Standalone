using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TheForce_Standalone.HediffComps
{
    internal class HediffComp_ProjectileHolder : HediffComp_ThingHolder
    {
        private List<Vector3> worldPositions = new List<Vector3>();
        private List<Quaternion> worldRotations = new List<Quaternion>();
        private List<bool> isCaughtProjectiles = new List<bool>();
        private int currentLaunchIndex = 0;

        public override Vector3 DrawPosition
        {
            get
            {
                if (isCaughtProjectiles.Count > 0 && isCaughtProjectiles[0])
                {
                    return worldPositions[0];
                }
                return base.DrawPosition;
            }
            set
            {
                if (isCaughtProjectiles.Count > 0 && isCaughtProjectiles[0])
                {
                    worldPositions[0] = value;
                }
                else
                {
                    base.DrawPosition = value;
                }
            }
        }

        public override Quaternion DrawRotation
        {
            get
            {
                if (isCaughtProjectiles.Count > 0 && isCaughtProjectiles[0])
                {
                    return worldRotations[0];
                }
                return base.DrawRotation;
            }
            set
            {
                if (isCaughtProjectiles.Count > 0 && isCaughtProjectiles[0])
                {
                    worldRotations[0] = value;
                }
                else
                {
                    base.DrawRotation = value;
                }
            }
        }

        public override void DrawWornExtras()
        {
            if (innerContainer.Count == 0) return;

            try
            {
                if (this.parent.pawn.Map == null) return;

                for (int i = 0; i < innerContainer.Count; i++)
                {
                    Thing thrownThing = innerContainer[i];
                    if (thrownThing == null || thrownThing.Destroyed) continue;

                    Vector3 drawPos;
                    Quaternion drawRot;
                    Vector3 groundPos;

                    bool isCaught = i < isCaughtProjectiles.Count && isCaughtProjectiles[i];

                    if (isCaught && i < worldPositions.Count && i < worldRotations.Count)
                    {
                        drawPos = worldPositions[i];
                        drawRot = worldRotations[i];
                        groundPos = worldPositions[i];
                    }
                    else
                    {
                        drawPos = CalculateOrbitingPosition(i, innerContainer.Count);
                        drawRot = base.DrawRotation;
                        groundPos = Pawn.Drawer.DrawPos;
                        DrawShadow(groundPos, 1.5f);
                    }

                    if (!isCaught) drawPos.x += 1;

                    if (thrownThing.def.projectile != null && thrownThing.def.projectile.beamMoteDef != null)
                    {
                        DrawBeamMote(thrownThing, drawPos, drawRot, isCaught);
                    }
                    else if (thrownThing is Corpse corpse)
                    {
                        if (corpse.InnerPawn == null) continue;
                        Rot4 rotation = isCaught ?
                            Rot4.FromAngleFlat(worldRotations[i].eulerAngles.y) :
                            parent.pawn.Rotation;
                        corpse.InnerPawn.Drawer?.renderer?.RenderPawnAt(drawPos, rotation, true);
                    }
                    else if (thrownThing is Pawn pawn)
                    {
                        if (pawn.Drawer?.renderer == null) continue;
                        Rot4 rotation = isCaught ?
                            Rot4.FromAngleFlat(worldRotations[i].eulerAngles.y) :
                            parent.pawn.Rotation;
                        pawn.Drawer.renderer.RenderPawnAt(drawPos, rotation, true);
                    }
                    else if (thrownThing?.Graphic is { } graphic)
                    {
                        if (!isCaught) drawPos.x += thrownThing.def.Size.x;

                        UnityEngine.Graphics.DrawMesh(
                            MeshPool.GridPlane(graphic.drawSize),
                            drawPos,
                            drawRot,
                            graphic.MatSingle,
                            0
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ProjectileHolder.DrawWornExtras: {ex}");
            }
        }

        private void DrawBeamMote(Thing beamThing, Vector3 drawPos, Quaternion drawRot, bool isCaught)
        {
            try
            {
                var beamMoteDef = beamThing.def.projectile.beamMoteDef;
                if (beamMoteDef == null || beamMoteDef.graphic == null) return;

                Graphic beamGraphic = beamMoteDef.graphic;

                float scale = isCaught ? 0.5f : 0.8f;

                Matrix4x4 matrix = Matrix4x4.TRS(
                    drawPos,
                    drawRot,
                    new Vector3(scale, scale, scale)
                );

                UnityEngine.Graphics.DrawMesh(
                    MeshPool.plane10,
                    matrix,
                    beamGraphic.MatSingle,
                    0
                );

                if (isCaught)
                {
                    DrawBeamMoteGlow(drawPos, beamGraphic);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error drawing beam mote: {ex}");
            }
        }

        private void DrawBeamMoteGlow(Vector3 position, Graphic beamGraphic)
        {
            Material glowMaterial = beamGraphic.MatSingle;
            if (glowMaterial != null)
            {
                Matrix4x4 glowMatrix = Matrix4x4.TRS(
                    position,
                    Quaternion.identity,
                    new Vector3(1.2f, 1.2f, 1.2f)
                );

                Color originalColor = glowMaterial.color;
                glowMaterial.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f);

                UnityEngine.Graphics.DrawMesh(
                    MeshPool.plane10,
                    glowMatrix,
                    glowMaterial,
                    0
                );

                glowMaterial.color = originalColor;
            }
        }

        private Vector3 CalculateOrbitingPosition(int index, int totalItems)
        {
            Vector3 basePos = Pawn.Drawer.DrawPos;

            float angle = (Time.realtimeSinceStartup * 2f + (index * Mathf.PI * 2f / totalItems)) % (Mathf.PI * 2f);
            float radius = 1.5f;
            float heightOffset = Mathf.Sin(Time.realtimeSinceStartup * 3f + index) * 0.2f;

            basePos.x += Mathf.Cos(angle) * radius;
            basePos.z += Mathf.Sin(angle) * radius;
            basePos.y += heightOffset + 1f;

            return basePos;
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            foreach (var gizmo in base.CompGetGizmos())
            {
                yield return gizmo;
            }

            if (innerContainer.Count > 0)
            {
                Thing currentProjectile = innerContainer[currentLaunchIndex];
                Texture2D gizmoIcon = GetProjectileIcon(currentProjectile);

                yield return new Command_Action
                {
                    defaultLabel = $"Force.LaunchNext".Translate(currentLaunchIndex + 1, innerContainer.Count),
                    defaultDesc = "Force.LaunchNextDesc".Translate(),
                    icon = gizmoIcon ?? ContentFinder<Texture2D>.Get("UI/Commands/Attack"),
                    action = () =>
                    {
                        currentLaunchIndex = (currentLaunchIndex + 1) % innerContainer.Count;
                    }
                };

                if (innerContainer.Count > 1)
                {
                    Texture2D launchAllIcon = GetProjectileIcon(innerContainer[0]) ??
                                             ContentFinder<Texture2D>.Get("UI/Commands/Attack");

                    yield return new Command_Action
                    {
                        defaultLabel = "Force.LaunchAll".Translate(innerContainer.Count),
                        defaultDesc = "Force.LaunchAllDesc".Translate(innerContainer.Count),
                        icon = launchAllIcon,
                        action = () =>
                        {
                            LaunchAllProjectiles();
                        }
                    };
                }
            }
        }

        private Texture2D GetProjectileIcon(Thing projectile)
        {
            if (projectile == null) return null;

            try
            {
                if (projectile.def.projectile?.beamMoteDef?.graphic != null)
                {
                    var moteGraphic = projectile.def.projectile.beamMoteDef.graphic;
                    if (moteGraphic.MatSingle?.mainTexture != null)
                    {
                        return (Texture2D)moteGraphic.MatSingle.mainTexture;
                    }
                }

                if (projectile.Graphic?.MatSingle?.mainTexture != null)
                {
                    return (Texture2D)projectile.Graphic.MatSingle.mainTexture;
                }

                if (projectile.def.uiIcon != null)
                {
                    return projectile.def.uiIcon;
                }

                if (projectile.def.graphicData?.texPath != null)
                {
                    return ContentFinder<Texture2D>.Get(projectile.def.graphicData.texPath);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to get projectile icon for {projectile.def.defName}: {ex.Message}");
            }

            return null;
        }

        public override void LaunchProjectileAt(IntVec3 targetCell, LocalTargetInfo targetInfo)
        {
            if (!CanLaunchProjectile || !targetCell.IsValid || innerContainer.Count == 0)
            {
                Log.Warning("[ForceThrow] Cannot launch - invalid conditions");
                return;
            }

            if (parent.pawn == null || parent.pawn.Map == null)
            {
                Log.Warning("[ForceThrow] Cannot launch - null pawn or map");
                return;
            }

            if (currentLaunchIndex >= innerContainer.Count)
            {
                currentLaunchIndex = 0;
            }

            Thing heldThing = innerContainer[currentLaunchIndex];
            if (heldThing == null) return;

            bool isCaught = currentLaunchIndex < isCaughtProjectiles.Count && isCaughtProjectiles[currentLaunchIndex];

            if (heldThing.def.projectile != null)
            {
                IntVec3 spawnPos;
                if (isCaught && currentLaunchIndex < worldPositions.Count)
                {
                    spawnPos = worldPositions[currentLaunchIndex].ToIntVec3();
                    if (!spawnPos.InBounds(parent.pawn.Map))
                    {
                        spawnPos = parent.pawn.Position;
                    }
                }
                else
                {
                    spawnPos = parent.pawn.Position;
                }

                Projectile projectile = (Projectile)GenSpawn.Spawn(heldThing.def, spawnPos, parent.pawn.Map);
                if (projectile != null)
                {
                    projectile.Launch(
                        launcher: parent.pawn,
                        usedTarget: targetInfo,
                        intendedTarget: targetInfo.Thing,
                        hitFlags: ProjectileHitFlags.All,
                        preventFriendlyFire: false,
                        equipment: null
                    );

                    RemoveProjectileAtIndex(currentLaunchIndex);

                    if (innerContainer.Count > 0)
                    {
                        currentLaunchIndex = Mathf.Min(currentLaunchIndex, innerContainer.Count - 1);
                    }
                    else
                    {
                        currentLaunchIndex = 0;
                    }
                }
            }
            else
            {
                base.LaunchProjectileAt(targetCell, targetInfo);
            }

            if (Props.soundCast != null)
            {
                Props.soundCast.PlayOneShot(new TargetInfo(parent.pawn.Position, parent.pawn.Map));
            }

            if (innerContainer.Count == 0)
            {
                Pawn.health.RemoveHediff(this.parent);
            }
        }

        private void LaunchAllProjectiles()
        {
            if (!Pawn.Drafted || !Pawn.Spawned || innerContainer.Count == 0) return;

            for (int i = innerContainer.Count - 1; i >= 0; i--)
            {
                Thing heldThing = innerContainer[i];
                if (heldThing?.def.projectile != null)
                {
                    IntVec3 randomTarget = Pawn.Position + new IntVec3(
                        Rand.RangeInclusive(-10, 10),
                        0,
                        Rand.RangeInclusive(-10, 10)
                    );

                    if (randomTarget.InBounds(Pawn.Map))
                    {
                        LaunchSpecificProjectile(i, randomTarget);
                    }
                }
            }
        }

        private void LaunchSpecificProjectile(int index, IntVec3 targetCell)
        {
            if (index >= innerContainer.Count) return;

            Thing heldThing = innerContainer[index];
            if (heldThing?.def.projectile == null) return;

            bool isCaught = index < isCaughtProjectiles.Count && isCaughtProjectiles[index];

            IntVec3 spawnPos;
            if (isCaught && index < worldPositions.Count)
            {
                spawnPos = worldPositions[index].ToIntVec3();
                if (!spawnPos.InBounds(Pawn.Map))
                {
                    spawnPos = Pawn.Position;
                }
            }
            else
            {
                spawnPos = Pawn.Position;
            }

            Projectile projectile = (Projectile)GenSpawn.Spawn(heldThing.def, spawnPos, Pawn.Map);
            if (projectile != null)
            {
                projectile.Launch(
                    launcher: Pawn,
                    usedTarget: new LocalTargetInfo(targetCell),
                    intendedTarget: null,
                    hitFlags: ProjectileHitFlags.All,
                    preventFriendlyFire: false,
                    equipment: null
                );

                RemoveProjectileAtIndex(index);
            }
        }

        private void RemoveProjectileAtIndex(int index)
        {
            if (index < innerContainer.Count)
            {
                innerContainer.RemoveAt(index);

                if (index < worldPositions.Count) worldPositions.RemoveAt(index);
                if (index < worldRotations.Count) worldRotations.RemoveAt(index);
                if (index < isCaughtProjectiles.Count) isCaughtProjectiles.RemoveAt(index);
            }
        }

        public override bool TryReleaseThing(out Thing releasedThing)
        {
            if (innerContainer.Count > 0)
            {
                releasedThing = innerContainer[0];
                innerContainer.Clear();
                worldPositions.Clear();
                worldRotations.Clear();
                isCaughtProjectiles.Clear();
                currentLaunchIndex = 0;
                return true;
            }

            releasedThing = null;
            return false;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (parent.pawn.Map != null && Props.catchProjectile)
            {
                try
                {
                    var radialThings = GenRadial.RadialDistinctThingsAround(Pawn.Position, Pawn.Map, 3f + 1, true).ToList();

                    foreach (var thing in radialThings)
                    {
                        if (thing is Projectile projectile)
                        {
                            if (CanCatchProjectile(projectile))
                            {
                                TryCatchProjectile(projectile);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[ProjectileHolder] Error in CompPostTick: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        public override bool CanCatchProjectile(Projectile projectile)
        {
            var cell = ((Vector3)NonPublicFields.Projectile_origin.GetValue(projectile)).Yto0().ToIntVec3();
            return Vector3.Distance(projectile.ExactPosition.Yto0(), Pawn.DrawPos.Yto0()) <= 3 &&
                !GenRadial.RadialCellsAround(Pawn.Position, 3, true).ToList().Contains(cell);
        }

        public override bool TryCatchProjectile(Projectile projectile)
        {
            if (!Props.catchProjectile || projectile == null || projectile.Destroyed)
                return false;

            if (projectile is Projectile_ForceThrow forceThrow)
            {
                return TryCatchForceThrowProjectile(forceThrow);
            }

            Thing thingToCatch = ThingMaker.MakeThing(projectile.def);
            if (thingToCatch == null) return false;

            if (innerContainer.TryAdd(thingToCatch))
            {
                worldPositions.Add(projectile.ExactPosition);
                worldRotations.Add(projectile.ExactRotation);
                isCaughtProjectiles.Add(true);
                projectile.Destroy();
                return true;
            }

            return false;
        }

        private bool TryCatchForceThrowProjectile(Projectile_ForceThrow forceThrow)
        {
            try
            {
                Thing carriedThing = forceThrow.GetDirectlyHeldThings().FirstOrDefault();
                if (carriedThing == null || carriedThing.Destroyed) return false;

                if (innerContainer.TryAddOrTransfer(carriedThing))
                {
                    worldPositions.Add(forceThrow.ExactPosition);
                    worldRotations.Add(forceThrow.ExactRotation);
                    isCaughtProjectiles.Add(true);
                    forceThrow.Destroy();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error catching ForceThrow projectile: {ex}");
            }

            return false;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();

            Scribe_Collections.Look(ref worldPositions, "worldPositions", LookMode.Value);
            Scribe_Collections.Look(ref worldRotations, "worldRotations", LookMode.Value);
            Scribe_Collections.Look(ref isCaughtProjectiles, "isCaughtProjectiles", LookMode.Value);
            Scribe_Values.Look(ref currentLaunchIndex, "currentLaunchIndex", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                worldPositions ??= new List<Vector3>();
                worldRotations ??= new List<Quaternion>();
                isCaughtProjectiles ??= new List<bool>();
            }
        }

        public override string CompTipStringExtra
        {
            get
            {
                if (innerContainer.Count == 0) return "";

                string tip = $"Holding {innerContainer.Count} projectiles:";
                for (int i = 0; i < Mathf.Min(innerContainer.Count, 3); i++)
                {
                    tip += $"\n- {innerContainer[i].LabelShortCap}";
                }

                if (innerContainer.Count > 3)
                {
                    tip += $"\n- ... and {innerContainer.Count - 3} more";
                }

                tip += $"\nNext to launch: {innerContainer[currentLaunchIndex].LabelShortCap}";

                return tip;
            }
        }
    }

    internal class HediffCompProperties_ProjectileHolder : HediffCompProperties_ThingHolder
    {
        public int maxProjectiles = 1000;

        public HediffCompProperties_ProjectileHolder()
        {
            compClass = typeof(HediffComp_ProjectileHolder);
            catchProjectile = true;
        }
    }
}