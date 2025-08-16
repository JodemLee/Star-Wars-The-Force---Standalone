using Verse;

namespace TheForce_Standalone.PawnRenderNodes
{
    internal class PawnRenderNodeWorker_AutomaticHiding : PawnRenderNodeWorker_Apparel_Head
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            Pawn pawn = parms.pawn;
            IntVec3 position = pawn.Position;
            Map map = pawn.Map;

            if (!base.CanDrawNow(node, parms) || !parms.flags.FlagSet(PawnRenderFlags.Clothes) || position.Roofed(map) || map.weatherManager.RainRate <= 0.01f)
            {
                return false;
            }
            return true;
        }
    }
}
