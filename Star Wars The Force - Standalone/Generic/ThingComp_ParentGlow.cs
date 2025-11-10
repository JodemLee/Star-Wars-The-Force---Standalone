using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Generic
{
    internal class ThingComp_ParentGlow : CompGlower
    {
        private IntVec3 previousPosition;
        private ColorInt defaultColorInt = new ColorInt(Color.white);

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            if (parent != null)
            {
                ApplyColor(parent.DrawColor);
            }
        }

        public override void Notify_ColorChanged()
        {
            if (parent != null)
            {
                ApplyColor(parent.DrawColor);
            }
        }


        private void ApplyColor(Color color)
        {
            defaultColorInt = new ColorInt(color);
            this.GlowColor = defaultColorInt;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref defaultColorInt, "defaultColorInt", new ColorInt(Color.white));
        }

        public void UpdateGlowerColor(ColorInt newColor)
        {
            GlowColor = newColor;
            UpdateLit(parent.MapHeld);
        }

        public override void CompTick()
        {
            if (parent.Spawned)
            {
                var position = parent.Position;
                if (position != previousPosition)
                {
                    ForceRegister(parent.Map);
                    previousPosition = position;
                }
            }
        }
    }
}
