using RimWorld;
using Verse;

namespace TheForce_Standalone.PawnRenderNodes
{
    internal class PawnSithScarDrawer : PawnScarDrawer
    {
        protected override string ScarTexturePath => "Things/Pawn/Overlays/SithExperimentation/ScarB";

        protected override float ScaleFactor => 0.5f;

        public PawnSithScarDrawer(Pawn pawn)
            : base(pawn)
        {
        }
    }
}
