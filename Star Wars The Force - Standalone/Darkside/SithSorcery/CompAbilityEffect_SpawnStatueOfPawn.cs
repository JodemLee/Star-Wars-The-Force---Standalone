using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    public class CompAbilityEffect_SpawnStatueOfPawn : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect_SpawnStatueOfPawn Props =>
            (CompProperties_AbilityEffect_SpawnStatueOfPawn)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (target.Pawn == null)
            {
                Log.Error("CompAbilityEffect_SpawnStatueOfPawn applied to non-pawn target");
                return;
            }
            target.Pawn.jobs.posture = PawnPosture.Standing;
            Thing statue = ThingMaker.MakeThing(Props.statueDef ?? ThingDefOf.Statue, Props.statueStuffDef);
            CompStatueWithContainer comp = statue.TryGetComp<CompStatueWithContainer>();
            if (comp != null)
            {
                comp.StorePawn(target.Pawn);
                comp.JustCreatedBy(parent.pawn);
                comp.GenerateImageDescription();
            }
            GenPlace.TryPlaceThing(statue, target.Cell, parent.pawn.Map, ThingPlaceMode.Near);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!base.CanApplyOn(target, dest))
                return false;


            return target.Pawn != null;
        }
    }

    public class CompProperties_AbilityEffect_SpawnStatueOfPawn : CompProperties_AbilityEffect
    {
        public ThingDef statueDef;
        public ThingDef statueStuffDef;

        public CompProperties_AbilityEffect_SpawnStatueOfPawn()
        {
            compClass = typeof(CompAbilityEffect_SpawnStatueOfPawn);
        }
    }
}

