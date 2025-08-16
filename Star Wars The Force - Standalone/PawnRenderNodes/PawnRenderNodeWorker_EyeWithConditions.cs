using UnityEngine;
using Verse;

namespace TheForce_Standalone.PawnRenderNodes
{
    public class PawnRenderNodeWorker_ConditionalEye : PawnRenderNodeWorker_Eye
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms))
                return false;

            if (node.Props is PawnRenderNodeProperties_ConditionalEye props)
            {
                // Check mod setting
                if (!Force_ModSettings.darksideVisuals)
                    return false;

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

            if (node.Props is PawnRenderNodeProperties_ConditionalEye props &&
                props.requiredHediff != null)
            {
                var hediff = parms.pawn.health?.hediffSet?.GetFirstHediffOfDef(props.requiredHediff);
                if (hediff != null)
                {
                    // Calculate opacity based on severity (0 to requiredSeverity maps to 0 to 1)
                    float opacity = Mathf.Clamp01(hediff.Severity / props.requiredSeverity);

                    // Create a new material instance so we don't modify the original
                    Material newMat = new Material(material);

                    // Adjust the color's alpha channel
                    Color color = newMat.color;
                    color.a = opacity;
                    newMat.color = color;

                    return newMat;
                }
            }

            return material;
        }
    }

    public class PawnRenderNodeProperties_ConditionalEye : PawnRenderNodeProperties_Eye
    {
        public HediffDef requiredHediff;
        public float requiredSeverity = 0.5f;
    }
}
