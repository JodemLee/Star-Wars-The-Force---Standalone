using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    [StaticConstructorOnStartup]
    public class CompStatueWithContainer : CompStatue, IThingHolder
    {
        private ThingOwner innerContainer;

        private static readonly SimpleCurve sculptureTargetWeightByOpinionCurve = new SimpleCurve(new CurvePoint[3]
    {
        new CurvePoint(-100f, 1f),
        new CurvePoint(0f, 2f),
        new CurvePoint(100f, 10f)
    });

        private static List<Pawn> tmpPawnsToSelectFrom = new List<Pawn>();

        private Pawn fakePawn;

        private BodyTypeDef bodyType;

        private HeadTypeDef headType;

        private HairDef hairDef;

        private XenotypeDef xenotype;

        private Color hairColor;

        private List<ThingDef> apparel = new List<ThingDef>();

        private List<GeneDef> nonXenotypeGenes = new List<GeneDef>();

        private List<SavedHediffProps> hediffsWhichAffectRendering = new List<SavedHediffProps>();

        private Verse.Name name;

        private Verse.Gender gender;

        protected new TaggedString titleInt = null;


        private int lifestageIndex;

        private int descriptionSeed;

        private Dictionary<string, object> additionalSavedPawnDataForMods = new Dictionary<string, object>();

        public override bool Active => fakePawn != null;

        public CompStatueWithContainer()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        public override void JustCreatedBy(Pawn pawn)
        {
            if (fakePawn != null)
            {
                var compArt = parent.TryGetComp<CompArt>();
                if (compArt != null && !compArt.Active)
                {
                    compArt.InitializeArt(ArtGenerationContext.Colony);
                }
            }
        }

        public void StorePawn(Pawn pawn)
        {
            if (pawn == null) return;

            if (Props.storePhysicalPawn)
            {
                if (pawn.Spawned)
                    pawn.DeSpawn();
                innerContainer.TryAdd(pawn);
            }

            var compArt = parent.TryGetComp<CompArt>();
            if (compArt != null && !compArt.Active)
            {
                compArt.InitializeArt(ArtGenerationContext.Colony);
                compArt.Title = pawn.LabelCap;
            }


            if (innerContainer.Count > 0 && innerContainer[0] is Pawn containedPawn)
            {
                name = containedPawn.Name;
                gender = containedPawn.gender;
                bodyType = containedPawn.story.bodyType;
                headType = containedPawn.story.headType;
                hairDef = containedPawn.story.hairDef;
                hairColor = containedPawn.story.HairColor;
                xenotype = ModsConfig.BiotechActive ? containedPawn.genes?.Xenotype : null;
                lifestageIndex = containedPawn.ageTracker.CurLifeStageIndex;

                apparel.Clear();
                foreach (var apparelItem in containedPawn.apparel.WornApparel)
                {
                    apparel.Add(apparelItem.def);
                }

                nonXenotypeGenes.Clear();
                if (ModsConfig.BiotechActive && containedPawn.genes != null)
                {
                    foreach (var gene in containedPawn.genes.GenesListForReading)
                    {
                        if (!gene.Overridden)
                        {
                            nonXenotypeGenes.Add(gene.def);
                        }
                    }
                }

                hediffsWhichAffectRendering.Clear();
                foreach (var hediff in containedPawn.health.hediffSet.hediffs)
                {
                    if (hediff.def.hediffClass == typeof(Hediff_MissingPart)) continue;
                    hediffsWhichAffectRendering.Add(new SavedHediffProps
                    {
                        hediffDef = hediff.def,
                        bodyPart = hediff.Part,
                        severity = hediff.Severity
                    });
                }
                InitFakePawn();
            }
        }

        public override void DrawAt(Vector3 drawPos, bool flip = false)
        {
            Vector3 loc = new Vector3(drawPos.x, drawPos.y - 0.03658537f, drawPos.z - 0.15f);
            StatueBaseGraphic.Draw(loc, flip ? parent.Rotation.Opposite : parent.Rotation, parent);

            if (fakePawn != null && Props.showContainedPawn)
            {
                Vector3 pawnDrawPos = drawPos + new Vector3(0f, 0f, -0.1f);
                float headSizeFactor = fakePawn.ageTracker.CurLifeStage.headSizeFactor ?? 1f;
                float heightOffset = (1f - headSizeFactor) * 0.5f;
                pawnDrawPos += Vector3.back * heightOffset;
                pawnDrawPos -= Altitudes.AltIncVect * 0.25f;
                fakePawn.Drawer.renderer.RenderPawnAt(pawnDrawPos, Rot4.South, neverAimWeapon: true);
            }
        }

        public override TaggedString GenerateImageDescription()
        {
            if (fakePawn == null)
                return "StatueOfUnknown".Translate();

            if (descriptionSeed == 0)
            {
                descriptionSeed = Gen.HashCombineInt(fakePawn.thingIDNumber, name?.GetHashCode() ?? 0);
            }

            GrammarRequest request = default(GrammarRequest);
            request.Includes.Add(Props.descriptionMaker);

            request.Rules.AddRange(GrammarUtility.RulesForPawn("SUBJECT", fakePawn, request.Constants));

            request.Rules.Add(new Rule_String("pawn_name", fakePawn.Name.ToStringFull));
            request.Rules.Add(new Rule_String("pawn_gender", fakePawn.gender.ToString()));

            if (fakePawn.story != null)
            {
                request.Rules.Add(new Rule_String("pawn_title", fakePawn.story.title ?? "None".Translate()));
            }

            Rand.PushState();
            Rand.Seed = descriptionSeed;
            string text = GrammarResolver.Resolve("r_statue_description", request, $"statue_desc_{fakePawn.thingIDNumber}");
            Rand.PopState();

            return text;
        }

        private void InitFakePawn()
        {
            fakePawn = (Pawn)ThingMaker.MakeThing(ThingDefOf.Human);
            PawnComponentsUtility.CreateInitialComponents(fakePawn);
            fakePawn.Name = name;
            fakePawn.gender = gender;
            fakePawn.kindDef = PawnKindDefOf.Colonist;

            if (ModsConfig.BiotechActive)
            {
                fakePawn.genes.SetXenotype(xenotype ?? XenotypeDefOf.Baseliner);
            }

            fakePawn.ageTracker.LockCurrentLifeStageIndex(lifestageIndex);
            fakePawn.story.bodyType = bodyType;
            fakePawn.story.headType = headType;
            fakePawn.story.hairDef = hairDef;
            fakePawn.story.HairColor = hairColor;
            fakePawn.style.beardDef = base.beard ?? BeardDefOf.NoBeard;

            fakePawn.apparel.DestroyAll();
            foreach (ThingDef item2 in apparel)
            {
                Apparel item = ((!item2.MadeFromStuff) ? ((Apparel)ThingMaker.MakeThing(item2)) : ((Apparel)ThingMaker.MakeThing(item2, GenStuff.DefaultStuffFor(item2))));
                fakePawn.apparel.WornApparel.Add(item);
            }

            if (!nonXenotypeGenes.NullOrEmpty())
            {
                foreach (GeneDef nonXenotypeGene in nonXenotypeGenes)
                {
                    fakePawn.genes.AddGene(nonXenotypeGene, xenogene: true);
                }
            }

            if (!hediffsWhichAffectRendering.NullOrEmpty())
            {
                foreach (SavedHediffProps item3 in hediffsWhichAffectRendering)
                {
                    fakePawn.health.AddHediff(item3.hediffDef, item3.bodyPart).Severity = item3.severity;
                }
            }

            fakePawn.Drawer.renderer.SetStatue(parent.Stuff);
            fakePawn.Drawer.renderer.SetAllGraphicsDirty();
            fakePawn.Drawer.renderer.EnsureGraphicsInitialized();
            Notify_ColorChanged();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);

            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref gender, "gender");
            Scribe_Values.Look(ref bodyType, "bodyType");
            Scribe_Values.Look(ref headType, "headType");
            Scribe_Values.Look(ref hairDef, "hairDef");
            Scribe_Values.Look(ref hairColor, "hairColor");
            Scribe_Defs.Look(ref xenotype, "xenotype");
            Scribe_Values.Look(ref lifestageIndex, "lifestageIndex");
            Scribe_Collections.Look(ref apparel, "apparel", LookMode.Def);
            Scribe_Collections.Look(ref nonXenotypeGenes, "nonXenotypeGenes", LookMode.Def);
            Scribe_Collections.Look(ref hediffsWhichAffectRendering, "hediffsWhichAffectRendering", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && innerContainer.Count > 0)
            {
                InitFakePawn();
            }
        }


        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            if (previousMap != null)
            {
                if (mode == DestroyMode.Deconstruct)
                {
                    innerContainer.TryDropAll(parent.Position, previousMap, ThingPlaceMode.Near);
                }
                foreach (Thing thing in innerContainer.ToList())
                {
                    if (thing is Pawn pawn)
                    {
                        if (!pawn.Dead)
                        {
                            pawn.Destroy();
                        }
                        if (pawn.Faction == Faction.OfPlayer)
                        {
                            Find.ColonistBar.MarkColonistsDirty();
                        }
                        
                    }
                }
                innerContainer.ClearAndDestroyContents();
            }
            else
            {
                innerContainer.ClearAndDestroyContents();
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder(base.CompInspectStringExtra());

            if (innerContainer.Count > 0)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append("Contains".Translate() + ": ");

                foreach (Thing thing in innerContainer)
                {
                    sb.Append(thing.LabelCap);
                    if (thing != innerContainer.Last())
                        sb.Append(", ");
                }
            }

            return sb.ToString();
        }

        public  new CompProperties_StatueWithContainer Props =>
            (CompProperties_StatueWithContainer)props;
    }

    public class CompProperties_StatueWithContainer : CompProperties_Statue
    {
        public bool storePhysicalPawn = true;
        public bool showContainedPawn = true; 

        public CompProperties_StatueWithContainer()
        {
            compClass = typeof(CompStatueWithContainer);
        }
    }
}