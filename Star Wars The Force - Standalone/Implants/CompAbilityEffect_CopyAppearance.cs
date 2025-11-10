using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Implants
{
    public class CompAbilityEffect_CopyAppearance : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = parent.pawn;
            Pawn targetPawn = target.Pawn;

            if (caster == null || targetPawn == null || caster == targetPawn)
                return;

            // Apply the appearance copying hediff to the caster
            Hediff hediff = HediffMaker.MakeHediff(ForceDefOf.Force_CopiedAppearance, caster);
            HediffComp_CopiedAppearance comp = hediff.TryGetComp<HediffComp_CopiedAppearance>();

            if (comp != null)
            {
                comp.StoreOriginalAppearance(caster);
                comp.CopyTargetAppearance(targetPawn);
            }

            caster.health.AddHediff(hediff);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!base.CanApplyOn(target, dest))
                return false;

            Pawn targetPawn = target.Pawn;
            Pawn caster = parent.pawn;

            if (targetPawn == null || caster == null)
                return false;

            if (targetPawn == caster || targetPawn.Dead)
                return false;

            Hediff existingHediff = caster.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_CopiedAppearance);
            return existingHediff == null;
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
                return false;

            Pawn targetPawn = target.Pawn;
            if (targetPawn == null)
            {
                if (throwMessages)
                    Messages.Message("Ability requires a valid pawn target.", MessageTypeDefOf.RejectInput);
                return false;
            }

            if (targetPawn == parent.pawn)
            {
                if (throwMessages)
                    Messages.Message("Cannot copy appearance from yourself.", MessageTypeDefOf.RejectInput);
                return false;
            }

            Hediff existingHediff = parent.pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_CopiedAppearance);
            if (existingHediff != null)
            {
                if (throwMessages)
                    Messages.Message("Appearance already copied from another pawn.", MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }
    }

    public class HediffComp_CopiedAppearance : HediffComp
    {
        public PawnAppearanceData originalAppearance;
        public PawnAppearanceData copiedAppearance;
        public Pawn copiedFromPawn;

        public HediffCompProperties_CopiedAppearance Props => (HediffCompProperties_CopiedAppearance)props;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Deep.Look(ref originalAppearance, "originalAppearance");
            Scribe_Deep.Look(ref copiedAppearance, "copiedAppearance");
            Scribe_References.Look(ref copiedFromPawn, "copiedFromPawn");
        }

        public void StoreOriginalAppearance(Pawn pawn)
        {
            originalAppearance = new PawnAppearanceData();
            originalAppearance.CopyFrom(pawn);
        }

        public void CopyTargetAppearance(Pawn targetPawn)
        {
            copiedAppearance = new PawnAppearanceData();
            copiedAppearance.CopyFrom(targetPawn);
            copiedFromPawn = targetPawn;

            // Apply the copied appearance to the current pawn
            copiedAppearance.ApplyTo(Pawn);

            // Refresh graphics
            Pawn.Drawer.renderer.SetAllGraphicsDirty();
            PortraitsCache.SetDirty(Pawn);
        }

        public void RestoreOriginalAppearance()
        {
            if (originalAppearance != null)
            {
                originalAppearance.ApplyTo(Pawn);
                Pawn.Drawer.renderer.SetAllGraphicsDirty();
                PortraitsCache.SetDirty(Pawn);
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            RestoreOriginalAppearance();
        }

        public override string CompLabelInBracketsExtra => copiedFromPawn != null ? $"({copiedFromPawn.LabelShort})" : null;

        public override string CompTipStringExtra => copiedFromPawn != null ?
            $"Appearance copied from: {copiedFromPawn.LabelShortCap}" :
            "Appearance altered";
    }

    public class HediffCompProperties_CopiedAppearance : HediffCompProperties
    {
        public HediffCompProperties_CopiedAppearance()
        {
            compClass = typeof(HediffComp_CopiedAppearance);
        }
    }

    public class PawnAppearanceData : IExposable
    {
        public HeadTypeDef headType;
        public BodyTypeDef bodyType;
        public HairDef hairDef;
        public Color hairColor;
        public Color skinColor;
        public Color? skinColorOverride;
        public BeardDef beardDef;
        public TattooDef bodyTattoo;
        public TattooDef faceTattoo;
        public FurDef furDef;

        public void CopyFrom(Pawn pawn)
        {
            if (pawn == null) return;

            headType = pawn.story.headType;
            bodyType = pawn.story.bodyType;
            hairDef = pawn.story.hairDef;
            hairColor = pawn.story.HairColor;
            skinColor = pawn.story.SkinColorBase;
            skinColorOverride = pawn.story.skinColorOverride;
            beardDef = pawn.style.beardDef;
            furDef = pawn.story.furDef;

            if (ModsConfig.IdeologyActive)
            {
                bodyTattoo = pawn.style.BodyTattoo;
                faceTattoo = pawn.style.FaceTattoo;
            }
        }

        public void ApplyTo(Pawn pawn)
        {
            if (pawn == null) return;

            pawn.story.headType = headType;
            pawn.story.bodyType = bodyType;
            pawn.story.hairDef = hairDef;
            pawn.story.HairColor = hairColor;
            pawn.story.SkinColorBase = skinColor;
            pawn.story.skinColorOverride = skinColorOverride;
            pawn.story.furDef = furDef;
            pawn.style.beardDef = beardDef;

            if (ModsConfig.IdeologyActive)
            {
                pawn.style.BodyTattoo = bodyTattoo;
                pawn.style.FaceTattoo = faceTattoo;
            }
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref headType, "headType");
            Scribe_Defs.Look(ref bodyType, "bodyType");
            Scribe_Defs.Look(ref hairDef, "hairDef");
            Scribe_Values.Look(ref hairColor, "hairColor");
            Scribe_Values.Look(ref skinColor, "skinColor");
            Scribe_Values.Look(ref skinColorOverride, "skinColorOverride");
            Scribe_Defs.Look(ref beardDef, "beardDef");
            Scribe_Defs.Look(ref bodyTattoo, "bodyTattoo");
            Scribe_Defs.Look(ref faceTattoo, "faceTattoo");
            Scribe_Defs.Look(ref furDef, "furDef");
        }
    }
}
