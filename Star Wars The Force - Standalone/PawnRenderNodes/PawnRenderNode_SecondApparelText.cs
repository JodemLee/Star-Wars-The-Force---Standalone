using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.PawnRenderNodes
{
    public class PawnRenderNodeProperties_DualTexture : PawnRenderNodeProperties
    {
        public string backGraphicPath;

        public PawnRenderNodeProperties_DualTexture()
        {

        }
    }

    public class PawnRenderNode_Apparel_DualTexture : PawnRenderNode_Apparel
    {
        private Graphic backGraphic;
        public bool isBackLayer;
        public PawnRenderNode_Apparel_DualTexture(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree, null)
        {
        }

        public PawnRenderNode_Apparel_DualTexture(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel)
            : base(pawn, props, tree, apparel)
        {
        }

        public PawnRenderNode_Apparel_DualTexture(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel, bool useHeadMesh)
            : base(pawn, props, tree, apparel, useHeadMesh)
        {
        }


        private Shader GetShader()
        {
            Shader shader = ShaderDatabase.Cutout;
            if (apparel.StyleDef?.graphicData.shaderType != null)
            {
                shader = apparel.StyleDef.graphicData.shaderType.Shader;
            }
            else if ((apparel.StyleDef is null && apparel.def.apparel.useWornGraphicMask) || (apparel.StyleDef is not null && apparel.StyleDef.UseWornGraphicMask))
            {
                shader = ShaderDatabase.CutoutComplex;
            }
            return shader;
        }

        protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            isBackLayer = false;
            var dualProps = props as PawnRenderNodeProperties_DualTexture;
            foreach (var g in base.GraphicsFor(pawn))
            {
                var graphic = GraphicDatabase.Get<Graphic_Multi>(
                    dualProps.backGraphicPath + "_" + pawn?.story.bodyType.defName,
                    GetShader(),
                    pawn.Drawer.renderer.renderTree.BodyGraphic.drawSize,
                    GetColor());

                yield return graphic;
            }

            if (ApparelGraphicRecordGetter.TryGetGraphicApparel(apparel, tree.pawn.story.bodyType, pawn.Drawer.renderer.StatueColor.HasValue, out var rec))
            {
                yield return rec.graphic;
            }
        }

        private Color GetColor()
        {
            return apparel.DrawColor;
        }


    }

    public class PawnRenderNodeWorker_Apparel_DualTexture : PawnRenderNodeWorker_Apparel_Body
    {
        public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
        {
            float baseLayer = base.LayerFor(node, parms);
            var dualNode = node as PawnRenderNode_Apparel_DualTexture;
            return (dualNode?.isBackLayer ?? false) ? baseLayer - 0.1f : baseLayer + 0.1f;
        }
    }

    public class PawnRenderNode_ApparelHead_DualTexture : PawnRenderNode_Apparel
    {
        private Graphic backGraphic;
        public bool isBackLayer;
        public PawnRenderNode_ApparelHead_DualTexture(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree, null)
        {
        }

        public PawnRenderNode_ApparelHead_DualTexture(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel)
            : base(pawn, props, tree, apparel)
        {
        }

        public PawnRenderNode_ApparelHead_DualTexture(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel, bool useHeadMesh)
            : base(pawn, props, tree, apparel, useHeadMesh)
        {
        }


        private Shader GetShader()
        {
            Shader shader = ShaderDatabase.Cutout;
            if (apparel.StyleDef?.graphicData.shaderType != null)
            {
                shader = apparel.StyleDef.graphicData.shaderType.Shader;
            }
            else if ((apparel.StyleDef is null && apparel.def.apparel.useWornGraphicMask) || (apparel.StyleDef is not null && apparel.StyleDef.UseWornGraphicMask))
            {
                shader = ShaderDatabase.CutoutComplex;
            }
            return shader;
        }

        protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            isBackLayer = false;
            var dualProps = props as PawnRenderNodeProperties_DualTexture;
            foreach (var g in base.GraphicsFor(pawn))
            {
                var graphic = GraphicDatabase.Get<Graphic_Multi>(
                    dualProps.backGraphicPath + "_" + pawn.Rotation.ToStringWord().ToLower(),
                    GetShader(),
                    apparel.def.graphicData.drawSize,
                    GetColor());

                yield return graphic;
            }

            if (ApparelGraphicRecordGetter.TryGetGraphicApparel(apparel, tree.pawn.story.bodyType, pawn.Drawer.renderer.StatueColor.HasValue, out var rec))
            {
                yield return rec.graphic;
            }
        }

        private Color GetColor()
        {
            return apparel.DrawColor;
        }
    }

    public class PawnRenderNodeWorker_ApparelHead_DualTexture : PawnRenderNodeWorker_Apparel_DualTexture
    {
        public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
        {
            float baseLayer = base.LayerFor(node, parms);
            var dualNode = node as PawnRenderNode_Apparel_DualTexture;
            return (dualNode?.isBackLayer ?? false) ? baseLayer - 0.1f : baseLayer + 0.1f;
        }

        public override bool CanDrawNow(PawnRenderNode n, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(n, parms))
            {
                return false;
            }
            if (!parms.flags.FlagSet(PawnRenderFlags.Clothes) || !parms.flags.FlagSet(PawnRenderFlags.Headgear))
            {
                return false;
            }
            if (!HeadgearVisible(parms))
            {
                return false;
            }
            if (parms.Portrait && Prefs.HatsOnlyOnMap)
            {
                return parms.flags.FlagSet(PawnRenderFlags.StylingStation);
            }
            return true;
        }

        public static bool HeadgearVisible(PawnDrawParms parms)
        {
            if (!parms.flags.FlagSet(PawnRenderFlags.Clothes) || !parms.flags.FlagSet(PawnRenderFlags.Headgear))
            {
                return false;
            }
            if (!parms.Portrait && parms.bed != null && !parms.bed.def.building.bed_showSleeperBody)
            {
                return false;
            }
            if (parms.Portrait && Prefs.HatsOnlyOnMap)
            {
                return parms.flags.FlagSet(PawnRenderFlags.StylingStation);
            }
            return true;
        }
    }
}
