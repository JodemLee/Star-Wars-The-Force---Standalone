using UnityEngine;
using Verse;

namespace TheForce_Standalone.Generic
{
    public static class ForceGhostUtility
    {
        public static Color? GetGhostColor(Pawn pawn)
        {
            if (pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Ghost) != null)
            {
                return new Color(150, 200, 255, 178f) / 255; 
            }
            if (pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_SithGhost) != null)
            {
                return new Color(250, 106, 63f, 178f) / 255;
            }
            if (pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_SithZombie) != null)
            {
                return new Color(0.6f, 0.6f, 0.6f, 1f);
            }
            return null;
        }

        public static bool IsForceGhost(Pawn pawn)
        {
            return GetGhostColor(pawn) != null;
        }
    }
}
