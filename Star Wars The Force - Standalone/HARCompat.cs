using AlienRace;
using Verse;

namespace TheForce_Standalone
{
    [StaticConstructorOnStartup]
    public static class HARCompat
    {
        public static bool HARActive = ModsConfig.IsActive("erdelf.humanoidalienraces");

        public static void CopyAlienData(this Pawn pawn, Pawn pawn1)
        {
           AlienPartGenerator.AlienComp.CopyAlienData(pawn, pawn1);
        }
    }
}
