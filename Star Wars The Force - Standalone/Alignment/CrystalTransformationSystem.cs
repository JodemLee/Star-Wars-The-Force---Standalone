using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TheForce_Standalone
{
    public class CrystalTransformationSystem
    {
        private readonly CompClass_ForceUser parent;

        public ThingDef TargetCrystalDef { get; set; }
        public JobDef TransformationJobDef { get; set; }

        public CrystalTransformationSystem(CompClass_ForceUser parent)
        {
            this.parent = parent;
            InitializeDefaults();
        }

        public List<ThingDef> DarkSideCrystals { get; set; } = new List<ThingDef>();
        public List<ThingDef> LightSideCrystals { get; set; } = new List<ThingDef>();

        private void InitializeDefaults()
        {
            var kyberCrystal = DefDatabase<ThingDef>.GetNamedSilentFail("Force_KyberCrystal");
            var purifiedCrystal = DefDatabase<ThingDef>.GetNamedSilentFail("Force_CleansedCrystal");
            var bledCrystal = DefDatabase<ThingDef>.GetNamedSilentFail("Force_BledCrystal");

            if (kyberCrystal != null) DarkSideCrystals.Add(kyberCrystal);
            if (purifiedCrystal != null) DarkSideCrystals.Add(purifiedCrystal);
            if (bledCrystal != null) LightSideCrystals.Add(bledCrystal);

            TransformationJobDef = DefDatabase<JobDef>.GetNamedSilentFail("Force_TransformCrystal");
        }

        public IEnumerable<Gizmo> GetTransformationGizmos()
        {
            if (parent.Pawn == null || !parent.Pawn.IsColonistPlayerControlled || !parent.enableCrystalTransformation)
                yield break;

            if (CanTransformToDarkSide())
            {
                yield return new Command_Action
                {
                    defaultLabel = "Force.AlignmentActions.BleedCrystal".Translate(),
                    defaultDesc = "Force.AlignmentActions.BleedCrystalDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmo/Bleed") ?? BaseContent.BadTex,
                    action = () => StartTargeting(TransformationType.DarkSide),
                    Disabled = !CanTransformNow(),
                    disabledReason = GetDisabledReason()
                };
            }

            if (CanTransformToLightSide())
            {
                yield return new Command_Action
                {
                    defaultLabel = "Force.AlignmentActions.PurifyCrystal".Translate(),
                    defaultDesc = "Force.AlignmentActions.PurifyCrystalDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmo/Purify") ?? BaseContent.BadTex,
                    action = () => StartTargeting(TransformationType.LightSide),
                    Disabled = !CanTransformNow(),
                    disabledReason = GetDisabledReason()
                };
            }
        }

        private bool CanTransformToDarkSide()
        {
            return parent.Alignment.DarkSideAttunement >= 50f;
        }

        private bool CanTransformToLightSide()
        {
            return parent.Alignment.LightSideAttunement >= 50f;
        }

        private bool CanTransformNow()
        {
            return parent.Pawn != null && TransformationJobDef != null;
        }

        private string GetDisabledReason()
        {
            if (parent.Pawn == null) return "Force.AlignmentActions.InvalidPawn".Translate();
            if (TransformationJobDef == null) return "Force.AlignmentActions.NoTransformationJob".Translate();
            return null;
        }

        private void StartTargeting(TransformationType transformationType)
        {
            if (!CanTransformNow())
                return;

            List<ThingDef> validCrystals = transformationType == TransformationType.DarkSide ?
                DarkSideCrystals : LightSideCrystals;

            Find.Targeter.BeginTargeting(
                new TargetingParameters
                {
                    canTargetItems = true,
                    mapObjectTargetsMustBeAutoAttackable = false,
                    validator = (TargetInfo t) =>
                        t.Thing != null &&
                        !t.Thing.Destroyed &&
                        validCrystals.Contains(t.Thing.def) &&
                        t.Thing.Spawned
                },
                (LocalTargetInfo target) =>
                {
                    if (target.HasThing && validCrystals.Contains(target.Thing.def))
                        CreateTransformJob(target, transformationType);
                }
            );
        }

        private void CreateTransformJob(LocalTargetInfo target, TransformationType transformationType)
        {
            Job job = JobMaker.MakeJob(TransformationJobDef, target);
            parent.Pawn.jobs.TryTakeOrderedJob(job);
        }

        public TransformationType GetTransformationTypeForCrystal(ThingDef crystalDef)
        {
            if (DarkSideCrystals.Contains(crystalDef))
                return TransformationType.DarkSide;
            if (LightSideCrystals.Contains(crystalDef))
                return TransformationType.LightSide;

            return TransformationType.DarkSide;
        }

        public void DoDarkSideTransformation(Thing targetCrystal)
        {
            var resultDef = DefDatabase<ThingDef>.GetNamedSilentFail("Force_BledCrystal");
            if (resultDef == null) return;

            TransformCrystal(targetCrystal, resultDef,
                () => FleckMaker.ThrowFireGlow(targetCrystal.DrawPos, targetCrystal.Map, 0.5f),
                () => SoundDef.Named("Force_KyberBleedSound")?.PlayOneShot(new TargetInfo(targetCrystal.Position, targetCrystal.Map)));

            parent.Alignment.AddLightSideAttunement(-25f);

            Messages.Message("Force.AlignmentActions.CrystalBleedSuccess".Translate(parent.Pawn.LabelShort), MessageTypeDefOf.PositiveEvent);
        }

        public void DoLightSideTransformation(Thing targetCrystal)
        {
            var resultDef = DefDatabase<ThingDef>.GetNamedSilentFail("Force_CleansedCrystal");
            if (resultDef == null) return;

            TransformCrystal(targetCrystal, resultDef,
                () => FleckMaker.ThrowLightningGlow(targetCrystal.DrawPos, targetCrystal.Map, 0.5f),
                () => SoundDefOf.PsychicSootheGlobal.PlayOneShot(new TargetInfo(targetCrystal.Position, targetCrystal.Map)));

            parent.Alignment.AddDarkSideAttunement(-25f);

            Messages.Message("Force.AlignmentActions.CrystalPurifySuccess".Translate(parent.Pawn.LabelShort), MessageTypeDefOf.PositiveEvent);
        }

        private void TransformCrystal(Thing targetCrystal, ThingDef resultDef, System.Action visualEffect, System.Action soundEffect)
        {
            if (targetCrystal == null || targetCrystal.Destroyed || resultDef == null)
                return;

            IntVec3 position = targetCrystal.Position;
            Map map = targetCrystal.Map;
            int stackCount = targetCrystal.stackCount;

            int? overrideIndex = targetCrystal.overrideGraphicIndex ??
                (targetCrystal.def.graphicData != null ? targetCrystal.thingIDNumber : null);

            targetCrystal.Destroy(DestroyMode.Vanish);

            Thing newCrystal = ThingMaker.MakeThing(resultDef);
            newCrystal.stackCount = stackCount;

            if (overrideIndex.HasValue)
            {
                newCrystal.overrideGraphicIndex = overrideIndex.Value;
            }

            GenPlace.TryPlaceThing(
                newCrystal,
                position,
                map,
                ThingPlaceMode.Near,
                (Thing placedThing, int count) =>
                {
                    if (placedThing.Spawned)
                    {
                        PostTransformationEffects(placedThing);
                    }
                },
                null,
                newCrystal.Rotation
            );

            visualEffect?.Invoke();
            soundEffect?.Invoke();
        }

        protected virtual void PostTransformationEffects(Thing newCrystal)
        {
            // Optional: Add transformation-specific effects
        }
    }

    public enum TransformationType
    {
        DarkSide,
        LightSide
    }
}