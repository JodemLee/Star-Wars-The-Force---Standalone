using RimWorld;
using TheForce_Standalone.Alignment;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    public class ForceUserAlignment
    {
        private readonly CompClass_ForceUser parent;
        private float darkSideAttunement;
        private float lightSideAttunement;
        public CrystalTransformationSystem CrystalTransformation { get; private set; }

        public bool enableDarkSideVisuals = true;

        public float DarkSideAttunement
        {
            get => darkSideAttunement;
            set
            {
                float oldValue = darkSideAttunement;
                darkSideAttunement = Mathf.Clamp(value, 0f, 1000f);
                if (!Mathf.Approximately(oldValue, darkSideAttunement))
                {
                    parent.MarkForRedraw();
                }
            }
        }

        public float LightSideAttunement
        {
            get => lightSideAttunement;
            set
            {
                float oldValue = lightSideAttunement;
                lightSideAttunement = Mathf.Clamp(value, 0f, 1000f);
                if (!Mathf.Approximately(oldValue, lightSideAttunement))
                {
                    parent.MarkForRedraw();
                }
            }
        }

        public float AlignmentBalance => (lightSideAttunement - darkSideAttunement) / 100f;

        public ForceUserAlignment(CompClass_ForceUser parent)
        {
            this.parent = parent;
        }

        public void Reset()
        {
            darkSideAttunement = 0f;
            lightSideAttunement = 0f;
        }

        public void Initialize()
        {
            if (parent?.Pawn == null) return;

            var forceUserExt = parent.Pawn.kindDef?.GetModExtension<ModExtension_ForceUser>();

            if (forceUserExt != null)
            {
                DarkSideAttunement = forceUserExt.darkSideRange.RandomInRange;
                LightSideAttunement = forceUserExt.lightSideRange.RandomInRange;
            }
            CrystalTransformation = new CrystalTransformationSystem(parent);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref darkSideAttunement, "darkSideAttunement", 0f);
            Scribe_Values.Look(ref lightSideAttunement, "lightSideAttunement", 0f);
            Scribe_Values.Look(ref enableDarkSideVisuals, "enableDarkSideVisuals", true);
        }

        public void AddDarkSideAttunement(float amount) => darkSideAttunement += amount;
        public void RemoveDarkSideAttunement(float amount) => darkSideAttunement -= amount;
        public void AddLightSideAttunement(float amount) => lightSideAttunement += amount;
        public void RemoveLightSideAttunement(float amount) => lightSideAttunement -= amount;

        public float GetStatOffset(StatDef stat)
        {
            if (stat == StatDef.Named("Force_Darkside_Attunement"))
                return DarkSideAttunement -= stat.defaultBaseValue;
            if (stat == StatDef.Named("Force_Lightside_Attunement"))
                return LightSideAttunement -= stat.defaultBaseValue;
            return 0f;
        }

        public float GetStatFactor(StatDef stat)
        {
            if (stat == StatDef.Named("Force_Darkside_Attunement"))
                return DarkSideAttunement *= stat.defaultBaseValue;
            if (stat == StatDef.Named("Force_Lightside_Attunement"))
                return LightSideAttunement *= stat.defaultBaseValue;
            return 1f;
        }
    }
}