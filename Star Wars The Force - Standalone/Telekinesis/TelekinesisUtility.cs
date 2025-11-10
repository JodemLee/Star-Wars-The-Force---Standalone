using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Telekinesis
{
    public static class TelekinesisUtility
    {

        private const float BasePushPullDistance = 3f;
        private const float MinDistanceForDamage = 1f;
        private const float DamagePerCell = 2f;
        private const float MassDamageFactor = 1f;

        public static IntVec3 CalculatePushPosition(IntVec3 casterPos, IntVec3 targetPos, int multiplier, Map map, out bool hitWall)
        {

            int minPushDistance = 3;
            int actualMultiplier = multiplier < minPushDistance ? minPushDistance : multiplier;

            IntVec3 direction = targetPos - casterPos;
            if (direction.LengthHorizontal == 0)
            {
                hitWall = false;
                return targetPos + new IntVec3(1, 0, 0) * actualMultiplier;
            }

            IntVec3 normalizedDirection = new IntVec3(
                Math.Sign(direction.x),
                0,
                Math.Sign(direction.z)
            );

            IntVec3 pushPos = targetPos + normalizedDirection * actualMultiplier;
            return ValidateFinalPosition(pushPos, casterPos, targetPos, map, out hitWall);
        }

        public static IntVec3 CalculatePullPosition(IntVec3 casterPos, IntVec3 targetPos, float maxDistance, Map map)
        {
            IntVec3 direction = casterPos - targetPos;
            if (direction.LengthHorizontal == 0)
                return casterPos;
            IntVec3 normalizedDirection = direction.Normalized();
            IntVec3 pullOffset = new IntVec3(
                Mathf.RoundToInt(normalizedDirection.x * maxDistance),
                0,
                Mathf.RoundToInt(normalizedDirection.z * maxDistance)
            );
            IntVec3 pullPos = targetPos + pullOffset;
            float distanceToCaster = casterPos.DistanceTo(targetPos);
            if (distanceToCaster <= maxDistance)
            {
                pullPos = casterPos;
            }

            return ValidateFinalPosition(pullPos, casterPos, targetPos, map, out bool hitwall);
        }

        public static IntVec3 Normalized(this IntVec3 vec)
        {
            float length = vec.LengthHorizontal;
            if (length > 0.01f)
            {
                return new IntVec3(
                    (int)Mathf.Sign(vec.x),
                    0,
                    (int)Mathf.Sign(vec.z)
                );
            }
            return new IntVec3(1, 0, 0);
        }

        public static IntVec3 ValidateFinalPosition(IntVec3 proposedPos, IntVec3 casterPos, IntVec3 originPos, Map map, out bool hitWall)
        {
            hitWall = false;

            // Immediate map boundary check
            if (!proposedPos.InBounds(map))
                return originPos;

            IntVec3 lastValid = originPos;
            bool obstructionFound = false;

            // Check each cell along the path
            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(casterPos, proposedPos))
            {
                if (!cell.InBounds(map))
                {
                    obstructionFound = true;
                    break;
                }
                Building building = (Building)cell.GetRoofHolderOrImpassable(map);
                if (building != null)
                {
                    hitWall = true;
                    obstructionFound = true;

                    // You could add impact effects here:
                    if (building.def.useHitPoints)
                    {
                        building.TakeDamage(new DamageInfo(
                            DamageDefOf.Blunt,
                            Mathf.RoundToInt(CalculateDistanceBasedDamage(10f, originPos, cell)),
                            -1f,
                            instigator: null
                        ));
                    }
                    break;
                }
                lastValid = cell;
            }
            if (obstructionFound)
            {
                return lastValid;
            }
            if (PositionUtils.CheckValidPosition(proposedPos, map))
            {
                return proposedPos;
            }
            else
            {
                hitWall = true; // Mark as hit if we can't reach proposed position
                return PositionUtils.FindValidPosition(originPos, proposedPos - originPos, map);
            }
        }

        // ========================
        // Damage Calculations
        // ========================

        public static float CalculateDistanceBasedDamage(float baseDamage, IntVec3 start, IntVec3 end)
        {
            float distance = (start - end).LengthHorizontal;
            return baseDamage + (distance * DamagePerCell);
        }

        public static float CalculateKineticDamage(float throwSpeed, Thing thing)
        {
            float mass = thing.GetStatValue(StatDefOf.Mass);
            if (thing is ThingWithComps twc && twc.def.CountAsResource)
                mass *= thing.stackCount;
            float kineticEnergy = 0.5f * mass * throwSpeed * throwSpeed;
            return kineticEnergy;
        }

        public static float CalculateMassBasedSpeed(float baseDamage, Thing thing)
        {
            float mass = thing.GetStatValue(StatDefOf.Mass);
            if (thing is ThingWithComps twc && twc.def.CountAsResource)
                mass *= thing.stackCount;

            // Speed decreases with mass but less dramatically
            return baseDamage / (1 + (mass * 0.05f));
        }

        public static List<IntVec3> GetConeCells(IntVec3 casterPos, IntVec3 targetPos, float angle, float radius, Map map)
        {
            List<IntVec3> cells = new List<IntVec3>();
            Vector3 start = casterPos.ToVector3Shifted();
            Vector3 targetDir = (targetPos.ToVector3Shifted() - start).normalized;

            foreach (IntVec3 cell in GenRadial.RadialPattern)
            {
                if (cell.LengthHorizontal > radius) continue;

                IntVec3 worldCell = casterPos + cell;
                if (!worldCell.InBounds(map)) continue;

                Vector3 cellDir = (worldCell.ToVector3Shifted() - start).normalized;
                if (Vector3.Angle(targetDir, cellDir) <= angle / 2f)
                    cells.Add(worldCell);
            }
            return cells;
        }

        public static void LaunchPawn(Pawn pawn, IntVec3 destination, Ability ability, ThingDef flyerDef, Map map, bool hitWall = false, IntVec3? wallImpactPos = null)
        {
            try
            {
                if (flyerDef == null || map == null || !destination.IsValid)
                    return;

                Thing flyerThing = PawnFlyer.MakeFlyer(flyerDef, pawn, destination, null, null);

                if (flyerThing is TelekinesisPawnFlyer abilityFlyer)
                {
                    abilityFlyer.ability = ability;
                    abilityFlyer.casterPos = ability.pawn.Position;
                    abilityFlyer.originalDestination = pawn.Position;
                    abilityFlyer.DestinationCell = destination;
                    abilityFlyer.hitWallDuringFlight = hitWall;
                    abilityFlyer.impactPosition = wallImpactPos ?? destination;
                    GenSpawn.Spawn(abilityFlyer, destination, map);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error launching pawn: {ex}");
            }
        }


        public static bool TryMinifyBuilding(Building building, out MinifiedThing minified)
        {
            minified = building.def.Minifiable ? building.Uninstall() : null;
            return minified != null;
        }

        public static void LaunchThing(Thing thing, LocalTargetInfo usedThing, IntVec3 destination, Pawn caster, ThingDef projectileDef, float baseSpeed)
        {
            if (projectileDef == null || caster.Map == null || thing == null || !thing.Spawned)
                return;

            var projectile = (Projectile_ForceThrow)GenSpawn.Spawn(projectileDef, thing.Position, caster.Map);
            float throwSpeed = CalculateMassBasedSpeed(baseSpeed, thing);
            float finalDamage = CalculateKineticDamage(throwSpeed, thing);
            projectile.DamageAmount = (int)finalDamage;
            projectile.destCell = destination;


            thing.DeSpawn(DestroyMode.Vanish);
            projectile.GetDirectlyHeldThings().TryAddOrTransfer(thing);
            projectile.Launch(caster, usedThing, thing, ProjectileHitFlags.NonTargetPawns);
        }

        public static void LaunchThingNoDespawn(Thing thing, LocalTargetInfo usedThing, IntVec3 destination, Pawn caster, ThingDef projectileDef, float baseSpeed, IntVec3 spawnPosition)
        {
            if (projectileDef == null || caster.Map == null)
                return;

            var projectile = (Projectile_ForceThrow)GenSpawn.Spawn(projectileDef, spawnPosition, caster.Map);
            float throwSpeed = CalculateMassBasedSpeed(baseSpeed, thing);
            float finalDamage = CalculateKineticDamage(throwSpeed, thing);
            projectile.DamageAmount = (int)finalDamage;
            projectile.destCell = destination;

            projectile.GetDirectlyHeldThings().TryAddOrTransfer(thing);
            projectile.Launch(caster, usedThing, thing, ProjectileHitFlags.NonTargetPawns);
        }

        public static IntVec3 normalized(this IntVec3 vec)
        {
            float length = vec.LengthHorizontal;
            if (length > 0.0001f)
                return new IntVec3(Mathf.RoundToInt(vec.x / length), 0, Mathf.RoundToInt(vec.z / length));
            return vec;
        }
    }
}