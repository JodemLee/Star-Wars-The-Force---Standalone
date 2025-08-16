using Verse;
using Verse.AI.Group;

namespace TheForce_Standalone
{
    public static class PositionUtils
    {
        // Checks if a position is valid (walkable and in bounds)
        public static bool CheckValidPosition(IntVec3 position, Map map)
        {
            return position.InBounds(map) && position.Standable(map);
        }

        // Finds a valid position close to the original position using the offset
        public static IntVec3 FindValidPosition(IntVec3 originalPosition, IntVec3 offset, Map map)
        {
            IntVec3 closestValidPosition = originalPosition;
            float closestDistanceSquared = float.MaxValue;

            for (int i = 1; i <= 3; i++)
            {
                IntVec3 newPosition = originalPosition + (offset * i);
                if (newPosition.InBounds(map) && newPosition.Standable(map))
                {
                    // Calculate distance to original position
                    float distanceSquared = newPosition.DistanceToSquared(originalPosition);
                    if (distanceSquared < closestDistanceSquared)
                    {
                        closestValidPosition = newPosition;
                        closestDistanceSquared = distanceSquared;
                    }
                }
            }
            return closestValidPosition;
        }

        public static bool IsCarryingWeaponOpenly(this Pawn pawn)
        {
            if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing != null)
            {
                return false;
            }
            if (pawn.Drafted)
            {
                return true;
            }
            if ((pawn.CurJob?.def?.alwaysShowWeapon).GetValueOrDefault())
            {
                return true;
            }
            if ((pawn.mindState?.duty?.def?.alwaysShowWeapon).GetValueOrDefault())
            {
                return true;
            }
            Lord lord = pawn.GetLord();
            if (lord != null && lord.LordJob != null && lord.LordJob.AlwaysShowWeapon)
            {
                return true;
            }
            return false;
        }

        public static string AsCompassDirection(this Rot4 rotation)
        {
            return rotation == Rot4.South ? "south" :
                   rotation == Rot4.North ? "north" :
                   rotation == Rot4.East ? "east" : "west";
        }
    }
}