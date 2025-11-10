using TheForce_Standalone.Alignment;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.PawnRenderNodes
{
    internal class PawnRenderNodeWorker_AlignmentConditions : PawnRenderNodeWorker_Eye
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms))
                return false;

            // Check the per-pawn setting first
            var forceComp = parms.pawn.GetComp<CompClass_ForceUser>();
            if (forceComp?.Alignment?.enableDarkSideVisuals == false)
                return false;

            if (!Force_ModSettings.darksideVisuals)
                return false;

            if (node.Props is PawnRenderNodeProperties_ConditionalEye props)
            {
                if (props.requiredHediff != null)
                {
                    var hediff = parms.pawn.health?.hediffSet?.GetFirstHediffOfDef(props.requiredHediff);
                    if (hediff == null)
                        return false;
                }
            }

            return true;
        }

        public override Material GetFinalizedMaterial(PawnRenderNode node, PawnDrawParms parms)
        {
            Material material = base.GetFinalizedMaterial(node, parms);

            if (node.Props is PawnRenderNodeProperties_AlignmentCondition props)
            {
                var forceComp = parms.pawn.GetComp<CompClass_ForceUser>();
                if (forceComp == null) return material;

                float currentAlignment = props.alignment == AlignmentType.Darkside
                    ? forceComp.Alignment.DarkSideAttunement
                    : forceComp.Alignment.LightSideAttunement;

                float opacity = Mathf.Clamp01(currentAlignment / props.requiredAlignment);
                Material newMat = new Material(material);
                Color color = newMat.color;
                color.a = opacity;
                newMat.color = color;
                return newMat;

            }

            return material;
        }
    }

    public class PawnRenderNodeProperties_AlignmentCondition : PawnRenderNodeProperties_Eye
    {
        public AlignmentType alignment;
        public float requiredAlignment = 1;
    }
}
