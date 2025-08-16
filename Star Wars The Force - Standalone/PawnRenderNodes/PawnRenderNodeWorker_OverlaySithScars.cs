using RimWorld;
using Verse;

namespace TheForce_Standalone.PawnRenderNodes
{
    internal class PawnRenderNodeWorker_OverlaySithScars : PawnRenderNodeWorker_Overlay
    {

        protected override PawnOverlayDrawer OverlayDrawer(Pawn pawn)
        {

            return new PawnSithScarDrawer(pawn);
        }

        private bool ShouldHaveSithScars(Pawn pawn)
        {
            return true;
        }

        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (base.CanDrawNow(node, parms))
            {
                return parms.rotDrawMode != RotDrawMode.Dessicated;
            }
            return false;
        }
    }

}
